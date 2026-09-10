# Changelog

## [1.0.0] - 2026-09-10

### Added
- Runtime: `ByteReader` (ref struct + Span, numeric 0 GC), `ByteWriter`, `BinaryFormat` (Magic `EXCF` + Version)
- Compiler: EPPlus parser, Schema, Validator (duplicate id / field names), CSharpGenerator (explicit Read/Write), BinaryGenerator
- Unity Editor: `Tools/Excel Config Compiler` one-click export window + `ExcelConfigSettings`
- CLI: single-file self-contained exe for external / CI usage
- Support scalar types + `int[]` / `byte[]` / `float[]` / `string[]`
- Auto index + `Get(id)` / `TryGet` when first column is `id` (int)
- openupm ready package structure

### Reserved
- Multi-language generators (only C# implemented)
- Enum / Dictionary / Ref field types (schema ready for extension)
