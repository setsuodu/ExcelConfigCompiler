using UnityEngine;

namespace ExcelConfigCompiler.Editor
{
    /// <summary>
    /// 导表路径：Excel 根、客户端输出、服务器输出三者分开，不套 OutputRoot/client。
    /// </summary>
    public class ExcelConfigSettings : ScriptableObject
    {
        [Header("1. Excel 输入")]
        [Tooltip("Excel 根目录（其下必须有 Client / Server / Shared）")]
        public string ExcelSourceFolder = "Excel";

        [Header("2. 客户端输出")]
        [Tooltip("客户端生成代码命名空间")]
        public string ClientNamespace = "Game.Config";

        [Tooltip("客户端 .cs 输出目录（直接写入此目录，不再拼 /client）")]
        public string ClientCodeFolder = "Assets/Config/Generated";

        [Tooltip("客户端 .bytes 输出目录")]
        public string ClientBytesFolder = "Assets/Config/Tables";

        [Header("3. 服务器输出")]
        [Tooltip("服务器生成代码命名空间（通常与客户端不同）")]
        public string ServerNamespace = "Game.Server.Config";

        [Tooltip("服务器 .cs 输出目录")]
        public string ServerCodeFolder = "";

        [Tooltip("服务器 .bytes 输出目录")]
        public string ServerBytesFolder = "";

        [Header("4. 服务器模板")]
        [Tooltip("使用 FrozenDictionary（需 net8+）；关闭则用 Dictionary")]
        public bool UseFrozenDictionary = true;

        [Header("5. JSON 导出（可选，AOT 零反射）")]
        [Tooltip("额外导出 JSON，供开发期/热更可读；正式包仍用 .bytes")]
        public bool ExportJson = false;

        [Tooltip("客户端 .json 输出目录；为空且开启 ExportJson 时默认写到 ClientBytesFolder 旁的 Json 目录逻辑由你指定路径")]
        public string ClientJsonFolder = "Assets/Config/Json";

        [Tooltip("服务器 .json 输出目录")]
        public string ServerJsonFolder = "";

        public const string DefaultAssetPath = "Assets/ExcelConfigCompilerSettings.asset";
    }
}
