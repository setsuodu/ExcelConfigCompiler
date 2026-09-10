# EPPlus 依赖说明

## Unity Editor

`ExcelConfigCompiler.Editor` 通过 `precompiledReferences: ["EPPlus.dll"]` 引用 EPPlus。

推荐版本：EPPlus 4.x.x（只能net3.5）。

下载方式：
1. https://www.nuget.org/packages/EPPlus
2. 下载 `.nupkg`，解压后取 `lib/net3.5/EPPlus.dll`（或对应 TFM）

**注意**：只把 dll 放进 Editor 专用目录，不要打进玩家包。
