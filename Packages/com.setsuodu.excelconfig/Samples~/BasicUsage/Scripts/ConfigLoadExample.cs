using System.IO;
using UnityEngine;
using ExcelConfigCompiler.Runtime;

/// <summary>
/// 运行时读表示例。挂到任意 GameObject 上，把导表生成的 .bytes 放到 Resources/Tables/ 后 Play。
///
/// 流程：
/// 1. Tools → Excel Config Compiler，用 Samples 里的 Item.xlsx 导表
/// 2. 把生成的 Item.cs 放进工程（或由导表输出目录直接进 Assets）
/// 3. 把 Item.bytes 放到 Resources/Tables/Item.bytes
/// 4. 本脚本里把命名空间/类型改成你生成的（默认 Game.Config）
/// 5. Play，看 Console
///
/// 下面用「手写等价结构」演示 ByteReader 读取，避免 Sample 强依赖你尚未生成的代码。
/// 真正项目里请直接用生成的 ItemTable.LoadAndCache / Get。
/// </summary>
public class ConfigLoadExample : MonoBehaviour
{
    [Tooltip("Resources 下的路径，不含扩展名。例如 Tables/Item 对应 Resources/Tables/Item.bytes")]
    public string resourcesPath = "Tables/Item";

    [Tooltip("若勾选，则从 StreamingAssets 读绝对文件（路径填 StreamingAssets 相对路径）")]
    public bool useStreamingAssets;

    public string streamingAssetsRelativePath = "Tables/Item.bytes";

    private void Start()
    {
        byte[] data = LoadBytes();
        if (data == null || data.Length == 0)
        {
            Debug.LogError("[ConfigLoadExample] 未读到 bytes。请先导表，并把 .bytes 放到 Resources/Tables/ 或 StreamingAssets。");
            return;
        }

        // ---------- 方式 A：用生成代码（项目里有 Item.cs 时取消注释）----------
        // using Game.Config;
        // ItemTable.LoadAndCache(data);
        // var item = ItemTable.Get(1001);
        // Debug.Log("Item 1001 = " + item.Name + ", attack=" + item.Attack);

        // ---------- 方式 B：示例直接用 ByteReader 读（不依赖生成代码）----------
        DemoReadWithByteReader(data);
    }

    private byte[] LoadBytes()
    {
        if (useStreamingAssets)
        {
            string path = Path.Combine(Application.streamingAssetsPath, streamingAssetsRelativePath);
            if (!File.Exists(path))
            {
                Debug.LogError("[ConfigLoadExample] 文件不存在: " + path);
                return null;
            }
            return File.ReadAllBytes(path);
        }

        var asset = Resources.Load<TextAsset>(resourcesPath);
        if (asset == null)
        {
            Debug.LogError("[ConfigLoadExample] Resources.Load 失败: " + resourcesPath +
                           "\n请把 Item.bytes 放到 Assets/Resources/Tables/Item.bytes");
            return null;
        }
        return asset.bytes;
    }

    /// <summary>
    /// 与 BinaryFormat / 生成代码约定一致的手写读取，用于验证 Runtime 是否正常。
    /// 假设表结构：id:int, name:string, attack:int, quality:byte（与旧 sample 一致）
    /// </summary>
    private void DemoReadWithByteReader(byte[] data)
    {
        var reader = new ByteReader(data);

        if (!reader.ReadMagicMatches(BinaryFormat.Magic))
        {
            Debug.LogError("[ConfigLoadExample] Magic 不匹配，不是本工具导出的 bytes。");
            return;
        }

        int version = reader.ReadInt32();
        if (version != BinaryFormat.CurrentVersion)
        {
            Debug.LogError("[ConfigLoadExample] 版本不匹配: file=v" + version + " code=v" + BinaryFormat.CurrentVersion);
            return;
        }

        int count = reader.ReadInt32();
        Debug.Log("[ConfigLoadExample] 行数 = " + count);

        for (int i = 0; i < count; i++)
        {
            int id = reader.ReadInt32();
            string name = reader.ReadString();
            int attack = reader.ReadInt32();
            byte quality = reader.ReadByte();

            Debug.Log(string.Format("[ConfigLoadExample] row{0}: id={1}, name={2}, attack={3}, quality={4}",
                i, id, name, attack, quality));
        }

        Debug.Log("[ConfigLoadExample] 读取完成。正式项目请改用生成的 XxxTable.LoadAndCache + Get(id)。");
    }
}
