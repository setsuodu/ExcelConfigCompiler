using UnityEngine;

namespace ExcelConfigCompiler.Editor
{
    /// <summary>
    /// 导表路径配置。可放在 Assets 任意位置，EditorWindow 会自动查找或创建。
    /// </summary>
    public class ExcelConfigSettings : ScriptableObject
    {
        [Header("输入")]
        [Tooltip("Excel 源表目录（可放多个 .xlsx）")]
        public string ExcelSourceFolder = "Assets/Config/Excel";

        [Header("输出")]
        [Tooltip("生成的 C# 代码输出目录")]
        public string GeneratedCodeFolder = "Assets/Config/Generated";

        [Tooltip("生成的 .bytes 二进制输出目录")]
        public string GeneratedBytesFolder = "Assets/Config/Tables";

        [Header("代码生成")]
        [Tooltip("生成代码的命名空间")]
        public string Namespace = "Game.Config";

        public const string DefaultAssetPath = "Assets/ExcelConfigCompilerSettings.asset";
    }
}
