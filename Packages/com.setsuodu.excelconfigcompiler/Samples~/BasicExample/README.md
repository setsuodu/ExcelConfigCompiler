# Basic Example — JSON / Binary 加载

## 导表与 JSON 导出

1. Excel 放在 `Excel/Client`、`Excel/Server` 或 `Excel/Shared`（**不要**放根目录）。
2. **Tools → Excel Config Compiler**
3. 勾选 **导出 JSON**
4. 目录对应关系：

| 表所在目录 | 会写入的 JSON 目录 |
|------------|-------------------|
| `Excel/Client/` | **客户端 JSON 目录** |
| `Excel/Server/` | **服务器 JSON 目录** |
| `Excel/Shared/` | 两端 JSON 目录（填了哪个写哪个） |

只填「服务器 JSON 目录」、表却在 `Client/` 下 → **不会出 JSON**（日志会有提示）。

## 运行时怎么解析 JSON

生成代码（见 `Scripts/ItemTable.Generated.Sample.cs`）大致是：

```csharp
// 1) 整表
var rows = ItemTable.LoadJsonFromFile(path);
// 或
var rows = ItemTable.LoadJson(File.ReadAllText(path));

// 2) 带 Id 索引
ItemTable.LoadJsonAndCache(jsonText);
var row = ItemTable.Get(1001);
```

内部：

1. `JsonReader` 读 `[` … `]`
2. 每条 `Item.ReadJson(ref reader)`：读 `{`，按字段名 `switch`，调用 `ReadInt32` / `ReadString` / `ReadInt32Array` …
3. **无反射**、无 `JsonUtility` / Newtonsoft

对照文件：

- `Json/Item.sample.json` — 数据样例
- `Scripts/ItemTable.Generated.Sample.cs` — 生成代码长什么样
- `Scripts/JsonLoadSample.cs` — 挂场景演示（默认用 SkipValue 数条数；取消注释即可接真实表）

## 正式包

继续用 `.bytes` + `ItemTable.Load(bytes)`，JSON 留给调试 / 热更可读。
