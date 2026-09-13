using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ExcelConfigCompiler.Compiler
{
    /// <summary>
    /// 写出 tables.lock.json：表名 → 归属目录 / 相对路径 / 内容 hash。
    /// 便于 PR 中发现 Client↔Server↔Shared 误挪表。
    /// </summary>
    public static class ManifestWriter
    {
        public sealed class Entry
        {
            public string TableName;
            public string Target;      // Client | Server | Shared
            public string RelativePath;
            public string ContentSha256;
            public int RowCount;
            public int FieldCount;
        }

        public static void Write(string outputPath, IList<Entry> entries)
        {
            var dir = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            // 手写 JSON，避免依赖 System.Text.Json 版本差异（Unity Editor 兼容）
            var sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"version\": 1,");
            sb.AppendLine($"  \"generatedAt\": \"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\",");
            sb.AppendLine("  \"tables\": [");
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                sb.Append("    {");
                sb.Append($"\"table\": \"{Escape(e.TableName)}\", ");
                sb.Append($"\"target\": \"{Escape(e.Target)}\", ");
                sb.Append($"\"path\": \"{Escape(e.RelativePath.Replace('\\', '/'))}\", ");
                sb.Append($"\"sha256\": \"{Escape(e.ContentSha256)}\", ");
                sb.Append($"\"rows\": {e.RowCount}, ");
                sb.Append($"\"fields\": {e.FieldCount}");
                sb.Append("}");
                if (i < entries.Count - 1) sb.Append(',');
                sb.AppendLine();
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");
            File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
        }

        public static string Sha256File(string path)
        {
            using (var fs = File.OpenRead(path))
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(fs);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (var b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static string Escape(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
