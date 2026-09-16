using UnityEngine;

namespace ExcelConfigCompiler.Editor
{
    /// <summary>
    /// 单一 Excel 输入；Client / Server / Dashboard 三套输出互不干扰。
    /// 所有输出路径默认空：不填 = 不生成该产物。
    /// </summary>
    public class ExcelConfigSettings : ScriptableObject
    {
        [Header("输入（唯一）")]
        [Tooltip("Excel 根目录（其下 Client / Server / Shared）")]
        public string ExcelSourceFolder = "Excel";

        [Header("客户端输出（全空 = 不生成客户端任何东西）")]
        public string ClientNamespace = "Game.Config";
        [Tooltip("生成 .cs；空 = 不生成代码")]
        public string ClientCodeFolder = "";
        [Tooltip("导出 .bytes")]
        public bool ClientExportBytes = false;
        public string ClientBytesFolder = "";
        [Tooltip("导出 .json（AOT）")]
        public bool ClientExportJson = false;
        public string ClientJsonFolder = "";

        [Header("服务器输出")]
        public string ServerNamespace = "Game.Server.Config";
        public string ServerCodeFolder = "";
        public bool ServerExportBytes = false;
        public string ServerBytesFolder = "";
        public bool ServerExportJson = false;
        public string ServerJsonFolder = "";
        [Tooltip("仅 net8+ 服务器；Unity 请关")]
        public bool UseFrozenDictionary = false;

        [Header("Dashboard 输出（可选第三端）")]
        public string DashboardNamespace = "Game.Dashboard.Config";
        public string DashboardCodeFolder = "";
        public bool DashboardExportBytes = false;
        public string DashboardBytesFolder = "";
        public bool DashboardExportJson = false;
        public string DashboardJsonFolder = "";
        [Tooltip("Dashboard 是否用服务器风格 readonly")]
        public bool DashboardUseServerStyle = true;

        public const string DefaultAssetPath = "Assets/ExcelConfigCompilerSettings.asset";
    }
}
