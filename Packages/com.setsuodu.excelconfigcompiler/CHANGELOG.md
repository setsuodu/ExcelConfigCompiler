# Changelog

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
