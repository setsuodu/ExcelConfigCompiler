using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using OfficeOpenXml;

namespace ExcelConfigCompiler.Compiler
{
    /// <summary>
    /// 用 EPPlus 读 xlsx，转成语言无关的 TableDef。
    /// 约定：第1行字段名，第2行字段类型，第3行起是数据；Worksheet 名 = 生成的类型名。
    /// 只在 Compiler（编辑器/命令行）阶段使用，产物不含 EPPlus 依赖。
    /// </summary>
    public static class ExcelParser
    {
        public static List<TableDef> ParseWorkbook(string xlsxPath)
        {
            using (var package = new ExcelPackage(new FileInfo(xlsxPath)))
            {
                var tables = new List<TableDef>();

                foreach (var sheet in package.Workbook.Worksheets)
                {
                    if (sheet.Dimension == null)
                        throw new CompileException(xlsxPath, sheet.Name, null, null, null, "Worksheet 是空的，没有任何数据。");
                    tables.Add(ParseSheet(xlsxPath, sheet));
                }

                if (tables.Count == 0)
                    throw new CompileException(xlsxPath, null, null, null, null, "文件里没有任何 Worksheet。");

                return tables;
            }
        }

        private static TableDef ParseSheet(string file, ExcelWorksheet sheet)
        {
            int lastCol = sheet.Dimension.End.Column;
            int lastRow = sheet.Dimension.End.Row;

            if (lastRow < 2)
                throw new CompileException(file, sheet.Name, null, null, null,
                    "至少需要 字段名行 + 类型行 两行，当前不足。");

            var fields = new List<FieldDef>();
            for (int col = 1; col <= lastCol; col++)
            {
                string excelName = (sheet.Cells[1, col].Text ?? "").Trim();
                if (string.IsNullOrEmpty(excelName)) continue;

                string typeText = (sheet.Cells[2, col].Text ?? "").Trim();
                if (string.IsNullOrEmpty(typeText))
                    throw new CompileException(file, sheet.Name, 2, col, excelName, "缺少类型声明。");

                var fieldType = ParseFieldType(typeText, file, sheet.Name, col, excelName);
                fields.Add(new FieldDef
                {
                    ExcelName = excelName,
                    CSharpName = ToPascalCase(excelName),
                    Type = fieldType,
                    ColumnIndex = col
                });
            }

            if (fields.Count == 0)
                throw new CompileException(file, sheet.Name, null, null, null, "没有任何有效字段。");

            var rows = new List<object[]>();
            for (int row = 3; row <= lastRow; row++)
            {
                var values = new object[fields.Count];
                bool allEmpty = true;
                for (int i = 0; i < fields.Count; i++)
                {
                    var field = fields[i];
                    string raw = (sheet.Cells[row, field.ColumnIndex].Text ?? "").Trim();
                    if (!string.IsNullOrEmpty(raw)) allEmpty = false;
                    values[i] = ParseValue(raw, field, file, sheet.Name, row);
                }
                if (allEmpty) continue;
                rows.Add(values);
            }

            return new TableDef
            {
                TableName = SanitizeTypeName(sheet.Name),
                SourceFile = file,
                Fields = fields,
                Rows = rows
            };
        }

        private static FieldType ParseFieldType(string text, string file, string sheet, int col, string excelName)
        {
            text = text.Trim();
            bool isArray = text.EndsWith("[]", StringComparison.Ordinal);
            if (isArray) text = text.Substring(0, text.Length - 2).Trim();

            string key = text.ToLowerInvariant();
            FieldType baseType;
            switch (key)
            {
                case "byte": baseType = FieldType.Byte; break;
                case "sbyte": baseType = FieldType.SByte; break;
                case "short":
                case "int16": baseType = FieldType.Int16; break;
                case "ushort":
                case "uint16": baseType = FieldType.UInt16; break;
                case "int":
                case "int32": baseType = FieldType.Int32; break;
                case "uint":
                case "uint32": baseType = FieldType.UInt32; break;
                case "long":
                case "int64": baseType = FieldType.Int64; break;
                case "ulong":
                case "uint64": baseType = FieldType.UInt64; break;
                case "float":
                case "single": baseType = FieldType.Single; break;
                case "double": baseType = FieldType.Double; break;
                case "bool":
                case "boolean": baseType = FieldType.Boolean; break;
                case "string":
                case "str": baseType = FieldType.String; break;
                default:
                    throw new CompileException(file, sheet, 2, col, excelName, "不支持的类型 '" + text + "'。");
            }

            if (!isArray) return baseType;

            switch (baseType)
            {
                case FieldType.Int32: return FieldType.Int32Array;
                case FieldType.Byte: return FieldType.ByteArray;
                case FieldType.Single: return FieldType.SingleArray;
                case FieldType.String: return FieldType.StringArray;
                default:
                    throw new CompileException(file, sheet, 2, col, excelName,
                        "类型 '" + text + "[]' 暂不支持数组（目前仅 int[] / byte[] / float[] / string[]）。");
            }
        }

        private static object ParseValue(string raw, FieldDef field, string file, string sheetName, int row)
        {
            try
            {
                switch (field.Type)
                {
                    case FieldType.Byte: return raw.Length == 0 ? (byte)0 : byte.Parse(raw, CultureInfo.InvariantCulture);
                    case FieldType.SByte: return raw.Length == 0 ? (sbyte)0 : sbyte.Parse(raw, CultureInfo.InvariantCulture);
                    case FieldType.Int16: return raw.Length == 0 ? (short)0 : short.Parse(raw, CultureInfo.InvariantCulture);
                    case FieldType.UInt16: return raw.Length == 0 ? (ushort)0 : ushort.Parse(raw, CultureInfo.InvariantCulture);
                    case FieldType.Int32: return raw.Length == 0 ? 0 : int.Parse(raw, CultureInfo.InvariantCulture);
                    case FieldType.UInt32: return raw.Length == 0 ? 0u : uint.Parse(raw, CultureInfo.InvariantCulture);
                    case FieldType.Int64: return raw.Length == 0 ? 0L : long.Parse(raw, CultureInfo.InvariantCulture);
                    case FieldType.UInt64: return raw.Length == 0 ? 0UL : ulong.Parse(raw, CultureInfo.InvariantCulture);
                    case FieldType.Single: return raw.Length == 0 ? 0f : float.Parse(raw, CultureInfo.InvariantCulture);
                    case FieldType.Double: return raw.Length == 0 ? 0d : double.Parse(raw, CultureInfo.InvariantCulture);
                    case FieldType.Boolean: return ParseBool(raw);
                    case FieldType.String: return raw;

                    case FieldType.Int32Array: return ParseIntArray(raw);
                    case FieldType.ByteArray: return ParseByteArray(raw);
                    case FieldType.SingleArray: return ParseFloatArray(raw);
                    case FieldType.StringArray: return ParseStringArray(raw);

                    default:
                        throw new InvalidOperationException("未处理的类型 " + field.Type);
                }
            }
            catch (Exception ex)
            {
                if (ex is CompileException) throw;
                throw new CompileException(file, sheetName, row, field.ColumnIndex, field.ExcelName,
                    "值 '" + raw + "' 无法转换为 " + field.Type + "。" + ex.Message);
            }
        }

        private static bool ParseBool(string raw)
        {
            if (raw.Length == 0) return false;
            string lower = raw.ToLowerInvariant();
            if (lower == "1" || lower == "true" || lower == "yes") return true;
            if (lower == "0" || lower == "false" || lower == "no") return false;
            return bool.Parse(raw);
        }

        private static int[] ParseIntArray(string raw)
        {
            if (raw.Length == 0) return new int[0];
            var parts = raw.Split(',');
            var arr = new int[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                arr[i] = int.Parse(parts[i].Trim(), CultureInfo.InvariantCulture);
            return arr;
        }

        private static byte[] ParseByteArray(string raw)
        {
            if (raw.Length == 0) return new byte[0];
            var parts = raw.Split(',');
            var arr = new byte[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                arr[i] = byte.Parse(parts[i].Trim(), CultureInfo.InvariantCulture);
            return arr;
        }

        private static float[] ParseFloatArray(string raw)
        {
            if (raw.Length == 0) return new float[0];
            var parts = raw.Split(',');
            var arr = new float[parts.Length];
            for (int i = 0; i < parts.Length; i++)
                arr[i] = float.Parse(parts[i].Trim(), CultureInfo.InvariantCulture);
            return arr;
        }

        private static string[] ParseStringArray(string raw)
        {
            if (raw.Length == 0) return new string[0];
            var parts = raw.Split(',');
            for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].Trim();
            return parts;
        }

        private static string ToPascalCase(string excelName)
        {
            var parts = excelName.Split(new[] { '_', ' ', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return excelName;

            var sb = new StringBuilder();
            foreach (var p in parts)
            {
                if (p.Length == 0) continue;
                sb.Append(char.ToUpperInvariant(p[0]));
                if (p.Length > 1) sb.Append(p.Substring(1));
            }
            return sb.ToString();
        }

        private static string SanitizeTypeName(string name)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (i == 0)
                {
                    if (char.IsLetter(c) || c == '_') sb.Append(c);
                    else sb.Append('_');
                }
                else
                {
                    if (char.IsLetterOrDigit(c) || c == '_') sb.Append(c);
                }
            }
            var result = sb.ToString();
            return string.IsNullOrEmpty(result) ? "Table" : result;
        }
    }
}
