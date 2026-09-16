using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using ExcelConfigCompiler.Compiler;

namespace ExcelConfigCompiler.Editor
{
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
            win.minSize = new Vector2(520, 520);
            win.Show();
        }

        private void OnEnable() => LoadOrCreateSettings();

        private void LoadOrCreateSettings()
        {
            var guids = AssetDatabase.FindAssets("t:ExcelConfigSettings");
            if (guids.Length > 0)
            {
                _settings = AssetDatabase.LoadAssetAtPath<ExcelConfigSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
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

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Excel Config Compiler", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Excel 根下分 Client / Server / Shared。\n" +
                "客户端、服务器输出目录各自填写，只写一份，不会再拼 /client。\n" +
                "两端命名空间独立。\n" +
                "可选导出 JSON（AOT 零反射 ReadJson/LoadJson），正式包仍用 .bytes。",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Excel 输入", EditorStyles.boldLabel);
            _settings.ExcelSourceFolder = FolderField("Excel 根目录", _settings.ExcelSourceFolder);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("客户端输出", EditorStyles.boldLabel);
            _settings.ClientNamespace = EditorGUILayout.TextField("客户端命名空间", _settings.ClientNamespace);
            _settings.ClientCodeFolder = FolderField("客户端代码目录", _settings.ClientCodeFolder);
            _settings.ClientBytesFolder = FolderField("客户端 bytes 目录", _settings.ClientBytesFolder);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("服务器输出", EditorStyles.boldLabel);
            _settings.ServerNamespace = EditorGUILayout.TextField("服务器命名空间", _settings.ServerNamespace);
            _settings.ServerCodeFolder = FolderField("服务器代码目录", _settings.ServerCodeFolder);
            _settings.ServerBytesFolder = FolderField("服务器 bytes 目录", _settings.ServerBytesFolder);
            _settings.UseFrozenDictionary = EditorGUILayout.Toggle("服务器 FrozenDictionary", _settings.UseFrozenDictionary);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("JSON 导出（可选）", EditorStyles.boldLabel);
            _settings.ExportJson = EditorGUILayout.Toggle("导出 JSON", _settings.ExportJson);
            using (new EditorGUI.DisabledScope(!_settings.ExportJson))
            {
                _settings.ClientJsonFolder = FolderField("客户端 JSON 目录", _settings.ClientJsonFolder);
                _settings.ServerJsonFolder = FolderField("服务器 JSON 目录", _settings.ServerJsonFolder);
            }

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_settings);

            EditorGUILayout.Space(10);
            using (new EditorGUI.DisabledScope(_isCompiling))
            {
                if (GUILayout.Button("一键导表", GUILayout.Height(36)))
                    Compile();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("日志", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(_lastLog, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private string FolderField(string label, string path)
        {
            EditorGUILayout.BeginHorizontal();
            path = EditorGUILayout.TextField(label, path ?? "");
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var abs = EditorUtility.OpenFolderPanel(label, Application.dataPath, "");
                if (!string.IsNullOrEmpty(abs))
                {
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
                string excelDir = ResolvePath(_settings.ExcelSourceFolder);
                if (!Directory.Exists(excelDir))
                {
                    _lastLog += "[错误] Excel 根目录不存在: " + excelDir + "\n";
                    return;
                }

                if (_settings.ExportJson)
                {
                    bool hasClientJson = !string.IsNullOrWhiteSpace(_settings.ClientJsonFolder);
                    bool hasServerJson = !string.IsNullOrWhiteSpace(_settings.ServerJsonFolder);
                    if (!hasClientJson && !hasServerJson)
                    {
                        _lastLog += "[错误] 已勾选导出 JSON，请至少填写客户端或服务器 JSON 目录。\n";
                        return;
                    }
                }

                var result = CompilePipeline.Compile(new CompilePipeline.Options
                {
                    ExcelRoot = excelDir,
                    ClientCodeDir = string.IsNullOrWhiteSpace(_settings.ClientCodeFolder) ? null : ResolvePath(_settings.ClientCodeFolder),
                    ClientBytesDir = string.IsNullOrWhiteSpace(_settings.ClientBytesFolder) ? null : ResolvePath(_settings.ClientBytesFolder),
                    ClientNamespace = _settings.ClientNamespace,
                    ServerCodeDir = string.IsNullOrWhiteSpace(_settings.ServerCodeFolder) ? null : ResolvePath(_settings.ServerCodeFolder),
                    ServerBytesDir = string.IsNullOrWhiteSpace(_settings.ServerBytesFolder) ? null : ResolvePath(_settings.ServerBytesFolder),
                    ServerNamespace = _settings.ServerNamespace,
                    UseFrozenDictionary = _settings.UseFrozenDictionary,
                    ExportJson = _settings.ExportJson,
                    ClientJsonDir = _settings.ExportJson && !string.IsNullOrWhiteSpace(_settings.ClientJsonFolder)
                        ? ResolvePath(_settings.ClientJsonFolder) : null,
                    ServerJsonDir = _settings.ExportJson && !string.IsNullOrWhiteSpace(_settings.ServerJsonFolder)
                        ? ResolvePath(_settings.ServerJsonFolder) : null,
                    ManifestPath = Path.Combine(excelDir, "tables.lock.json"),
                });

                foreach (var msg in result.Messages)
                    _lastLog += msg + "\n";

                _lastLog += $"\n完成：{result.TableCount} 表 / {result.TotalRows} 行\n";
                if (!string.IsNullOrEmpty(_settings.ClientCodeFolder))
                    _lastLog += "客户端代码 → " + ResolvePath(_settings.ClientCodeFolder) + "\n";
                if (!string.IsNullOrEmpty(_settings.ServerCodeFolder))
                    _lastLog += "服务器代码 → " + ResolvePath(_settings.ServerCodeFolder) + "\n";
                if (_settings.ExportJson)
                {
                    if (!string.IsNullOrWhiteSpace(_settings.ClientJsonFolder))
                        _lastLog += "客户端 JSON → " + ResolvePath(_settings.ClientJsonFolder) + "\n";
                    if (!string.IsNullOrWhiteSpace(_settings.ServerJsonFolder))
                        _lastLog += "服务器 JSON → " + ResolvePath(_settings.ServerJsonFolder) + "\n";
                    if (result.GeneratedJsonFiles != null && result.GeneratedJsonFiles.Count > 0)
                        _lastLog += $"JSON 文件数 → {result.GeneratedJsonFiles.Count}\n";
                }

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
            if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Assets\\", StringComparison.OrdinalIgnoreCase))
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            // 工程根相对（如 Excel，与 Assets 同级）
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
        }
    }
}
