using System.Collections.Generic;

namespace ExcelConfigCompiler.Compiler
{
    /// <summary>
    /// 支持的字段类型。这一层是语言无关的（不属于 C# CodeGen），
    /// 未来如果要加 Go / C++ / Python 等 Generator，直接消费这个枚举即可。
    /// 多语言目前只预留，不做具体实现。
    /// </summary>
    public enum FieldType
    {
        Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64,
        Single, Double, Boolean, String,
        Int32Array, ByteArray, SingleArray, StringArray,
        // 预留：Enum, Dictionary, Ref 等后续扩展
    }

    public sealed class FieldDef
    {
        public string ExcelName;   // Excel 表头原名
        public string CSharpName;  // 生成的 C# 成员名（PascalCase）
        public FieldType Type;
        public int ColumnIndex;    // 1-based，用于错误定位
    }

    public sealed class TableDef
    {
        public string TableName;   // Worksheet 名 -> C# 类型名
        public string SourceFile;
        public List<FieldDef> Fields;
        public List<object[]> Rows; // 每行按 Fields 顺序存放已解析好的值

        /// <summary>
        /// 第一列名为 "id"（大小写不敏感）且是整数类型时，视为主键，允许生成 Get(id) 索引。
        /// </summary>
        public FieldDef IdField
        {
            get
            {
                if (Fields == null || Fields.Count == 0) return null;
                var first = Fields[0];
                if (!string.Equals(first.ExcelName, "id", System.StringComparison.OrdinalIgnoreCase))
                    return null;
                if (!IsIntegerType(first.Type)) return null;
                return first;
            }
        }

        private static bool IsIntegerType(FieldType t)
        {
            return t == FieldType.Byte || t == FieldType.SByte || t == FieldType.Int16 || t == FieldType.UInt16
                || t == FieldType.Int32 || t == FieldType.UInt32 || t == FieldType.Int64 || t == FieldType.UInt64;
        }
    }
}
