# Excel Config Compiler

Unity / .NET 轻量导表：按 **Client / Server / Shared** 分流，生成 C# + `.bytes`，可选 **AOT 零反射 JSON**。

- 客户端：可变 `struct` + `Dictionary`
- 服务器：`readonly struct` + 可选 `FrozenDictionary`
- Shared：两端代码各一份，**同一份 wire format bytes**（分别写入你配置的客户端/服务器 bytes 目录）
- JSON（可选）：与 binary 同一套 Schema 静态生成 `ReadJson` / `LoadJson`，**无反射、无第三方 JSON 库**，AOT/IL2CPP 安全

## Excel 目录（强制）

```text
Excel/
  Client/
  Server/
  Shared/
```

禁止把 xlsx 直接放在 Excel 根下。

## Editor 配置（三者分开）

| 配置 | 含义 |
|------|------|
| Excel 根目录 | 含 Client/Server/Shared |
| 客户端命名空间 / 代码目录 / bytes 目录 | 只写客户端产物，**不再拼 `/client`** |
| 服务器命名空间 / 代码目录 / bytes 目录 | 只写服务器产物；命名空间与客户端独立 |
| ExportJson + Client/Server Json 目录 | 可选；开发期/热更可读 JSON，正式包仍用 `.bytes` |

`tables.lock.json` 默认写在 Excel 根目录。

## CLI

```bash
ExcelConfigCompiler ./Excel \
  --client-code ./ClientGen --client-bytes ./ClientBytes \
  --server-code ./ServerGen --server-bytes ./ServerBytes \
  -n Game.Config --server-namespace Game.Server.Config
```

开启 JSON 时在 `CompilePipeline.Options` 中设置：

```csharp
ExportJson = true,
ClientJsonDir = "./ClientJson",
ServerJsonDir = "./ServerJson",
```

## 运行时加载

```csharp
// 正式包：binary（推荐）
var items = ItemConfigTable.Load(bytes);
// 或带索引
var items = ItemConfigTable.LoadAndCache(bytes);
var row = ItemConfigTable.Get(1001);

// 开发期 / 热更：AOT JSON（零反射）
var items = ItemConfigTable.LoadJsonFromFile(path);
// 或
var items = ItemConfigTable.LoadJson(jsonText);
var items = ItemConfigTable.LoadJsonAndCache(jsonText);
```

JSON 格式为数组 of object，字段名与 C# 属性一致：

```json
[{"Id":1,"Name":"sword","Attrs":[1,2,3]},{"Id":2,"Name":"shield","Attrs":[]}]
```

## 设计要点

- Binary 与 JSON **共用同一 Schema**，生成代码里字段顺序/名称一致
- `JsonReader` 为 `ref struct`，只实现导表子集，不引入 `System.Text.Json` / Newtonsoft / `JsonUtility`
- 性能定位：去掉反射 JSON 库依赖，方便调试与热更；极限性能仍以 `.bytes` 为准

## License

MIT
