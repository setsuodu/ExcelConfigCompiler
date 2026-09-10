# Excel Config Compiler

Unity 轻量导表：**Excel → 生成 C#（显式 Read/Write，无反射）+ `.bytes` 二进制**。

Runtime 使用 `ref struct ByteReader`，数值路径 0 GC，IL2CPP / AOT 友好。

## 安装

[OpenUPM](https://openupm.com/packages/com.setsuodu.excelconfigcompiler/)

```bash
openupm add com.setsuodu.excelconfigcompiler
```

或在 `Packages/manifest.json` 使用 git / OpenUPM scoped registry。

**Editor 需要 EPPlus.dll**（勿打进玩家包）：

1. 从 [NuGet EPPlus](https://www.nuget.org/packages/EPPlus) 下载 `.nupkg` 并解压  
2. 取 `lib/netstandard2.1/EPPlus.dll`  
3. 放到 `Assets/Plugins/Editor/` 或包内 `Editor/Plugins/`

CLI 通过 NuGet 自动还原 EPPlus，无需手动拷 dll。

## Excel 约定

| 行 | 内容 |
|----|------|
| 1 | 字段名（PascalCase） |
| 2 | 类型（`int` / `string` / `int[]` …） |
| 3+ | 数据 |

Worksheet 名 = 生成的 C# 类型名。首列名为 `Id` 且类型为 `int` 时，生成 `XxxTable.Get(id)`。

## 使用

1. 菜单 **Tools → Excel Config Compiler**，配置源目录 / 代码与二进制输出目录 / 命名空间后点「一键导表」  
2. 运行时：

```csharp
ItemTable.LoadAndCache(Resources.Load<TextAsset>("Tables/Item").bytes);
var row = ItemTable.Get(1001);
```

路径支持工程根相对（如 `Excel`，与 `Assets` 同级）、`Assets/...` 或绝对路径。

## CLI（可选）

```bash
ExcelConfigCompiler <输入.xlsx|目录> <输出目录> -n Game.Config
```

预编译 exe 见仓库 [Releases](https://github.com/setsuodu/ExcelConfigCompiler/releases)。

## License

MIT
