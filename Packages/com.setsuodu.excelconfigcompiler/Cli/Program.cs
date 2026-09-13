using System;
using System.IO;
using ExcelConfigCompiler.Compiler;

namespace ExcelConfigCompiler.Cli
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 1 || args[0] is "-h" or "--help" or "/?")
            {
                PrintHelp();
                return args.Length < 1 ? 1 : 0;
            }

            string excelRoot = null;
            string clientCode = null, clientBytes = null, clientNs = "Game.Config";
            string serverCode = null, serverBytes = null, serverNs = "Game.Server.Config";
            bool useFrozen = true;
            string manifest = null;

            // 兼容旧用法: <excel> <outputDir> [-n ns]
            if (args.Length >= 2 && !args[1].StartsWith("-"))
            {
                excelRoot = args[0];
                string outRoot = args[1];
                clientCode = Path.Combine(outRoot, "client", "Generated");
                clientBytes = Path.Combine(outRoot, "client", "Tables");
                serverCode = Path.Combine(outRoot, "server", "Generated");
                serverBytes = Path.Combine(outRoot, "server", "Tables");
                manifest = Path.Combine(outRoot, "tables.lock.json");
                for (int i = 2; i < args.Length; i++)
                    ParseFlag(args, ref i, ref clientNs, ref serverNs, ref useFrozen, ref clientCode, ref clientBytes, ref serverCode, ref serverBytes, ref manifest);
            }
            else
            {
                excelRoot = args[0];
                for (int i = 1; i < args.Length; i++)
                    ParseFlag(args, ref i, ref clientNs, ref serverNs, ref useFrozen, ref clientCode, ref clientBytes, ref serverCode, ref serverBytes, ref manifest);
            }

            try
            {
                if (!Directory.Exists(excelRoot) && !File.Exists(excelRoot))
                {
                    Console.Error.WriteLine($"[错误] Excel 路径不存在: {excelRoot}");
                    return 1;
                }

                var result = CompilePipeline.Compile(new CompilePipeline.Options
                {
                    ExcelRoot = excelRoot,
                    ClientCodeDir = clientCode,
                    ClientBytesDir = clientBytes,
                    ClientNamespace = clientNs,
                    ServerCodeDir = serverCode,
                    ServerBytesDir = serverBytes,
                    ServerNamespace = serverNs,
                    UseFrozenDictionary = useFrozen,
                    ManifestPath = manifest,
                });

                foreach (var msg in result.Messages)
                    Console.WriteLine(msg);

                Console.WriteLine($"完成：{result.TableCount} 张表，共 {result.TotalRows} 行。");
                if (!string.IsNullOrEmpty(clientCode)) Console.WriteLine($"客户端代码: {clientCode}");
                if (!string.IsNullOrEmpty(serverCode)) Console.WriteLine($"服务器代码: {serverCode}");
                if (!string.IsNullOrEmpty(result.ManifestPath)) Console.WriteLine($"Manifest: {result.ManifestPath}");
                return 0;
            }
            catch (CompileException ex)
            {
                Console.Error.WriteLine("[编译错误] " + ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[异常] " + ex);
                return 2;
            }
        }

        private static void ParseFlag(
            string[] args, ref int i,
            ref string clientNs, ref string serverNs, ref bool useFrozen,
            ref string clientCode, ref string clientBytes,
            ref string serverCode, ref string serverBytes,
            ref string manifest)
        {
            string a = args[i];
            if (a is "-n" or "--namespace" or "--client-namespace" && i + 1 < args.Length)
                clientNs = args[++i];
            else if (a is "--server-namespace" && i + 1 < args.Length)
                serverNs = args[++i];
            else if (a is "--client-code" && i + 1 < args.Length)
                clientCode = args[++i];
            else if (a is "--client-bytes" && i + 1 < args.Length)
                clientBytes = args[++i];
            else if (a is "--server-code" && i + 1 < args.Length)
                serverCode = args[++i];
            else if (a is "--server-bytes" && i + 1 < args.Length)
                serverBytes = args[++i];
            else if (a is "--manifest" && i + 1 < args.Length)
                manifest = args[++i];
            else if (a is "--no-frozen-dict" or "--no-frozen")
                useFrozen = false;
            else
                throw new ArgumentException("未知参数: " + a);
        }

        private static void PrintHelp()
        {
            Console.WriteLine(@"ExcelConfigCompiler — Client / Server / Shared 分流导表

推荐用法（路径完全分开）:
  ExcelConfigCompiler <Excel根> \
    --client-code <客户端cs目录> --client-bytes <客户端bytes目录> \
    --server-code <服务器cs目录> --server-bytes <服务器bytes目录> \
    -n Game.Config --server-namespace Game.Server.Config

兼容旧用法（仍会在 output 下建 client/ server 子目录）:
  ExcelConfigCompiler <Excel根> <outputDir> -n Game.Config

选项:
  -n, --client-namespace   客户端命名空间
  --server-namespace       服务器命名空间
  --client-code / --client-bytes
  --server-code / --server-bytes
  --manifest <path>        tables.lock.json 路径（默认写在 Excel 根）
  --no-frozen-dict         服务器索引不用 FrozenDictionary
");
        }
    }
}
