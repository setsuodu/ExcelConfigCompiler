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
            win.minSize = new Vector2(560, 640);
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

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Excel Config Compiler", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "只填一个 Excel 根目录。\n" +
                "Client / Server / Dashboard 三套输出互相独立：不填 = 不生成。\n" +
                "每种格式（bytes / json）单独勾选 + 单独目录。\n" +
                "表仍按 Excel 下 Client/Server/Shared 分类；Shared 写到你启用的各端。",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("输入", EditorStyles.boldLabel);
            _settings.ExcelSourceFolder = FolderField("Excel 根目录", _settings.ExcelSourceFolder);

            DrawTargetBlock(
                "客户端",
                ref _settings.ClientNamespace,
                ref _settings.ClientCodeFolder,
                ref _settings.ClientExportBytes,
                ref _settings.ClientBytesFolder,
                ref _settings.ClientExportJson,
                ref _settings.ClientJsonFolder,
                showFrozen: false);

            DrawTargetBlock(
                "服务器",
                ref _settings.ServerNamespace,
                ref _settings.ServerCodeFolder,
                ref _settings.ServerExportBytes,
                ref _settings.ServerBytesFolder,
                ref _settings.ServerExportJson,
                ref _settings.ServerJsonFolder,
                showFrozen: true);

            DrawTargetBlock(
                "Dashboard",
                ref _settings.DashboardNamespace,
                ref _settings.DashboardCodeFolder,
                ref _settings.DashboardExportBytes,
                ref _settings.DashboardBytesFolder,
                ref _settings.DashboardExportJson,
                ref _settings.DashboardJsonFolder,
                showFrozen: false);

            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_settings);

            EditorGUILayout.Space(8);
            using (new EditorGUI.DisabledScope(_isCompiling))
            {
                if (GUILayout.Button("一键导表", GUILayout.Height(36)))
                    Compile();
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("日志", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));
            EditorGUILayout.TextArea(_lastLog ?? "", GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void DrawTargetBlock(
            string title,
            ref string ns,
            ref string codeFolder,
            ref bool exportBytes,
            ref string bytesFolder,
            ref bool exportJson,
            ref string jsonFolder,
            bool showFrozen)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            ns = EditorGUILayout.TextField("命名空间", ns ?? "");
            codeFolder = FolderField("代码目录（空=不生成 .cs）", codeFolder);

            EditorGUILayout.BeginHorizontal();
            exportBytes = EditorGUILayout.ToggleLeft("bytes", exportBytes, GUILayout.Width(70));
            using (new EditorGUI.DisabledScope(!exportBytes))
                bytesFolder = FolderFieldInline(bytesFolder);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            exportJson = EditorGUILayout.ToggleLeft("json", exportJson, GUILayout.Width(70));
            using (new EditorGUI.DisabledScope(!exportJson))
                jsonFolder = FolderFieldInline(jsonFolder);
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ToggleLeft("lua（未实现）", false, GUILayout.Width(100));
            EditorGUILayout.TextField("");
            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();

            if (showFrozen)
                _settings.UseFrozenDictionary = EditorGUILayout.Toggle(
                    new GUIContent("FrozenDictionary", "仅 net8+；Unity 请关"),
                    _settings.UseFrozenDictionary);
        }

        private string FolderFieldInline(string path)
        {
            path = EditorGUILayout.TextField(path ?? "");
            if (GUILayout.Button("...", GUILayout.Width(30)))
            {
                var abs = EditorUtility.OpenFolderPanel("选择目录", Application.dataPath, "");
                if (!string.IsNullOrEmpty(abs))
                {
                    if (abs.StartsWith(Application.dataPath))
                        path = "Assets" + abs.Substring(Application.dataPath.Length).Replace('\\', '/');
                    else
                        path = abs;
                }
            }
            return path;
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

                bool anyOutput =
                    HasPath(_settings.ClientCodeFolder) ||
                    (_settings.ClientExportBytes && HasPath(_settings.ClientBytesFolder)) ||
                    (_settings.ClientExportJson && HasPath(_settings.ClientJsonFolder)) ||
                    HasPath(_settings.ServerCodeFolder) ||
                    (_settings.ServerExportBytes && HasPath(_settings.ServerBytesFolder)) ||
                    (_settings.ServerExportJson && HasPath(_settings.ServerJsonFolder)) ||
                    HasPath(_settings.DashboardCodeFolder) ||
                    (_settings.DashboardExportBytes && HasPath(_settings.DashboardBytesFolder)) ||
                    (_settings.DashboardExportJson && HasPath(_settings.DashboardJsonFolder));

                if (!anyOutput)
                {
                    _lastLog += "[错误] 所有输出都为空。请至少在某一端勾选格式并填写目录，或填写代码目录。\n";
                    return;
                }

                var result = CompilePipeline.Compile(new CompilePipeline.Options
                {
                    ExcelRoot = excelDir,
                    ClientCodeDir = NullIfEmpty(_settings.ClientCodeFolder),
                    ClientBytesDir = _settings.ClientExportBytes ? NullIfEmpty(_settings.ClientBytesFolder) : null,
                    ClientNamespace = _settings.ClientNamespace,
                    ClientJsonDir = _settings.ClientExportJson ? NullIfEmpty(_settings.ClientJsonFolder) : null,
                    ServerCodeDir = NullIfEmpty(_settings.ServerCodeFolder),
                    ServerBytesDir = _settings.ServerExportBytes ? NullIfEmpty(_settings.ServerBytesFolder) : null,
                    ServerNamespace = _settings.ServerNamespace,
                    ServerJsonDir = _settings.ServerExportJson ? NullIfEmpty(_settings.ServerJsonFolder) : null,
                    UseFrozenDictionary = _settings.UseFrozenDictionary,
                    ExportJson = _settings.ClientExportJson || _settings.ServerExportJson || _settings.DashboardExportJson,
                    ManifestPath = Path.Combine(excelDir, "tables.lock.json"),
                });

                foreach (var msg in result.Messages)
                    _lastLog += msg + "\n";

                if (HasDashboardOutput())
                {
                    var dash = CompilePipeline.Compile(new CompilePipeline.Options
                    {
                        ExcelRoot = excelDir,
                        ClientCodeDir = null,
                        ClientBytesDir = null,
                        ClientJsonDir = null,
                        ClientNamespace = _settings.DashboardNamespace,
                        ServerCodeDir = NullIfEmpty(_settings.DashboardCodeFolder),
                        ServerBytesDir = _settings.DashboardExportBytes ? NullIfEmpty(_settings.DashboardBytesFolder) : null,
                        ServerJsonDir = _settings.DashboardExportJson ? NullIfEmpty(_settings.DashboardJsonFolder) : null,
                        ServerNamespace = _settings.DashboardNamespace,
                        UseFrozenDictionary = _settings.DashboardUseServerStyle && _settings.UseFrozenDictionary,
                        ExportJson = _settings.DashboardExportJson,
                        ManifestPath = Path.Combine(excelDir, "tables.lock.dashboard.json"),
                    });
                    _lastLog += "\n--- Dashboard ---\n";
                    foreach (var msg in dash.Messages)
                        _lastLog += msg + "\n";
                }

                _lastLog += $"\n完成：{result.TableCount} 表 / {result.TotalRows} 行\n";
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

        private bool HasDashboardOutput() =>
            HasPath(_settings.DashboardCodeFolder) ||
            (_settings.DashboardExportBytes && HasPath(_settings.DashboardBytesFolder)) ||
            (_settings.DashboardExportJson && HasPath(_settings.DashboardJsonFolder));

        private static bool HasPath(string p) => !string.IsNullOrWhiteSpace(p);

        private string NullIfEmpty(string path) =>
            string.IsNullOrWhiteSpace(path) ? null : ResolvePath(path);

        private static string ResolvePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return Application.dataPath;
            if (Path.IsPathRooted(path)) return path;
            if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("Assets\\", StringComparison.OrdinalIgnoreCase))
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
        }
    }
}
