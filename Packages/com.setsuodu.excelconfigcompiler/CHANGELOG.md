# Changelog

## [1.2.0] - 2026-09-16

### Added
- **AOT 零反射 JSON** 支持
  - Runtime: `JsonReader`（ref struct）、`JsonWriter`
  - Compiler: `JsonGenerator`（TableDef → JSON 数组）
  - 生成代码：`struct.ReadJson` / `Table.LoadJson` / `LoadJsonFromFile` / `LoadJsonAndCache`
  - `CompilePipeline.Options.ExportJson` + `ClientJsonDir` / `ServerJsonDir`
  - **Editor 窗口**：勾选「导出 JSON」+ 客户端/服务器 JSON 目录；`ExcelConfigSettings` 持久化
- 与 binary 共用 Schema；JSON 字段名 = CSharpName；未知字段 `SkipValue` 向前兼容

### Notes
- 正式包仍推荐 `.bytes`；JSON 面向开发期与热更可读场景
- 不依赖 System.Text.Json / Newtonsoft / JsonUtility，IL2CPP/AOT 安全
- `JsonReader` 使用 `NumberStyles` + `CultureInfo`，兼容 Unity BCL

## [1.1.0] - 2026-09-13

### Added
- Client / Server / Shared 目录强制分类；根目录散落 xlsx 直接报错
- 服务器专属生成：`readonly struct`、构造函数读入、可选 `FrozenDictionary` 索引
- Shared 表双端代码 + 同一份 `.bytes`（shared + 拷贝到 client/server Tables）
- `tables.lock.json` 清单（表归属 + sha256）
- CLI：`--no-frozen-dict`；Editor：输出根与可选拷贝路径

### Changed
- 输出布局改为 `client|server|shared/{Generated,Tables}`
- 编译入口支持 `CompilePipeline.Options`

## [1.0.0] - 2026-09-10

### Added
- Runtime: ByteReader / ByteWriter / BinaryFormat
- Compiler: EPPlus、Schema、Validator、CSharpGenerator、BinaryGenerator
- Unity Editor 一键导表 + CLI
