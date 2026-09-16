using System;
using System.Globalization;
using System.Text;
using ExcelConfigCompiler.Runtime;

namespace ExcelConfigCompiler.Compiler
{
    /// <summary>
    /// TableDef → JSON 字符串（数组 of object）。
    /// 字段名使用 CSharpName，与生成的 ReadJson switch 一致。
    /// </summary>
    public static class JsonGenerator
    {
        public static string Generate(TableDef table)
        {
            var w = new JsonWriter();
            w.BeginArray();
            for (int r = 0; r < table.Rows.Count; r++)
            {
                if (r > 0) w.Comma();
                w.BeginObject();
                var row = table.Rows[r];
                for (int i = 0; i < table.Fields.Count; i++)
                {
                    if (i > 0) w.Comma();
                    var f = table.Fields[i];
                    w.WritePropertyName(f.CSharpName);
                    WriteValue(w, f.Type, row[i]);
                }
                w.EndObject();
            }
            w.EndArray();
            return w.ToString();
        }

        private static void WriteValue(JsonWriter w, FieldType type, object value)
        {
            switch (type)
            {
                case FieldType.String:
                    w.WriteString(value as string);
                    break;
                case FieldType.Boolean:
                    w.WriteBoolean(Convert.ToBoolean(value ?? false));
                    break;
                case FieldType.Byte:
                case FieldType.SByte:
                case FieldType.Int16:
                case FieldType.UInt16:
                case FieldType.Int32:
                case FieldType.UInt32:
                case FieldType.Int64:
                case FieldType.UInt64:
                    w.WriteNumber(Convert.ToInt64(value ?? 0L));
                    break;
                case FieldType.Single:
                case FieldType.Double:
                    w.WriteNumber(Convert.ToDouble(value ?? 0d));
                    break;
                case FieldType.Int32Array:
                    WriteIntArray(w, value as int[]);
                    break;
                case FieldType.ByteArray:
                    WriteByteArray(w, value as byte[]);
                    break;
                case FieldType.SingleArray:
                    WriteFloatArray(w, value as float[]);
                    break;
                case FieldType.StringArray:
                    WriteStringArray(w, value as string[]);
                    break;
                default:
                    throw new NotSupportedException("JsonGenerator 未支持类型: " + type);
            }
        }

        private static void WriteIntArray(JsonWriter w, int[] arr)
        {
            if (arr == null) { w.WriteNull(); return; }
            w.BeginArray();
            for (int i = 0; i < arr.Length; i++)
            {
                if (i > 0) w.Comma();
                w.WriteNumber(arr[i]);
            }
            w.EndArray();
        }

        private static void WriteByteArray(JsonWriter w, byte[] arr)
        {
            if (arr == null) { w.WriteNull(); return; }
            w.BeginArray();
            for (int i = 0; i < arr.Length; i++)
            {
                if (i > 0) w.Comma();
                w.WriteNumber(arr[i]);
            }
            w.EndArray();
        }

        private static void WriteFloatArray(JsonWriter w, float[] arr)
        {
            if (arr == null) { w.WriteNull(); return; }
            w.BeginArray();
            for (int i = 0; i < arr.Length; i++)
            {
                if (i > 0) w.Comma();
                w.WriteNumber(arr[i]);
            }
            w.EndArray();
        }

        private static void WriteStringArray(JsonWriter w, string[] arr)
        {
            if (arr == null) { w.WriteNull(); return; }
            w.BeginArray();
            for (int i = 0; i < arr.Length; i++)
            {
                if (i > 0) w.Comma();
                w.WriteString(arr[i]);
            }
            w.EndArray();
        }
    }
}
