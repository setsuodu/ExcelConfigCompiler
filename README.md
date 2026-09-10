# ExcelConfigCompiler

Unity / .NET 用的轻量 Excel 配置编译器。

```
.xlsx  →  EPPlus（仅编译期）  →  Schema  →  *.cs + *.bytes
                                              ↑
                                    Runtime ByteReader 加载
```

- **包名**：`com.setsuodu.excelconfigcompiler`  
- **OpenUPM**：https://openupm.com/packages/com.setsuodu.excelconfigcompiler/  
- **面向使用者的说明**：见包内 [`Packages/com.setsuodu.excelconfigcompiler/README.md`](Packages/com.setsuodu.excelconfigcompiler/README.md)（安装、Excel 格式、Editor / 运行时）

本仓库 README 侧重**开发与发布**；第三方集成以包 README 为准。

## 仓库结构

```
Packages/com.setsuodu.excelconfigcompiler/
  Runtime/          # ByteReader / ByteWriter / BinaryFormat（进游戏包）
  Editor/           # 导表窗口 + Compiler（仅 Editor）
  Editor/Compiler/  # 与 CLI 共用的解析 / 生成逻辑
  Cli/              # 外部 / CI 单文件 exe
  Samples~/         # UPM Sample（需在 Package Manager 里 Import）
  docs/             # 维护者补充说明（如 EPPlus）
```

Unity 工程根下的 `Assets/`、`ProjectSettings/` 仅用于本地打开包开发，**不是**下游游戏工程模板。

## 本地开发

1. 用 Unity 2022.3+ 打开本仓库  
2. 放入 EPPlus.dll（见包 README）  
3. **Tools → Excel Config Compiler** 验证导表  

## CLI 构建与发布

```bash
cd Packages/com.setsuodu.excelconfigcompiler/Cli
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

CI：GitHub Actions **Publish CLI**（`workflow_dispatch` 手动触发）。  
填写 `version`（如 `v1.0.1`）且勾选 `create_release` 时，会把多平台 exe 挂到 [Releases](https://github.com/setsuodu/ExcelConfigCompiler/releases)。

若仓库尚无 `Cli/ExcelConfigCompiler.Cli.csproj`，Action 会在构建时生成一份最小工程（编译 `Program.cs` + `Editor/Compiler` + `Runtime`）。建议将 csproj **提交进仓库**，避免 CI 与本地不一致。

## 二进制格式（实现约定）

```
MAGIC    4  'E''X''C''F'
VERSION  4  int32 = 1
COUNT    4  行数
ROW×N    字段顺序与表定义一致，Little Endian
string:  int32 长度（-1=null，0=空串）+ UTF-8
array:   int32 个数 + 元素…
```

## 路线图（简）

- [x] C# 生成 + `.bytes` + Editor 一键导表 + CLI  
- [ ] 多语言 Generator  
- [ ] Enum / Dictionary / 跨表 Ref  

## License

MIT
