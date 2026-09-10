using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ExcelConfigCompiler.Compiler
{
    public static class Validator
    {
        private static readonly Regex IdentifierPattern = new Regex(
            @"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);

        private static readonly HashSet<string> CSharpKeywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract","as","base","bool","break","byte","case","catch","char","checked","class","const",
            "continue","decimal","default","delegate","do","double","else","enum","event","explicit","extern",
            "false","finally","fixed","float","for","foreach","goto","if","implicit","in","int","interface",
            "internal","is","lock","long","namespace","new","null","object","operator","out","override",
            "params","private","protected","public","readonly","ref","return","sbyte","sealed","short",
            "sizeof","stackalloc","static","string","struct","switch","this","throw","true","try","typeof",
            "uint","ulong","unchecked","unsafe","ushort","using","virtual","void","volatile","while"
        };

        public static void Validate(TableDef table)
        {
            if (string.IsNullOrWhiteSpace(table.TableName) ||
                !IdentifierPattern.IsMatch(table.TableName) ||
                CSharpKeywords.Contains(table.TableName))
            {
                throw new CompileException(table.SourceFile, table.TableName, null, null, null,
                    $"Worksheet 名 '{table.TableName}' 不是合法的 C# 类型名，请改成字母/下划线开头，且不是关键字。");
            }

            ValidateFieldNames(table);
            ValidateIds(table);
        }

        private static void ValidateFieldNames(TableDef table)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var field in table.Fields)
            {
                if (!IdentifierPattern.IsMatch(field.CSharpName) || CSharpKeywords.Contains(field.CSharpName))
                    throw new CompileException(table.SourceFile, table.TableName, 1, field.ColumnIndex, field.ExcelName,
                        $"生成的成员名 '{field.CSharpName}' 不是合法的 C# 标识符，请改一下 Excel 表头。");

                if (!seen.Add(field.CSharpName))
                    throw new CompileException(table.SourceFile, table.TableName, 1, field.ColumnIndex, field.ExcelName,
                        $"字段名 '{field.CSharpName}' 重复（可能是不同大小写的表头撞到一起了）。");
            }
        }

        private static void ValidateIds(TableDef table)
        {
            var idField = table.IdField;
            if (idField == null) return;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int r = 0; r < table.Rows.Count; r++)
            {
                var idValue = table.Rows[r][0];
                var key = idValue?.ToString() ?? "";
                if (!seen.Add(key))
                    throw new CompileException(table.SourceFile, table.TableName, r + 3, idField.ColumnIndex, idField.ExcelName,
                        $"重复的 id 值 '{key}'。");
            }
        }
    }
}
