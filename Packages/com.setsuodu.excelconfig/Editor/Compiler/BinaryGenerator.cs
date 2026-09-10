using System;
using ExcelConfigCompiler.Runtime;

namespace ExcelConfigCompiler.Compiler
{
    /// <summary>
    /// TableDef → .bytes 二进制数据。
    /// 与 Runtime 的 ByteReader / BinaryFormat 严格对称。
    /// </summary>
    public static class BinaryGenerator
    {
        public static byte[] Generate(TableDef table)
        {
            var writer = new ByteWriter();
            writer.WriteMagic(BinaryFormat.Magic);
            writer.WriteInt32(BinaryFormat.CurrentVersion);
            writer.WriteInt32(table.Rows.Count);

            foreach (var row in table.Rows)
            {
                for (int i = 0; i < table.Fields.Count; i++)
                {
                    var field = table.Fields[i];
                    var value = row[i];
                    WriteField(writer, field.Type, value);
                }
            }

            return writer.ToArray();
        }

        private static void WriteField(ByteWriter writer, FieldType type, object value)
        {
            switch (type)
            {
                case FieldType.Byte: writer.WriteByte(Convert.ToByte(value ?? 0)); break;
                case FieldType.SByte: writer.WriteSByte(Convert.ToSByte(value ?? 0)); break;
                case FieldType.Int16: writer.WriteInt16(Convert.ToInt16(value ?? 0)); break;
                case FieldType.UInt16: writer.WriteUInt16(Convert.ToUInt16(value ?? 0)); break;
                case FieldType.Int32: writer.WriteInt32(Convert.ToInt32(value ?? 0)); break;
                case FieldType.UInt32: writer.WriteUInt32(Convert.ToUInt32(value ?? 0)); break;
                case FieldType.Int64: writer.WriteInt64(Convert.ToInt64(value ?? 0)); break;
                case FieldType.UInt64: writer.WriteUInt64(Convert.ToUInt64(value ?? 0)); break;
                case FieldType.Single: writer.WriteSingle(Convert.ToSingle(value ?? 0f)); break;
                case FieldType.Double: writer.WriteDouble(Convert.ToDouble(value ?? 0d)); break;
                case FieldType.Boolean: writer.WriteBoolean(Convert.ToBoolean(value ?? false)); break;
                case FieldType.String: writer.WriteString(value as string); break;

                case FieldType.ByteArray: writer.WriteByteArray(value as byte[]); break;
                case FieldType.Int32Array: writer.WriteInt32Array(value as int[]); break;
                case FieldType.SingleArray: writer.WriteSingleArray(value as float[]); break;
                case FieldType.StringArray: writer.WriteStringArray(value as string[]); break;

                default:
                    throw new NotSupportedException("BinaryGenerator 未支持类型: " + type);
            }
        }
    }
}
