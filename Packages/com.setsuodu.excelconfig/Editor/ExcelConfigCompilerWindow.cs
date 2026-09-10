using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using ExcelConfigCompiler.Compiler;

namespace ExcelConfigCompiler.Editor
{
    /// <summary>
    /// Unity 编辑器一键导表窗口。
    /// 菜单：Tools / Excel Config Compiler
    /// </summary>
    public class ExcelConfigCompilerWindow : EditorWindow
    {
        private ExcelConfigSettings _settings;
        private Vector2 _scroll;
        private string _lastLog = "";
        private bool _isCompiling;

        [MenuItem("Tools/Excel Config Compiler")]
        public static void Open()
        {
            var win = GetWindow<ExcelConfigCompilerWindow>("Excel Config Compiler");
            win.minSize = new Vector2(480, 360);
            win.Show();
        }

        private void OnEnable()
        {
            LoadOrCreateSettings();
        }

        private void LoadOrCreateSettings()
        {
            var guids = AssetDatabase.FindAssets("t:ExcelConfigSettings");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _settings = AssetDatabase.LoadAssetAtPath<ExcelConfigSettings>(path);
                return;
            }

            _settings = CreateInstance<ExcelConfigSettings>();
            AssetDatabase.CreateAsset(_settings, ExcelConfigSettings.DefaultAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ExcelConfigCompiler] 已创建默认配置: {ExcelConfigSettings.DefaultAssetPath}");
        }

        private void OnGUI()
        {
            if (_settings == null)
            {
                EditorGUILayout.HelpBox("找不到 ExcelConfigSettings，请重新打开窗口。", MessageType.Error);
                if (GUILayout.Button("重新加载")) LoadOrCreateSettings();
                return;
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Excel Config Compiler", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "EPPlus 读表 → 生成显式 Read/Write 的 C# 代码 + 二进制 .bytes\n" +
                "Runtime 使用 ref struct ByteReader，数值路径 0 GC，AOT/IL2CPP 友好。",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUI.BeginChangeCheck();

            _settings.ExcelSourceFolder = FolderField("Excel 源目录", _settings.ExcelSourceFolder);
            _settings.GeneratedCodeFolder = FolderField("代码输出目录", _settings.GeneratedCodeFolder);
            _settings.GeneratedBytesFolder = FolderField("二进制输出目录", _settings.GeneratedBytesFolder);
            _settings.Namespace = EditorGUILayout.TextField("命名空间", _settings.Namespace);

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(_settings);
            }

            EditorGUILayout.Space(12);

            using (new EditorGUI.DisabledScope(_isCompiling))
            {
                if (GUILayout.Button("一键导表 (Compile All)", GUILayout.Height(36)))
                {
                    Compile();
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("日志", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(_lastLog, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private string FolderField(string label, string path)
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField(label, path);
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var abs = EditorUtility.OpenFolderPanel(label, Application.dataPath, "");
                if (!string.IsNullOrEmpty(abs))
                {
                    // 尽量转成相对 Assets 的路径
                    if (abs.StartsWith(Application.dataPath))
                        path = "Assets" + abs.Substring(Application.dataPath.Length).Replace('\\', '/');
                    else
                        path = abs;
                }
            }
            EditorGUILayout.EndHorizontal();
            return path;
        }

        private void Compile()
        {
            _isCompiling = true;
            _lastLog = "";
            try
            {
                // 解析相对路径
                string excelDir = ResolvePath(_settings.ExcelSourceFolder);
                string codeDir = ResolvePath(_settings.GeneratedCodeFolder);
                string bytesDir = ResolvePath(_settings.GeneratedBytesFolder);

                if (!Directory.Exists(excelDir))
                {
                    Directory.CreateDirectory(excelDir);
                    _lastLog += $"已创建源目录: {excelDir}\n请放入 .xlsx 后重试。\n";
                    return;
                }

                // 为了简单，把代码和二进制都输出到同一个临时根，再分别移动
                // 或者直接让 Pipeline 支持两个输出目录。这里简单处理：先输出到 codeDir 的父级再整理。
                string tempRoot = Path.Combine(Path.GetTempPath(), "ExcelConfigCompiler_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempRoot);

                var result = CompilePipeline.Compile(excelDir, tempRoot, _settings.Namespace);

                // 复制产物到目标位置
                Directory.CreateDirectory(codeDir);
                Directory.CreateDirectory(bytesDir);

                foreach (var f in result.GeneratedCodeFiles)
                {
                    var dest = Path.Combine(codeDir, Path.GetFileName(f));
                    File.Copy(f, dest, true);
                }
                foreach (var f in result.GeneratedBinaryFiles)
                {
                    var dest = Path.Combine(bytesDir, Path.GetFileName(f));
                    File.Copy(f, dest, true);
                }

                try { Directory.Delete(tempRoot, true); } catch { /* ignore */ }

                foreach (var msg in result.Messages)
                    _lastLog += msg + "\n";

                _lastLog += $"\n完成：共 {result.TableCount} 张表，{result.TotalRows} 行。\n";
                _lastLog += $"代码 → {codeDir}\n二进制 → {bytesDir}\n";

                AssetDatabase.Refresh();
                Debug.Log($"[ExcelConfigCompiler] 导表成功：{result.TableCount} 表 / {result.TotalRows} 行");
            }
            catch (CompileException ex)
            {
                _lastLog += "[编译错误] " + ex.Message + "\n";
                Debug.LogError("[ExcelConfigCompiler] " + ex.Message);
            }
            catch (Exception ex)
            {
                _lastLog += "[异常] " + ex + "\n";
                Debug.LogException(ex);
            }
            finally
            {
                _isCompiling = false;
                Repaint();
            }
        }

        private static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return Application.dataPath;
            if (Path.IsPathRooted(path)) return path;
            // 相对 Assets
            if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Assets\\", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            }
            return Path.GetFullPath(Path.Combine(Application.dataPath, path));
        }
    }
}
