[English](#english) | [中文](#chinese)

<a id="english"></a>
# Stratum Desktop - Build Directory Guide

## 📁 Directory structure

```
Stratum.Desktop/
├── bin/              # ❌ Auto-generated, do not commit
├── obj/              # ❌ Auto-generated, do not commit
├── test-build/       # ❌ Test build, do not commit
├── releases/         # ✅ Release artifacts, can be committed
├── Controls/         # ✅ Source code
├── Panels/           # ✅ Source code
├── Resources/        # ✅ Source code
├── Services/         # ✅ Source code
└── ...
```

---

## 🔨 Build commands

### 1. Development build (Debug)

```bash
dotnet build
```

**Output:** `bin/Debug/net9.0-windows/win-x64/`

**Use case:** day-to-day development and debugging

---

### 2. Test build (Release)

```bash
dotnet publish -c Release -r win-x64 --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -o "test-build"
```

**Output:** `test-build/Stratum.exe`

**Use case:** quick testing of release builds

---

### 3. Official release

```bash
.\publish-release.bat
```

**Output:** `releases/v{VERSION}/`

**Included file:**
- `Stratum-Windows-x64-v{VERSION}.exe` - standalone executable

**Use case:** create official release artifacts

---

## 🧹 Clean build outputs

### Manual cleanup

```bash
# Windows
rmdir /s /q bin obj test-build

# Linux/macOS
rm -rf bin obj test-build
```

### Use the cleanup script

```bash
.\clean.bat
```

This removes:
- `bin/` - build outputs
- `obj/` - intermediate files
- `test-build/` - test build

**Keep:**
- `releases/` - release history

---

## 📦 .gitignore configuration

The following directories are ignored and never committed:

```gitignore
# Auto-generated build outputs
bin/
obj/
test-build/

# Intermediate files
publish/
```

**Note:** `releases/` is **not** ignored and can be committed.

---

## 🚀 Recommended workflow

### Daily development

1. **Write code**
2. **Run for debugging:**
   ```bash
   dotnet run
   ```
3. **Commit changes:**
   ```bash
   git add .
   git commit -m "feat: add new feature"
   ```

### Test release build

1. **Build a test release:**
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true \
       -p:PublishSingleFile=true \
       -p:IncludeNativeLibrariesForSelfExtract=true \
       -p:EnableCompressionInSingleFile=true \
       -o "test-build"
   ```
2. **Test:**
   ```bash
   cd test-build
   .\Stratum.exe
   ```
3. **If issues:** fix code and rebuild

### Official release

1. **Update version:** edit `Stratum.Desktop.csproj`
   ```xml
   <Version>1.0.1</Version>
   ```

2. **Run release script:**
   ```bash
   .\publish-release.bat
   ```
   Enter a version (e.g., 1.0.1)

3. **Test release build:**
   ```bash
   cd releases\v1.0.1
   .\Stratum-Windows-x64-v1.0.1.exe
   ```

4. **Commit release:**
   ```bash
   git add releases/v1.0.1
   git commit -m "release: v1.0.1"
   git tag v1.0.1
   git push origin master --tags
   ```

5. **Create GitHub Release:**
   ```bash
   gh release create v1.0.1 \
       releases/v1.0.1/Stratum-Windows-x64-v1.0.1.exe \
       releases/v1.0.1/Stratum-Windows-x64-v1.0.1.zip \
       --title "Stratum Desktop v1.0.1" \
       --notes "Release notes here"
   ```

---

## 🗑️ Safe-to-delete directories

The following folders can be deleted at any time and will regenerate on build:

- ✅ `bin/` - build outputs
- ✅ `obj/` - intermediate files
- ✅ `test-build/` - test build

**Do not delete:**
- ❌ `releases/` - release history
- ❌ Source folders (Controls/, Panels/, Resources/, Services/, etc.)

---

## 💡 Tips

### Disk space management

If disk space is tight, clean up periodically:

```bash
# Clean all build outputs
.\clean.bat

# Or only clean bin and obj
dotnet clean
```

### Build speed optimization

If builds are slow:

1. **Use incremental builds** (default):
   ```bash
   dotnet build
   ```

2. **Clean then rebuild:**
   ```bash
   dotnet clean
   dotnet build
   ```

3. **Parallel builds** (multi-core CPU):
   ```bash
   dotnet build -m
   ```

---

## 📊 Directory size reference

| Folder | Size | Notes |
|------|------|------|
| `bin/Debug/` | ~50 MB | Debug build outputs |
| `bin/Release/` | ~50 MB | Release build outputs |
| `obj/` | ~10 MB | Intermediate files |
| `test-build/` | ~65 MB | Single-file publish output |
| `releases/v1.0.0/` | ~65 MB | Official release output |

**Total:** ~240 MB (all build outputs)

**After cleanup:** ~65 MB (only releases/)

---

## 🔧 Troubleshooting

### Issue: build fails due to locked files

**Cause:** The app is running and files are locked

**Fix:**
```bash
# Close all Stratum instances
taskkill /f /im Stratum.exe

# Clean and rebuild
.\clean.bat
dotnet build
```

### Issue: release script fails

**Cause:** permissions or path issues

**Fix:**
1. Run PowerShell as administrator
2. Check if paths contain non-ASCII or special characters
3. Run the publish command manually

### Issue: test-build folder is too large

**Cause:** includes the full .NET runtime

**Fix:** This is expected for single-file publish. To reduce size:
```bash
# Framework-dependent publish (requires .NET runtime installed)
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## 📚 Related docs

- [PUBLISH_GUIDE.md](PUBLISH_GUIDE.md) - Detailed release guide
- [IMPLEMENTATION_SUMMARY.md](../IMPLEMENTATION_SUMMARY.md) - Implementation summary
- [QUICK_REFERENCE.txt](../QUICK_REFERENCE.txt) - Quick reference

---

<a id="chinese"></a>
# Stratum Desktop - 构建目录说明

## 📁 目录结构

```
Stratum.Desktop/
├── bin/              # ❌ 自动生成，不提交到 git
├── obj/              # ❌ 自动生成，不提交到 git
├── test-build/       # ❌ 测试构建，不提交到 git
├── releases/         # ✅ 正式发布版本，可以提交到 git
├── Controls/         # ✅ 源代码
├── Panels/           # ✅ 源代码
├── Resources/        # ✅ 源代码
├── Services/         # ✅ 源代码
└── ...
```

---

## 🔨 构建命令

### 1. 开发构建（Debug）

```bash
dotnet build
```

**输出位置**：`bin/Debug/net9.0-windows/win-x64/`

**用途**：日常开发和调试

---

### 2. 测试构建（Release）

```bash
dotnet publish -c Release -r win-x64 --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true \
    -o "test-build"
```

**输出位置**：`test-build/Stratum.exe`

**用途**：快速测试发布版本

---

### 3. 正式发布

```bash
.\publish-release.bat
```

**输出位置**：`releases/v{VERSION}/`

**包含文件**：
- `Stratum-Windows-x64-v{VERSION}.exe` - 独立可执行文件

**用途**：创建正式发布版本

---

## 🧹 清理构建输出

### 手动清理

```bash
# Windows
rmdir /s /q bin obj test-build

# Linux/macOS
rm -rf bin obj test-build
```

### 使用清理脚本

```bash
.\clean.bat
```

这会清理：
- `bin/` - 编译输出
- `obj/` - 中间文件
- `test-build/` - 测试构建

**保留**：
- `releases/` - 正式发布版本（版本历史）

---

## 📦 .gitignore 配置

以下目录已被忽略，不会提交到 git：

```gitignore
# 自动生成的构建输出
bin/
obj/
test-build/

# 中间文件
publish/
```

**注意**：`releases/` 目录**不在** .gitignore 中，可以选择性提交发布版本。

---

## 🚀 推荐工作流

### 日常开发

1. **编写代码**
2. **调试运行**：
   ```bash
   dotnet run
   ```
3. **提交代码**：
   ```bash
   git add .
   git commit -m "feat: add new feature"
   ```

### 测试发布版本

1. **构建测试版本**：
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true \
       -p:PublishSingleFile=true \
       -p:IncludeNativeLibrariesForSelfExtract=true \
       -p:EnableCompressionInSingleFile=true \
       -o "test-build"
   ```
2. **测试**：
   ```bash
   cd test-build
   .\Stratum.exe
   ```
3. **如果有问题**：修复代码，重新构建

### 正式发布

1. **更新版本号**：编辑 `Stratum.Desktop.csproj`
   ```xml
   <Version>1.0.1</Version>
   ```

2. **运行发布脚本**：
   ```bash
   .\publish-release.bat
   ```
   输入版本号（如 1.0.1）

3. **测试发布版本**：
   ```bash
   cd releases\v1.0.1
   .\Stratum-Windows-x64-v1.0.1.exe
   ```

4. **提交发布**：
   ```bash
   git add releases/v1.0.1
   git commit -m "release: v1.0.1"
   git tag v1.0.1
   git push origin master --tags
   ```

5. **创建 GitHub Release**：
   ```bash
   gh release create v1.0.1 \
       releases/v1.0.1/Stratum-Windows-x64-v1.0.1.exe \
       releases/v1.0.1/Stratum-Windows-x64-v1.0.1.zip \
       --title "Stratum Desktop v1.0.1" \
       --notes "Release notes here"
   ```

---

## 🗑️ 可以安全删除的目录

以下目录可以随时删除，下次构建时会自动重新生成：

- ✅ `bin/` - 编译输出
- ✅ `obj/` - 中间文件
- ✅ `test-build/` - 测试构建

**不要删除**：
- ❌ `releases/` - 包含发布版本历史
- ❌ 源代码目录（Controls/, Panels/, Resources/, Services/ 等）

---

## 💡 提示

### 磁盘空间管理

如果磁盘空间不足，可以定期清理：

```bash
# 清理所有构建输出
.\clean.bat

# 或者只清理 bin 和 obj
dotnet clean
```

### 构建速度优化

如果构建速度慢，可以：

1. **使用增量构建**（默认）：
   ```bash
   dotnet build
   ```

2. **清理后重新构建**：
   ```bash
   dotnet clean
   dotnet build
   ```

3. **并行构建**（多核 CPU）：
   ```bash
   dotnet build -m
   ```

---

## 📊 目录大小参考

| 目录 | 大小 | 说明 |
|------|------|------|
| `bin/Debug/` | ~50 MB | Debug 构建输出 |
| `bin/Release/` | ~50 MB | Release 构建输出 |
| `obj/` | ~10 MB | 中间文件 |
| `test-build/` | ~65 MB | 单文件发布版本 |
| `releases/v1.0.0/` | ~65 MB | 正式发布版本 |

**总计**：约 240 MB（包含所有构建输出）

**清理后**：约 65 MB（只保留 releases/）

---

## 🔧 故障排除

### 问题：构建失败，提示文件被占用

**原因**：应用正在运行，文件被锁定

**解决**：
```bash
# 关闭所有 Stratum 实例
taskkill /f /im Stratum.exe

# 清理并重新构建
.\clean.bat
dotnet build
```

### 问题：发布脚本失败

**原因**：可能是权限问题或路径问题

**解决**：
1. 以管理员身份运行 PowerShell
2. 检查路径中是否有中文或特殊字符
3. 手动运行发布命令

### 问题：test-build 目录很大

**原因**：包含了完整的 .NET 运行时

**解决**：这是正常的，单文件发布会包含所有依赖。如果需要减小体积：
```bash
# 使用框架依赖发布（需要用户安装 .NET 运行时）
dotnet publish -c Release -r win-x64 --self-contained false
```

---

## 📚 相关文档

- [PUBLISH_GUIDE.md](PUBLISH_GUIDE.md) - 详细的发布指南
- [IMPLEMENTATION_SUMMARY.md](../IMPLEMENTATION_SUMMARY.md) - 实现总结
- [QUICK_REFERENCE.txt](../QUICK_REFERENCE.txt) - 快速参考
