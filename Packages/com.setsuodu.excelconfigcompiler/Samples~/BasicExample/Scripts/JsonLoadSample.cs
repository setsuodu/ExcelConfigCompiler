using System.IO;
using UnityEngine;
// 导表生成的命名空间以你设置为准，示例默认 Game.Config
// using Game.Config;

namespace ExcelConfigCompiler.Samples
{
    /// <summary>
    /// 演示 AOT 零反射 JSON 加载（与 .bytes 二选一即可）。
    /// 前提：已用 Editor 勾选「导出 JSON」并生成 XxxTable.cs + Xxx.json。
    ///
    /// 用法：
    /// 1. 导表生成 Item.cs / Item.json（表名按你的 Excel）
    /// 2. 把下面 Item → 改成你的表名
    /// 3. 挂到场景空物体上，运行 Play
    /// </summary>
    public class JsonLoadSample : MonoBehaviour
    {
        [Header("相对 StreamingAssets 或绝对/工程路径")]
        [Tooltip("例如：Config/Json/Item.json 或 Assets/Config/Json/Item.json")]
        public string jsonRelativePath = "Config/Json/Item.json";

        [Tooltip("若勾选，优先从 StreamingAssets 拼路径")]
        public bool useStreamingAssets = true;

        private void Start()
        {
            string path = ResolvePath(jsonRelativePath);
            if (!File.Exists(path))
            {
                Debug.LogError("[JsonLoadSample] 找不到 JSON: " + path +
                               "\n请先在 Tools → Excel Config Compiler 勾选「导出 JSON」并导表。");
                return;
            }

            // ---------- 替换为你的生成类型 ----------
            // var rows = ItemTable.LoadJsonFromFile(path);
            // Debug.Log($"[JsonLoadSample] 加载 {rows.Length} 行，首行示例见下方");
            // if (rows.Length > 0)
            // {
            //     var r = rows[0];
            //     Debug.Log($"Id={r.Id} ...");
            // }
            //
            // // 带索引（主键为 int Id 时）
            // ItemTable.LoadJsonAndCache(File.ReadAllText(path));
            // var one = ItemTable.Get(1001);

            // 无具体生成类型时，用 JsonReader 演示解析流程（与生成代码同一套 API）
            DemoRawJsonReader(File.ReadAllText(path));
        }

        /// <summary>
        /// 生成代码内部等价逻辑示意（struct.ReadJson / Table.LoadJson）。
        /// </summary>
        private static void DemoRawJsonReader(string json)
        {
            var reader = new ExcelConfigCompiler.Runtime.JsonReader(json);
            reader.Expect('[');
            int count = 0;
            if (!reader.TryExpect(']'))
            {
                while (true)
                {
                    // 一条记录 = 一个 object；生成代码里是 row.ReadJson(ref reader)
                    reader.Expect('{');
                    if (!reader.TryExpect('}'))
                    {
                        while (true)
                        {
                            string name = reader.ReadPropertyName();
                            // 生成代码：switch(name) { case "Id": Id = reader.ReadInt32(); ... }
                            // 这里统一跳过，只统计条数
                            reader.SkipValue();
                            if (reader.TryExpect('}')) break;
                            reader.Expect(',');
                        }
                    }
                    count++;
                    if (reader.TryExpect(']')) break;
                    reader.Expect(',');
                }
            }
            Debug.Log($"[JsonLoadSample] JsonReader 解析完成，共 {count} 条记录（演示 SkipValue；真实项目用 XxxTable.LoadJson）。");
        }

        private string ResolvePath(string relative)
        {
            if (string.IsNullOrWhiteSpace(relative)) return relative;
            if (Path.IsPathRooted(relative) && File.Exists(relative)) return relative;

            if (useStreamingAssets)
            {
                var p = Path.Combine(Application.streamingAssetsPath, relative);
                if (File.Exists(p)) return p;
            }

            // 工程内 Assets/...
            if (relative.StartsWith("Assets/") || relative.StartsWith("Assets\\"))
            {
                var p = Path.GetFullPath(Path.Combine(Application.dataPath, "..", relative));
                if (File.Exists(p)) return p;
            }

            // 相对 dataPath
            var fallback = Path.GetFullPath(Path.Combine(Application.dataPath, relative));
            return fallback;
        }
    }
}
