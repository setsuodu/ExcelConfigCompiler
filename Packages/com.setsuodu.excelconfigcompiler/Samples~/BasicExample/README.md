# Basic Usage Sample

## 内容

| 路径 | 说明 |
|------|------|
| `Tables/Item.xlsx` | 示例 Excel（第1行字段名、第2行类型、第3行起数据） |
| `Scripts/ConfigLoadExample.cs` | 运行时读取 `.bytes` 的测试脚本 |

## 使用步骤

1. 在 Package Manager 中对本包点击 **Import**（Samples → Basic Usage）
2. 安装 EPPlus（推荐 4.x 的 `net35` / `net40` 单 dll）到 `Editor/Plugins/EPPlus.dll`
3. 菜单 **Tools → Excel Config Compiler**
   - Excel 源目录指到本 Sample 的 `Tables`
   - 代码/二进制输出到你的 `Assets/Config/...`
4. 把生成的 `Item.bytes` 拷到 `Assets/Resources/Tables/Item.bytes`
5. 空场景挂 `ConfigLoadExample`，Play，看 Console

## 关于 Samples~

Unity UPM 约定使用 `Samples~` 目录名（带 `~`）：

- 包内默认**不**把示例编译进工程
- 用户在 Package Manager → Sample 里点 Import 才复制到 `Assets/Samples/...`

`package.json` 的 `samples[].path` 已指向 `Samples~/BasicUsage`。
