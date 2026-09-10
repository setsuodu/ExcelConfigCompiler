# Excel Config Compiler

轻量 Excel 配置编译器：**EPPlus 读表 → 生成显式 Read/Write 的 C# 代码（无反射）→ 生成二进制 .bytes**。

定位类似 Luban 的「代码生成」思路，但砍掉跨语言/跨平台的通用抽象，只服务 **Unity + .NET 二进制配置** 场景。

```
item.xlsx ──(EPPlus, 仅编译期)──► Schema ──┬──► Item.cs     (放进 Unity / Server)
                                           └──► Item.bytes  (运行时加载)
```

## 特性

- **Runtime 高性能**：`ref struct ByteReader` + `Span<byte>`，数值字段读取路径 **0 GC**
- **无反射 / AOT 友好**：生成代码全部是显式 `reader.ReadInt32()` 展开，IL2CPP 安全
- **Magic + Version 校验**：文件头 `EXCF` + 版本号，防止错表/旧表
- **主键索引**：第一列名为 `id` 且为 `int` 时，自动生成 `LoadAndCache` + `Get(id)` / `TryGet`
- **Unity Editor 一键导表** + **独立 CLI exe**（CI / 外部用）
- **openupm 就绪**，结构极简（几十个 C# 文件以内）

## 安装（openupm）

```bash
openupm add com.excelconfigcompiler
```

或在 `Packages/manifest.json` 添加：

```json
{
  "dependencies": {
    "com.excelconfigcompiler": "https://github.com/YOUR_ORG/ExcelConfigCompiler.git"
  }
}
```

### 重要：EPPlus 依赖（仅 Editor 需要）

Unity Editor 导表需要 EPPlus。请把 **EPPlus.dll** 放到：

```
Assets/Plugins/Editor/EPPlus.dll
```

（或任意 Editor 可引用的位置，并确保 `ExcelConfigCompiler.Editor.asmdef` 的 `precompiledReferences` 能找到它）

推荐从 NuGet 下载 EPPlus 7.x 的 netstandard2.1 / net6.0 版本，只拷贝主 dll 即可。  
**Runtime 不依赖 EPPlus**，玩家包不会带上。

CLI 则通过 NuGet 自动还原，无需手动处理。

## Excel 约定

| 行 | 内容 |
|----|------|
| 第 1 行 | 字段名（会转成 PascalCase） |
| 第 2 行 | 类型 |
| 第 3 行起 | 数据 |

**Worksheet 名** = 生成的 C# 类型名（必须是合法标识符）。

### 支持的类型

| Excel 类型 | C# 类型 |
|------------|---------|
| `byte` `sbyte` `short` `ushort` `int` `uint` `long` `ulong` | 对应数值类型 |
| `float` `double` `bool` | float / double / bool |
| `string` | string? |
| `int[]` `byte[]` `float[]` `string[]` | 对应数组（单元格内用逗号分隔） |

第一列名为 `id`（大小写不敏感）且类型为 `int` 时，自动生成主键索引。

## Unity 使用

### 1. Editor 一键导表

菜单 **Tools → Excel Config Compiler**

1. 设置 Excel 源目录、代码输出目录、二进制输出目录、命名空间
2. 点击「一键导表」
3. 生成的 `.cs` 和 `.bytes` 会自动刷新到 Project

### 2. 运行时加载

```csharp
using UnityEngine;
using Game.Config;          // 你的命名空间
using ExcelConfigCompiler.Runtime;

public class ConfigBootstrap : MonoBehaviour
{
    void Awake()
    {
        var asset = Resources.Load<TextAsset>("Tables/Item"); // 或 Addressables
        ItemTable.LoadAndCache(asset.bytes);

        var item = ItemTable.Get(1001);
        Debug.Log(item.Name);
    }
}
```

把 `.bytes` 放到 `Resources/Tables/` 或走 Addressables 即可。Unity 原生识别 `.bytes` 后缀。

## CLI（外部 / CI）

```bash
# 还原 & 构建（单文件自包含 exe）
cd Cli
dotnet publish -c Release

# 运行
./bin/Release/net8.0/win-x64/publish/ExcelConfigCompiler.exe ./Tables ./Output -n Game.Config
```

支持单个 xlsx 或整个目录。

参数：
- `<输入.xlsx|目录>`
- `<输出目录>`
- `-n / --namespace` 命名空间（默认 `Config`）

多语言目前**只预留**（只生成 C#），后续可通过扩展 Generator 支持。

## 项目结构

```
ExcelConfigCompiler/
├── package.json                 # openupm
├── Runtime/                     # Unity 运行时（无依赖）
│   ├── ByteReader.cs            # ref struct, 0 GC 数值路径
│   ├── ByteWriter.cs
│   └── BinaryFormat.cs
├── Editor/                      # Unity 编辑器
│   ├── ExcelConfigCompilerWindow.cs
│   ├── ExcelConfigSettings.cs
│   └── Compiler/                # 编译逻辑（Editor + CLI 共享）
│       ├── Schema.cs
│       ├── ExcelParser.cs
│       ├── Validator.cs
│       ├── CSharpGenerator.cs
│       ├── BinaryGenerator.cs
│       └── CompilePipeline.cs
├── Cli/                         # 独立 CLI 项目（可 publish 成单文件 exe）
└── samples/
```

## 二进制格式

```
MAGIC    4 bytes  'E' 'X' 'C' 'F'
VERSION  4 bytes  int32 = 1
COUNT    4 bytes  int32 行数
ROW × N
  字段按声明顺序，Little Endian
  string: int32 长度（-1 = null，0 = 空串）+ UTF8 字节
  array:  int32 元素个数 + 元素...
```

## 已知限制 / 后续可扩展

- 多语言 Generator 只预留，当前仅 C#
- Enum / Dictionary / 跨表引用（Ref）未实现（Schema 已留扩展点）
- 主键索引目前仅对 `int` id 生成强类型 `Get(int)`
- EPPlus 授权 API 在不同大版本间有变化，如遇编译错误按本地 EPPlus 文档调整 `License` 设置即可

## License

MIT
```
