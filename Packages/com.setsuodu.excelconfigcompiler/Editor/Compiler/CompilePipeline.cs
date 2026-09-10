using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace ExcelConfigCompiler.Compiler
{
    /// <summary>
    /// 编译流水线统一入口。CLI 和 Unity Editor 都调用这里，避免两边逻辑分叉。
    /// </summary>
    public static class CompilePipeline
    {
        public sealed class Result
        {
            public int TableCount;
            public int TotalRows;
            public List<string> GeneratedCodeFiles = new List<string>();
            public List<string> GeneratedBinaryFiles = new List<string>();
            public List<string> Messages = new List<string>();
        }

        /// <summary>
        /// 编译一个或多个 xlsx（文件或目录）。
        /// </summary>
        public static Result Compile(string inputPath, string outputDir, string ns = "Config")
        {
            EnsureEpplusLicense();

            var result = new Result();
            var codeDir = Path.Combine(outputDir, "Generated");
            var dataDir = Path.Combine(outputDir, "Tables");
            Directory.CreateDirectory(codeDir);
            Directory.CreateDirectory(dataDir);

            var files = CollectXlsxFiles(inputPath);
            if (files.Count == 0)
                throw new CompileException(inputPath, null, null, null, null, "没有找到任何 .xlsx 文件。");

            foreach (var xlsx in files)
            {
                var tables = ExcelParser.ParseWorkbook(xlsx);
                foreach (var table in tables)
                {
                    Validator.Validate(table);

                    var code = CSharpGenerator.Generate(table, ns);
                    var codePath = Path.Combine(codeDir, table.TableName + ".cs");
                    File.WriteAllText(codePath, code);

                    var bytes = BinaryGenerator.Generate(table);
                    var bytesPath = Path.Combine(dataDir, table.TableName + ".bytes");
                    File.WriteAllBytes(bytesPath, bytes);

                    result.TableCount++;
                    result.TotalRows += table.Rows.Count;
                    result.GeneratedCodeFiles.Add(codePath);
                    result.GeneratedBinaryFiles.Add(bytesPath);
                    result.Messages.Add("[OK] " + table.TableName + ": " + table.Rows.Count + " 行 → " +
                        Path.GetFileName(codePath) + ", " + Path.GetFileName(bytesPath));
                }
            }

            return result;
        }

        private static List<string> CollectXlsxFiles(string inputPath)
        {
            var list = new List<string>();
            if (File.Exists(inputPath) && inputPath.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                list.Add(Path.GetFullPath(inputPath));
                return list;
            }

            if (Directory.Exists(inputPath))
            {
                foreach (var f in Directory.GetFiles(inputPath, "*.xlsx", SearchOption.AllDirectories))
                {
                    if (Path.GetFileName(f).StartsWith("~$")) continue;
                    list.Add(Path.GetFullPath(f));
                }
            }

            return list;
        }

        /// <summary>
        /// 用反射设置 EPPlus 授权，兼容 5/6/7/8 不同 API，避免编译期绑定到某个版本的成员。
        /// EPPlus 5~7: ExcelPackage.LicenseContext = LicenseContext.NonCommercial
        /// EPPlus 8+:   ExcelPackage.License.SetNonCommercialPersonal(...)
        /// </summary>
        private static void EnsureEpplusLicense()
        {
            try
            {
                var packageType = Type.GetType("OfficeOpenXml.ExcelPackage, EPPlus")
                    ?? typeof(OfficeOpenXml.ExcelPackage);

                // 方式1：LicenseContext（EPPlus 5 / 6 / 部分 7）
                var licenseContextProp = packageType.GetProperty("LicenseContext",
                    BindingFlags.Public | BindingFlags.Static);
                if (licenseContextProp != null && licenseContextProp.CanWrite)
                {
                    var enumType = licenseContextProp.PropertyType;
                    object nonCommercial = Enum.Parse(enumType, "NonCommercial");
                    licenseContextProp.SetValue(null, nonCommercial, null);
                    return;
                }

                // 方式2：License.SetNonCommercialPersonal（EPPlus 8+）
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
