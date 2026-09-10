using System;
using System.IO;
using ExcelConfigCompiler.Compiler;

namespace ExcelConfigCompiler.Cli
{
    /// <summary>
    /// 外部 / CI 使用的单体 CLI。
    /// 多语言目前只预留（只生成 C#），后续可扩展 -lang 参数。
    /// </summary>
    internal static class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length < 2 || args[0] is "-h" or "--help" or "/?")
            {
                PrintHelp();
                return args.Length < 2 ? 1 : 0;
            }

            string input = args[0];
            string output = args[1];
            string ns = "Config";
            // 预留：string lang = "csharp";

            for (int i = 2; i < args.Length; i++)
            {
                if (args[i] is "-n" or "--namespace" && i + 1 < args.Length)
                {
                    ns = args[++i];
                }
                // 预留多语言
                // else if (args[i] is "-l" or "--lang" && i + 1 < args.Length)
                // {
                //     lang = args[++i];
                // }
                else
                {
                    Console.Error.WriteLine($"未知参数: {args[i]}");
                    PrintHelp();
                    return 1;
                }
            }

            try
            {
                if (!File.Exists(input) && !Directory.Exists(input))
                {
                    Console.Error.WriteLine($"[错误] 输入路径不存在: {input}");
                    return 1;
                }

                var result = CompilePipeline.Compile(input, output, ns);

                foreach (var msg in result.Messages)
                    Console.WriteLine(msg);

                Console.WriteLine($"完成：{result.TableCount} 张表，共 {result.TotalRows} 行。");
                Console.WriteLine($"代码目录: {Path.Combine(output, "Generated")}");
                Console.WriteLine($"二进制目录: {Path.Combine(output, "Tables")}");
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

        private static void PrintHelp()
        {
            Console.WriteLine(@"ExcelConfigCompiler - Excel → C# + Binary 配置编译器

用法:
  ExcelConfigCompiler <输入.xlsx|目录> <输出目录> [选项]

选项:
  -n, --namespace <ns>   生成代码的命名空间（默认 Config）
  -h, --help             显示帮助

示例:
  ExcelConfigCompiler ./Tables ./Output -n Game.Config
  ExcelConfigCompiler ./Item.xlsx ./Output

说明:
  - 输入可以是单个 xlsx，也可以是包含多个 xlsx 的目录
  - 会在输出目录下生成 Generated/*.cs 和 Tables/*.bytes
  - 多语言目前只预留，当前仅生成 C#
");
        }
    }
}
