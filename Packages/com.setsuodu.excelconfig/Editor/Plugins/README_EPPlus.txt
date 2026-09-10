请将 EPPlus.dll 放在此目录（或项目 Assets/Plugins/Editor/）。

下载：https://www.nuget.org/packages/EPPlus
解压 nupkg，取 lib/netstandard2.1/EPPlus.dll

放好后 asmdef 的 precompiledReferences 才能解析 OfficeOpenXml。
