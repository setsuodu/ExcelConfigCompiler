# Excel Config Compiler

Unity / .NET 轻量导表：按 **Client / Server / Shared** 分流，生成 C# + `.bytes`。

- 客户端：可变 `struct` + `Dictionary`
- 服务器：`readonly struct` + 可选 `FrozenDictionary`
- Shared：两端代码各一份，**同一份 wire format bytes**（分别写入你配置的客户端/服务器 bytes 目录）

## Excel 目录（强制）

```text
Excel/
  Client/
  Server/
  Shared/
```

禁止把 xlsx 直接放在 Excel 根下。

## Editor 配置（三者分开）

| 配置 | 含义 |
|------|------|
| Excel 根目录 | 含 Client/Server/Shared |
| 客户端命名空间 / 代码目录 / bytes 目录 | 只写客户端产物，**不再拼 `/client`** |
| 服务器命名空间 / 代码目录 / bytes 目录 | 只写服务器产物；命名空间与客户端独立 |

`tables.lock.json` 默认写在 Excel 根目录。

## CLI

```bash
ExcelConfigCompiler ./Excel \
  --client-code ./ClientGen --client-bytes ./ClientBytes \
  --server-code ./ServerGen --server-bytes ./ServerBytes \
  -n Game.Config --server-namespace Game.Server.Config
```

## License

MIT
