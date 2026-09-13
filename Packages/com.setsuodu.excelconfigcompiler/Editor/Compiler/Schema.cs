using System.Collections.Generic;

namespace ExcelConfigCompiler.Compiler
{
    /// <summary>
    /// 支持的字段类型。这一层是语言无关的（不属于 C# CodeGen），
    /// 未来如果要加 Go / C++ / Python 等 Generator，直接消费这个枚举即可。
    /// </summary>
    public enum FieldType
    {
        Byte, SByte, Int16, UInt16, Int32, UInt32, Int64, UInt64,
        Single, Double, Boolean, String,
        Int32Array, ByteArray, SingleArray, StringArray,
    }

    /// <summary>
    /// 表归属目标。由 Excel 根下 Client / Server / Shared 目录决定。
    /// </summary>
    public enum TableTarget
    {
        /// <summary>仅客户端模板 + client bytes</summary>
        Client = 0,
        /// <summary>仅服务器模板 + server bytes</summary>
        Server = 1,
        /// <summary>两端模板各一份；bytes 同一份 wire format</summary>
        Shared = 2,
    }

    public sealed class FieldDef
    {
        public string ExcelName;
        public string CSharpName;
        public FieldType Type;
        public int ColumnIndex;
    }

    public sealed class TableDef
    {
        public string TableName;
        public string SourceFile;
        public List<FieldDef> Fields;
        public List<object[]> Rows;

        /// <summary>由相对 Excel 根的路径段 Client/Server/Shared 解析。</summary>
        public TableTarget Target = TableTarget.Shared;

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
