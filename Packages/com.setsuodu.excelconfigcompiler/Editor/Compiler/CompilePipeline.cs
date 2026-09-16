using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ExcelConfigCompiler.Compiler
{
    /// <summary>
    /// 编译流水线：按 Excel 根下 Client / Server / Shared 目录分流。
    /// 客户端 / 服务器输出路径与命名空间完全独立，不再套一层 output/client。
    /// </summary>
    public static class CompilePipeline
    {
        public sealed class Result
        {
            public int TableCount;
            public int TotalRows;
            public List<string> GeneratedCodeFiles = new List<string>();
            public List<string> GeneratedBinaryFiles = new List<string>();
            public List<string> GeneratedJsonFiles = new List<string>();
            public List<string> Messages = new List<string>();
            public string ManifestPath;
        }

        public sealed class Options
        {
            /// <summary>Excel 根目录（其下 Client/Server/Shared）</summary>
            public string ExcelRoot;

            public string ClientCodeDir;
            public string ClientBytesDir;
            public string ClientNamespace = "Game.Config";

            public string ServerCodeDir;
            public string ServerBytesDir;
            public string ServerNamespace = "Game.Server.Config";

            public bool UseFrozenDictionary = true;

            /// <summary>是否额外导出 JSON（开发期/热更可读）。默认 false，正式包仍用 .bytes。</summary>
            public bool ExportJson = false;

            /// <summary>客户端 JSON 输出目录；空则跟随 ClientBytesDir 旁的 Json 子逻辑——实际以本字段为准。</summary>
            public string ClientJsonDir;

            /// <summary>服务器 JSON 输出目录。</summary>
            public string ServerJsonDir;

            /// <summary>tables.lock.json 路径；空则写到 ExcelRoot/tables.lock.json</summary>
            public string ManifestPath;
        }

        /// <summary>兼容旧调用：单一 outputDir 时仍分 client/server 子目录（CLI 旧参数）。</summary>
        public static Result Compile(string inputPath, string outputDir, string ns = "Config")
        {
            var root = Path.GetFullPath(outputDir);
            return Compile(new Options
            {
                ExcelRoot = inputPath,
                ClientCodeDir = Path.Combine(root, "client", "Generated"),
                ClientBytesDir = Path.Combine(root, "client", "Tables"),
                ClientNamespace = ns ?? "Config",
                ServerCodeDir = Path.Combine(root, "server", "Generated"),
                ServerBytesDir = Path.Combine(root, "server", "Tables"),
                ServerNamespace = ns ?? "Config",
                UseFrozenDictionary = true,
                ManifestPath = Path.Combine(root, "tables.lock.json"),
            });
        }

        public static Result Compile(Options options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (string.IsNullOrWhiteSpace(options.ExcelRoot))
                throw new CompileException("", null, null, null, null, "ExcelRoot 不能为空。");

            EnsureEpplusLicense();

            var result = new Result();
            string inputPath = Path.GetFullPath(options.ExcelRoot);

            string clientCode = string.IsNullOrWhiteSpace(options.ClientCodeDir) ? null : Path.GetFullPath(options.ClientCodeDir);
            string clientBytes = string.IsNullOrWhiteSpace(options.ClientBytesDir) ? null : Path.GetFullPath(options.ClientBytesDir);
            string serverCode = string.IsNullOrWhiteSpace(options.ServerCodeDir) ? null : Path.GetFullPath(options.ServerCodeDir);
            string serverBytes = string.IsNullOrWhiteSpace(options.ServerBytesDir) ? null : Path.GetFullPath(options.ServerBytesDir);

            string clientNs = string.IsNullOrWhiteSpace(options.ClientNamespace) ? "Game.Config" : options.ClientNamespace;
            string serverNs = string.IsNullOrWhiteSpace(options.ServerNamespace) ? "Game.Server.Config" : options.ServerNamespace;

            string clientJson = string.IsNullOrWhiteSpace(options.ClientJsonDir) ? null : Path.GetFullPath(options.ClientJsonDir);
            string serverJson = string.IsNullOrWhiteSpace(options.ServerJsonDir) ? null : Path.GetFullPath(options.ServerJsonDir);

            var files = CollectClassifiedXlsx(inputPath);
            if (files.Count == 0)
                throw new CompileException(inputPath, null, null, null, null,
                    "没有找到任何 .xlsx。请将表放入 Client/、Server/ 或 Shared/ 子目录。");

            var manifestEntries = new List<ManifestWriter.Entry>();

            foreach (var item in files)
            {
                var tables = ExcelParser.ParseWorkbook(item.FullPath);
                foreach (var table in tables)
                {
                    table.Target = item.Target;
                    Validator.Validate(table);

                    var bytes = BinaryGenerator.Generate(table);
                    string primaryBytesPath = null;

                    switch (item.Target)
                    {
                        case TableTarget.Client:
                            if (string.IsNullOrEmpty(clientCode) && string.IsNullOrEmpty(clientBytes)
                                && !(options.ExportJson && !string.IsNullOrEmpty(clientJson)))
                                throw new CompileException(table.SourceFile, table.TableName, null, null, null,
                                    "Client 表请至少填写：客户端代码目录、bytes 目录或客户端 JSON 目录之一。");
                            if (!string.IsNullOrEmpty(clientCode))
                            {
                                Directory.CreateDirectory(clientCode);
                                WriteClientCode(table, clientNs, clientCode, result);
                                result.Messages.Add("[OK][Client] " + table.TableName + " → code");
                            }
                            if (!string.IsNullOrEmpty(clientBytes))
                            {
                                Directory.CreateDirectory(clientBytes);
                                primaryBytesPath = Path.Combine(clientBytes, table.TableName + ".bytes");
                                File.WriteAllBytes(primaryBytesPath, bytes);
                                result.GeneratedBinaryFiles.Add(primaryBytesPath);
                                result.Messages.Add("[OK][Client] " + table.TableName + " → bytes");
                            }
                            MaybeWriteJson(table, options.ExportJson, clientJson, result, "Client");
                            if (options.ExportJson && string.IsNullOrEmpty(clientJson) && !string.IsNullOrEmpty(serverJson))
                                result.Messages.Add("[提示] Client 表 " + table.TableName + " 不会写入「服务器 JSON 目录」；请填「客户端 JSON 目录」或把表放到 Shared/Server");
                            break;

                        case TableTarget.Server:
                            if (string.IsNullOrEmpty(serverCode) && string.IsNullOrEmpty(serverBytes)
                                && !(options.ExportJson && !string.IsNullOrEmpty(serverJson)))
                                throw new CompileException(table.SourceFile, table.TableName, null, null, null,
                                    "Server 表请至少填写：服务器代码目录、bytes 目录或服务器 JSON 目录之一。");
                            if (!string.IsNullOrEmpty(serverCode))
                            {
                                Directory.CreateDirectory(serverCode);
                                WriteServerCode(table, serverNs, options.UseFrozenDictionary, serverCode, result);
                                result.Messages.Add("[OK][Server] " + table.TableName + " → code");
                            }
                            if (!string.IsNullOrEmpty(serverBytes))
                            {
                                Directory.CreateDirectory(serverBytes);
                                primaryBytesPath = Path.Combine(serverBytes, table.TableName + ".bytes");
                                File.WriteAllBytes(primaryBytesPath, bytes);
                                result.GeneratedBinaryFiles.Add(primaryBytesPath);
                                result.Messages.Add("[OK][Server] " + table.TableName + " → bytes");
                            }
                            MaybeWriteJson(table, options.ExportJson, serverJson, result, "Server");
                            if (options.ExportJson && string.IsNullOrEmpty(serverJson) && !string.IsNullOrEmpty(clientJson))
                                result.Messages.Add("[提示] Server 表 " + table.TableName + " 不会写入「客户端 JSON 目录」；请填「服务器 JSON 目录」");
                            break;

                        case TableTarget.Shared:
                        default:
                            // Shared：两端各写一份代码；同一份 bytes 各写一份（不另建 shared 目录）
                            // JSON 与代码目录解耦：只要配置了对应 JsonDir 且 ExportJson，就会写出
                            if (string.IsNullOrEmpty(clientCode) && string.IsNullOrEmpty(serverCode)
                                && !(options.ExportJson && (!string.IsNullOrEmpty(clientJson) || !string.IsNullOrEmpty(serverJson))))
                                throw new CompileException(table.SourceFile, table.TableName, null, null, null,
                                    "Shared 表至少需要客户端/服务器代码目录，或开启 ExportJson 并填写 JSON 目录。");
                            if (!string.IsNullOrEmpty(clientCode))
                            {
                                Directory.CreateDirectory(clientCode);
                                WriteClientCode(table, clientNs, clientCode, result);
                                result.Messages.Add("[OK][Shared/Client] " + table.TableName + " → code");
                            }
                            if (!string.IsNullOrEmpty(clientBytes))
                            {
                                Directory.CreateDirectory(clientBytes);
                                var cb = Path.Combine(clientBytes, table.TableName + ".bytes");
                                File.WriteAllBytes(cb, bytes);
                                result.GeneratedBinaryFiles.Add(cb);
                                primaryBytesPath = cb;
                                result.Messages.Add("[OK][Shared/Client] " + table.TableName + " → bytes");
                            }
                            if (!string.IsNullOrEmpty(serverCode))
                            {
                                Directory.CreateDirectory(serverCode);
                                WriteServerCode(table, serverNs, options.UseFrozenDictionary, serverCode, result);
                                result.Messages.Add("[OK][Shared/Server] " + table.TableName + " → code");
                            }
                            if (!string.IsNullOrEmpty(serverBytes))
                            {
                                Directory.CreateDirectory(serverBytes);
                                var sb = Path.Combine(serverBytes, table.TableName + ".bytes");
                                File.WriteAllBytes(sb, bytes);
                                result.GeneratedBinaryFiles.Add(sb);
                                if (primaryBytesPath == null) primaryBytesPath = sb;
                                result.Messages.Add("[OK][Shared/Server] " + table.TableName + " → bytes");
                            }
                            // JSON：按目录分别写，不依赖是否生成了该端代码
                            if (options.ExportJson)
                            {
                                if (!string.IsNullOrEmpty(clientJson))
                                    MaybeWriteJson(table, true, clientJson, result, "Client");
                                if (!string.IsNullOrEmpty(serverJson))
                                    MaybeWriteJson(table, true, serverJson, result, "Server");
                                if (string.IsNullOrEmpty(clientJson) && string.IsNullOrEmpty(serverJson))
                                    result.Messages.Add("[跳过][Shared] " + table.TableName + " → ExportJson 已开但未配置任何 JSON 目录");
                            }
                            result.Messages.Add("[OK][Shared] " + table.TableName + " → 已写到已配置的端（同一 wire format）");
                            break;
                    }

                    result.TableCount++;
                    result.TotalRows += table.Rows.Count;

                    manifestEntries.Add(new ManifestWriter.Entry
                    {
                        TableName = table.TableName,
                        Target = item.Target.ToString(),
                        RelativePath = item.RelativePath,
                        ContentSha256 = primaryBytesPath != null ? ManifestWriter.Sha256File(primaryBytesPath) : "",
                        RowCount = table.Rows.Count,
                        FieldCount = table.Fields.Count,
                    });
                }
            }

            string manifestPath = options.ManifestPath;
            if (string.IsNullOrWhiteSpace(manifestPath))
                manifestPath = Path.Combine(inputPath, "tables.lock.json");
            else
                manifestPath = Path.GetFullPath(manifestPath);

            ManifestWriter.Write(manifestPath, manifestEntries);
            result.ManifestPath = manifestPath;
            result.Messages.Add("[OK] manifest → " + manifestPath);

            return result;
        }


        private static void MaybeWriteJson(TableDef table, bool exportJson, string jsonDir, Result result, string tag)
        {
            if (!exportJson) return;
            if (string.IsNullOrEmpty(jsonDir))
            {
                result.Messages.Add("[跳过][" + tag + "] " + table.TableName + " → 未配置 " + tag + " JSON 目录，跳过 json");
                return;
            }
            Directory.CreateDirectory(jsonDir);
            var json = JsonGenerator.Generate(table);
            var path = Path.Combine(jsonDir, table.TableName + ".json");
            File.WriteAllText(path, json, System.Text.Encoding.UTF8);
            result.GeneratedJsonFiles.Add(path);
            result.Messages.Add("[OK][" + tag + "] " + table.TableName + " → json  " + path);
        }

        private static void RequireDir(string dir, string message)
        {
            if (string.IsNullOrWhiteSpace(dir))
                throw new CompileException("", null, null, null, null, message);
        }

        private static void WriteClientCode(TableDef table, string ns, string codeDir, Result result)
        {
            var code = CSharpGenerator.Generate(table, ns);
            var codePath = Path.Combine(codeDir, table.TableName + ".cs");
            File.WriteAllText(codePath, code);
            result.GeneratedCodeFiles.Add(codePath);
        }

        private static void WriteServerCode(TableDef table, string ns, bool useFrozen, string codeDir, Result result)
        {
            var code = ServerCSharpGenerator.Generate(table, ns, useFrozen);
            var codePath = Path.Combine(codeDir, table.TableName + ".cs");
            File.WriteAllText(codePath, code);
            result.GeneratedCodeFiles.Add(codePath);
        }

        private sealed class ClassifiedFile
        {
            public string FullPath;
            public string RelativePath;
            public TableTarget Target;
        }

        private static List<ClassifiedFile> CollectClassifiedXlsx(string inputPath)
        {
            var list = new List<ClassifiedFile>();

            if (File.Exists(inputPath) && inputPath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                if (Path.GetFileName(inputPath).StartsWith("~$")) return list;
                if (!TryResolveTargetFromPath(inputPath, out var target, out var rel))
                    throw new CompileException(inputPath, null, null, null, null,
                        "单文件必须位于名为 Client、Server 或 Shared 的目录下。");
                list.Add(new ClassifiedFile { FullPath = Path.GetFullPath(inputPath), RelativePath = rel, Target = target });
                return list;
            }

            if (!Directory.Exists(inputPath))
                throw new CompileException(inputPath, null, null, null, null, "输入路径不存在。");

            string root = Path.GetFullPath(inputPath);
            foreach (var f in Directory.GetFiles(root, "*.xlsx", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(f).StartsWith("~$")) continue;
                var full = Path.GetFullPath(f);
                if (!TryResolveTargetFromPath(full, root, out var target, out var rel))
                {
                    throw new CompileException(full, null, null, null, null,
                        "xlsx 必须放在 Client/、Server/ 或 Shared/ 子目录中，禁止放在 Excel 根目录。");
                }
                list.Add(new ClassifiedFile { FullPath = full, RelativePath = rel, Target = target });
            }

            return list;
        }

        private static bool TryResolveTargetFromPath(string fullFilePath, out TableTarget target, out string relativePath)
        {
            var dir = Path.GetDirectoryName(fullFilePath);
            relativePath = Path.GetFileName(fullFilePath);
            while (!string.IsNullOrEmpty(dir))
            {
                var name = Path.GetFileName(dir);
                if (TryParseTargetFolder(name, out target))
                {
                    relativePath = Path.Combine(name, relativePath);
                    return true;
                }
                relativePath = Path.Combine(name, relativePath);
                var parent = Path.GetDirectoryName(dir);
                if (parent == dir) break;
                dir = parent;
            }
            target = TableTarget.Shared;
            return false;
        }

        private static bool TryResolveTargetFromPath(string fullFilePath, string root, out TableTarget target, out string relativePath)
        {
            relativePath = fullFilePath;
            if (fullFilePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                relativePath = fullFilePath.Substring(root.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var parts = relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (TryParseTargetFolder(p, out target))
                    return true;
            }

            var rootName = Path.GetFileName(root.TrimEnd('/', '\\'));
            if (TryParseTargetFolder(rootName, out target))
            {
                relativePath = Path.Combine(rootName, relativePath);
                return true;
            }

            target = TableTarget.Shared;
            return false;
        }

        private static bool TryParseTargetFolder(string name, out TableTarget target)
        {
            if (string.Equals(name, "Client", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "c", StringComparison.OrdinalIgnoreCase))
            {
                target = TableTarget.Client;
                return true;
            }
            if (string.Equals(name, "Server", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "s", StringComparison.OrdinalIgnoreCase))
            {
                target = TableTarget.Server;
                return true;
            }
            if (string.Equals(name, "Shared", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(name, "Common", StringComparison.OrdinalIgnoreCase))
            {
                target = TableTarget.Shared;
                return true;
            }
            target = TableTarget.Shared;
            return false;
        }

        private static void EnsureEpplusLicense()
        {
            try
            {
                var packageType = Type.GetType("OfficeOpenXml.ExcelPackage, EPPlus")
                    ?? typeof(OfficeOpenXml.ExcelPackage);

                var licenseContextProp = packageType.GetProperty("LicenseContext",
                    BindingFlags.Public | BindingFlags.Static);
                if (licenseContextProp != null && licenseContextProp.CanWrite)
                {
                    var enumType = licenseContextProp.PropertyType;
                    object nonCommercial = Enum.Parse(enumType, "NonCommercial");
                    licenseContextProp.SetValue(null, nonCommercial, null);
                    return;
                }

                var licenseProp = packageType.GetProperty("License",
                    BindingFlags.Public | BindingFlags.Static);
                if (licenseProp != null)
                {
                    var licenseObj = licenseProp.GetValue(null, null);
                    if (licenseObj != null)
                    {
                        var method = licenseObj.GetType().GetMethod("SetNonCommercialPersonal",
                            new Type[] { typeof(string) });
                        if (method != null)
                        {
                            method.Invoke(licenseObj, new object[] { "ExcelConfigCompiler" });
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[ExcelConfigCompiler] EPPlus license setup failed: " + ex.Message);
            }
        }
    }
}
