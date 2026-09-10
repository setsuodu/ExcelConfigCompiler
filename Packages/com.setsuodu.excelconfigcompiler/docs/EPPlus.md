# EPPlus 依赖说明

## Unity Editor

`ExcelConfigCompiler.Editor` 通过 `precompiledReferences: ["EPPlus.dll"]` 引用 EPPlus。

请手动将 EPPlus 的主程序集放到：

```
Assets/Plugins/Editor/EPPlus.dll
```

推荐版本：EPPlus 7.5.x（netstandard2.1 或 net6.0）。

下载方式：
1. https://www.nuget.org/packages/EPPlus
2. 下载 `.nupkg`，解压后取 `lib/netstandard2.1/EPPlus.dll`（或对应 TFM）

**注意**：只把 dll 放进 Editor 专用目录，不要打进玩家包。

## CLI

CLI 项目已通过 NuGet 引用 EPPlus，直接 `dotnet restore` / `dotnet publish` 即可，无需额外操作。

## 授权

EPPlus 7+ 需要在代码里声明非商业用途（已在 `CompilePipeline` 中调用）：

```csharp
ExcelPackage.License.SetNonCommercialPersonal("ExcelConfigCompiler");
```

如使用旧版 EPPlus 5/6，请改为：

```csharp
ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
```
