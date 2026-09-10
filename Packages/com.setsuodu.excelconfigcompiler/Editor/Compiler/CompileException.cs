using System;

namespace ExcelConfigCompiler.Compiler
{
    /// <summary>
    /// 编译期错误。统一带上 文件/Sheet/Row/Column/Field 定位信息，
    /// 报错格式始终是 "xxx.xlsx / Sheet xxx / Row n / Column field: message"。
    /// </summary>
    public sealed class CompileException : Exception
    {
        public CompileException(
            string file, string sheet, int? row, int? column, string field, string message)
            : base(Format(file, sheet, row, column, field, message))
        {
        }

        private static string Format(string file, string sheet, int? row, int? column, string field, string message)
        {
            var loc = file;
            if (sheet != null) loc += " / Sheet " + sheet;
            if (row != null) loc += " / Row " + row;
            if (field != null) loc += " / Column " + field;
            else if (column != null) loc += " / Column #" + column;
            return loc + ": " + message;
        }
    }
}
