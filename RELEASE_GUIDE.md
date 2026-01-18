[English](#english) | [中文](#chinese)

<a id="english"></a>
# Stratum Desktop - Release Guide

## Quick release

### Windows users

**Use the release script (recommended):**
```cmd
cd Stratum.Desktop
publish-release.bat
```

The script will automatically:
1. Clean old release files
2. Build the Release configuration
3. Create a single-file exe
4. Create a ZIP package
5. Show file size and location

**Manual release:**
```cmd
cd Stratum.Desktop

REM Publish a single-file build
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=true -p:TrimMode=partial -p:EnableCompressionInSingleFile=true

REM Output location
REM bin\Release\net9.0-windows\win-x64\publish\Stratum.exe
```

### Linux/macOS users

```bash
cd Stratum.Desktop
chmod +x publish-release.sh
./publish-release.sh
```

---

## Publish to GitHub

### Method 1: GitHub CLI (recommended)

```bash
# 1. Install GitHub CLI
# Windows: winget install GitHub.cli
# macOS: brew install gh
# Linux: https://github.com/cli/cli/blob/trunk/docs/install_linux.md

# 2. Sign in to GitHub
gh auth login

# 3. Create a release
cd Stratum.Desktop
gh release create v1.0.0 \
  --title "Stratum Desktop v1.0.0 - UI Modernization" \
  --notes "See IMPLEMENTATION_SUMMARY.md" \
  releases/v1.0.0/Stratum-Windows-x64-v1.0.0.exe \
  releases/v1.0.0/Stratum-Windows-x64-v1.0.0.zip
```

### Method 2: GitHub web UI

1. Visit `https://github.com/banlanzs/stratum-2fa/releases/new`
2. Fill in:
   - **Tag version:** `v1.0.0`
   - **Release title:** `Stratum Desktop v1.0.0 - UI Modernization`
   - **Description:** Paste the Release Notes (template below)
3. Upload files:
   - `Stratum-Windows-x64-v1.0.0.exe`
   - `Stratum-Windows-x64-v1.0.0.zip`
4. Click **Publish release**

---

## Release Notes template

```markdown
# Stratum Desktop v1.0.0 - UI Modernization

## 🎉 Major update

This release delivers a full UI modernization for Stratum Desktop and a refreshed user experience.

### ✨ New features

- **Single window + navigation rail** - Say goodbye to multiple windows; everything is in the main window
- **Material Design 3** - Modern design language with light/dark themes
- **5 dedicated panels** - Home, Settings, Categories, Backup, About
- **More spacious layout** - Window size increased from 450×600 to 850×600

### 🎨 Visual improvements

- ✅ Navigation rail for one-click switching
- ✅ Material Design 3 color system
- ✅ Light/dark theme support
- ✅ Unified design language and interactions

### 🔧 Technical improvements

- ✅ Modular architecture for easier maintenance and expansion
- ✅ MVVM pattern with improved code quality
- ✅ Fully backward compatible, all features preserved

### 📦 Downloads

| Platform | File | Size |
|------|------|------|
| Windows x64 | [Stratum-Windows-x64-v1.0.0.exe](link) | ~60 MB |
| Windows x64 (ZIP) | [Stratum-Windows-x64-v1.0.0.zip](link) | ~60 MB |

### 📋 System requirements

- **OS:** Windows 10/11 (x64)
- **.NET runtime:** Not required (self-contained)
- **Disk space:** ~150 MB

### 🚀 Installation

1. Download `Stratum-Windows-x64-v1.0.0.exe`
2. Double-click to run
3. On first launch, Windows Defender may prompt (click "Run anyway")

### ⚠️ Notes

- This version is a standalone executable and requires no installation
- Database path: `%APPDATA%\Stratum\authenticator.db3`
- Settings path: `%APPDATA%\Stratum\settings.json`
- Data migrates automatically when upgrading from older versions

### 📚 Full changelog

See [IMPLEMENTATION_SUMMARY.md](https://github.com/banlanzs/stratum-2fa/blob/master/IMPLEMENTATION_SUMMARY.md)

### 🐛 Known issues

- None

### 🙏 Thanks

Thanks to all contributors and users for your support.

---

**Full source:** https://github.com/banlanzs/stratum-2fa
**Issue tracker:** https://github.com/banlanzs/stratum-2fa/issues
```

---

## Release checklist

### Before release

- [ ] All features tested
- [ ] Build has zero warnings/errors (`dotnet build -c Release`)
- [ ] Update version in `Stratum.Desktop.csproj`
- [ ] Update `CHANGELOG.md`
- [ ] Prepare Release Notes
- [ ] Run release script (`publish-release.bat`)
- [ ] Test the published exe
- [ ] Verify file size is reasonable (50-80 MB)

### During release

- [ ] Create Git tag (`git tag v1.0.0`)
- [ ] Push tag (`git push origin v1.0.0`)
- [ ] Create GitHub Release
- [ ] Upload release files
- [ ] Fill in Release Notes
- [ ] Mark as Latest Release

### After release

- [ ] Download and test release files
- [ ] Verify download links work
- [ ] Check Release Notes formatting
- [ ] Update download links in docs
- [ ] Announce to users (social media/forums)
- [ ] Close resolved issues

---

## Versioning

Follows [Semantic Versioning 2.0.0](https://semver.org/lang/zh-CN/):

**Format:** `major.minor.patch`

**Rules:**
- **Major:** incompatible API changes
  - Example: `v1.0.0` → `v2.0.0` (breaking architecture change)

- **Minor:** backward-compatible feature additions
  - Example: `v1.0.0` → `v1.1.0` (add drag-and-drop sorting)

- **Patch:** backward-compatible bug fixes
  - Example: `v1.0.0` → `v1.0.1` (fix a bug)

**Examples:**
```
v1.0.0 - First stable release (UI modernization)
v1.0.1 - Fix dark theme display issue
v1.1.0 - Add drag-and-drop sorting
v1.2.0 - Add auto-update
v2.0.0 - Major architecture changes (if any)
```

---

## FAQ

### Q: The exe file is too large. What can I do?

**A:** Use an optimized publish configuration:

```bash
dotnet publish -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:TrimMode=partial \
  -p:EnableCompressionInSingleFile=true
```

This can reduce size by 30-40% (from 80-120 MB down to 50-70 MB).

### Q: Windows Defender blocks the app.

**A:** This is normal because the exe is not digitally signed. Options:

1. **Get a code signing certificate** (recommended, paid)
   - Purchase from DigiCert, Sectigo, etc.
   - Use `signtool` to sign the exe

2. **Explain it in Release Notes**
   ```markdown
   ### Security notice

   On first run, Windows Defender may warn because the app is not signed.

   **How to run:**
   1. Click "More info"
   2. Click "Run anyway"

   The app is open source, so you can review the code for safety.
   ```

3. **Provide a ZIP package** as an alternative download

### Q: How do I support multiple platforms?

**A:** Publish per platform:

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Windows ARM64
dotnet publish -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# macOS x64
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true

# macOS ARM64 (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
```

### Q: How do I add auto-updates?

**A:** You can integrate one of these libraries:

- **Squirrel.Windows** - Windows auto updates
- **AutoUpdater.NET** - Cross-platform auto updates
- **Velopack** - Modern update framework

Example (AutoUpdater.NET):

```csharp
// In App.xaml.cs
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    AutoUpdater.Start("https://your-domain.com/update.xml");
}
```

### Q: How do I reduce first-launch time?

**A:** Enable ReadyToRun (R2R):

```bash
dotnet publish -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishReadyToRun=true
```

Note: this increases file size but improves startup time.

---

## Automated release (GitHub Actions)

Create `.github/workflows/release.yml`:

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build-and-release:
    runs-on: windows-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'

    - name: Publish Windows x64
      run: |
        cd Stratum.Desktop
        dotnet publish -c Release -r win-x64 --self-contained true `
          -p:PublishSingleFile=true `
          -p:IncludeNativeLibrariesForSelfExtract=true `
          -p:PublishTrimmed=true `
          -p:TrimMode=partial `
          -p:EnableCompressionInSingleFile=true

    - name: Create ZIP
      run: |
        $version = "${{ github.ref_name }}"
        $exePath = "Stratum.Desktop/bin/Release/net9.0-windows/win-x64/publish/Stratum.exe"
        $zipPath = "Stratum-Windows-x64-$version.zip"
        Compress-Archive -Path $exePath -DestinationPath $zipPath

    - name: Create Release
      uses: softprops/action-gh-release@v1
      with:
        files: |
          Stratum.Desktop/bin/Release/net9.0-windows/win-x64/publish/Stratum.exe
          Stratum-Windows-x64-${{ github.ref_name }}.zip
        body: |
          ## Stratum Desktop ${{ github.ref_name }}

          ### Downloads
          - **Windows x64:** Stratum.exe
          - **Windows x64 (ZIP):** Stratum-Windows-x64-${{ github.ref_name }}.zip

          ### System requirements
          - Windows 10/11 (x64)
          - No .NET runtime required

          ### Full changelog
          See [IMPLEMENTATION_SUMMARY.md](https://github.com/${{ github.repository }}/blob/master/IMPLEMENTATION_SUMMARY.md)
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

**Usage:**

```bash
# Create and push tag
git tag v1.0.0
git push origin v1.0.0

# GitHub Actions will automatically:
# 1. Build Release configuration
# 2. Create ZIP package
# 3. Create GitHub Release
# 4. Upload release files
```

---

## Summary

**Recommended release workflow:**

1. **Development complete** → test all features
2. **Update version** → edit `.csproj`
3. **Run release script** → `publish-release.bat`
4. **Test release build** → confirm exe runs
5. **Create Git tag** → `git tag v1.0.0 && git push origin v1.0.0`
6. **Create GitHub Release** → use `gh` CLI or web UI
7. **Upload files** → exe and zip
8. **Fill Release Notes** → use template
9. **Publish** → click Publish release
10. **Notify users** → social media/forums/email

**File list:**
- ✅ `publish-release.bat` - Windows release script
- ✅ `publish-release.sh` - Linux/macOS release script
- ✅ `RELEASE_GUIDE.md` - This release guide
- ✅ Release Notes template
- ✅ GitHub Actions config (optional)

Good luck with the release! 🚀

---

<a id="chinese"></a>
# Stratum Desktop - 发布指南

## 快速发布

### Windows 用户

**使用发布脚本（推荐）：**
```cmd
cd Stratum.Desktop
publish-release.bat
```

脚本会自动：
1. 清理旧的发布文件
2. 编译 Release 版本
3. 创建单文件 exe
4. 创建 ZIP 压缩包
5. 显示文件大小和位置

**手动发布：**
```cmd
cd Stratum.Desktop

REM 发布单文件版本
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishTrimmed=true -p:TrimMode=partial -p:EnableCompressionInSingleFile=true

REM 输出位置
REM bin\Release\net9.0-windows\win-x64\publish\Stratum.exe
```

### Linux/macOS 用户

```bash
cd Stratum.Desktop
chmod +x publish-release.sh
./publish-release.sh
```

---

## 发布到 GitHub

### 方法 1: 使用 GitHub CLI（推荐）

```bash
# 1. 安装 GitHub CLI
# Windows: winget install GitHub.cli
# macOS: brew install gh
# Linux: https://github.com/cli/cli/blob/trunk/docs/install_linux.md

# 2. 登录 GitHub
gh auth login

# 3. 创建 Release
cd Stratum.Desktop
gh release create v1.0.0 \
  --title "Stratum Desktop v1.0.0 - UI 现代化重设计" \
  --notes "详见 IMPLEMENTATION_SUMMARY.md" \
  releases/v1.0.0/Stratum-Windows-x64-v1.0.0.exe \
  releases/v1.0.0/Stratum-Windows-x64-v1.0.0.zip
```

### 方法 2: 使用 GitHub 网页

1. 访问 `https://github.com/banlanzs/stratum-2fa/releases/new`
2. 填写信息：
   - **Tag version:** `v1.0.0`
   - **Release title:** `Stratum Desktop v1.0.0 - UI 现代化重设计`
   - **Description:** 粘贴 Release Notes（见下方模板）
3. 上传文件：
   - `Stratum-Windows-x64-v1.0.0.exe`
   - `Stratum-Windows-x64-v1.0.0.zip`
4. 点击 **Publish release**

---

## Release Notes 模板

```markdown
# Stratum Desktop v1.0.0 - UI 现代化重设计

## 🎉 重大更新

本版本对 Stratum Desktop 进行了全面的 UI 现代化重设计，带来全新的用户体验！

### ✨ 新功能

- **单窗口 + 侧边栏导航** - 告别多窗口割裂，所有功能集成在主窗口
- **Material Design 3** - 现代化的设计语言，支持浅色/深色主题
- **5 个独立面板** - Home、Settings、Categories、Backup、About
- **更宽敞的界面** - 窗口尺寸从 450×600 升级到 850×600

### 🎨 视觉改进

- ✅ 侧边栏导航，一键切换功能
- ✅ Material Design 3 颜色系统
- ✅ 浅色/深色主题支持
- ✅ 统一的设计语言和交互体验

### 🔧 技术改进

- ✅ 模块化架构，易于维护和扩展
- ✅ MVVM 模式，代码质量提升
- ✅ 完全向后兼容，所有功能保持完整

### 📦 下载

| 平台 | 文件 | 大小 |
|------|------|------|
| Windows x64 | [Stratum-Windows-x64-v1.0.0.exe](链接) | ~60 MB |
| Windows x64 (ZIP) | [Stratum-Windows-x64-v1.0.0.zip](链接) | ~60 MB |

### 📋 系统要求

- **操作系统：** Windows 10/11 (x64)
- **.NET 运行时：** 无需安装（自包含）
- **磁盘空间：** 约 150 MB

### 🚀 安装说明

1. 下载 `Stratum-Windows-x64-v1.0.0.exe`
2. 双击运行即可
3. 首次运行可能需要 Windows Defender 确认（点击"仍要运行"）

### ⚠️ 注意事项

- 本版本为独立可执行文件，无需安装
- 数据库文件位置：`%APPDATA%\Stratum\authenticator.db3`
- 设置文件位置：`%APPDATA%\Stratum\settings.json`
- 从旧版本升级时，数据会自动迁移

### 📚 完整更新日志

详见 [IMPLEMENTATION_SUMMARY.md](https://github.com/banlanzs/stratum-2fa/blob/master/IMPLEMENTATION_SUMMARY.md)

### 🐛 已知问题

- 无

### 🙏 致谢

感谢所有贡献者和用户的支持！

---

**完整源代码：** https://github.com/banlanzs/stratum-2fa
**问题反馈：** https://github.com/banlanzs/stratum-2fa/issues
```

---

## 发布检查清单

### 发布前

- [ ] 所有功能测试通过
- [ ] 编译无警告无错误（`dotnet build -c Release`）
- [ ] 更新版本号（在 `Stratum.Desktop.csproj` 中）
- [ ] 更新 `CHANGELOG.md`
- [ ] 准备 Release Notes
- [ ] 运行发布脚本（`publish-release.bat`）
- [ ] 测试发布的 exe 文件
- [ ] 检查文件大小合理（50-80 MB）

### 发布时

- [ ] 创建 Git tag（`git tag v1.0.0`）
- [ ] 推送 tag（`git push origin v1.0.0`）
- [ ] 创建 GitHub Release
- [ ] 上传发布文件
- [ ] 填写 Release Notes
- [ ] 设置为 Latest Release

### 发布后

- [ ] 下载并测试 Release 文件
- [ ] 验证下载链接有效
- [ ] 检查 Release Notes 格式
- [ ] 更新文档中的下载链接
- [ ] 在社交媒体/论坛通知用户
- [ ] 关闭已修复的 Issues

---

## 版本号管理

遵循 [语义化版本 2.0.0](https://semver.org/lang/zh-CN/)：

**格式：** `主版本号.次版本号.修订号`

**规则：**
- **主版本号（Major）：** 不兼容的 API 修改
  - 示例：`v1.0.0` → `v2.0.0`（重大架构变更）

- **次版本号（Minor）：** 向下兼容的功能性新增
  - 示例：`v1.0.0` → `v1.1.0`（添加拖拽排序功能）

- **修订号（Patch）：** 向下兼容的问题修正
  - 示例：`v1.0.0` → `v1.0.1`（修复 bug）

**示例：**
```
v1.0.0 - 首次正式发布（UI 现代化重设计）
v1.0.1 - 修复深色主题显示问题
v1.1.0 - 添加拖拽排序功能
v1.2.0 - 添加自动更新功能
v2.0.0 - 重大架构变更（如果有）
```

---

## 常见问题

### Q: 发布的 exe 文件太大怎么办？

**A:** 使用优化的发布配置：

```bash
dotnet publish -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:TrimMode=partial \
  -p:EnableCompressionInSingleFile=true
```

可以减小 30-40% 的文件大小（从 80-120 MB 降到 50-70 MB）。

### Q: 用户报告 Windows Defender 拦截？

**A:** 这是正常的，因为 exe 文件没有数字签名。解决方案：

1. **申请代码签名证书**（推荐，但需要费用）
   - 从 DigiCert、Sectigo 等 CA 购买
   - 使用 `signtool` 签名 exe 文件

2. **在 Release Notes 中说明**
   ```markdown
   ### 安全提示

   首次运行时，Windows Defender 可能会显示警告。这是因为应用没有数字签名。

   **如何运行：**
   1. 点击"更多信息"
   2. 点击"仍要运行"

   应用是开源的，您可以查看源代码确认安全性。
   ```

3. **提供 ZIP 压缩包**作为替代下载方式

### Q: 如何支持多平台？

**A:** 发布多个平台版本：

```bash
# Windows x64
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true

# Windows ARM64
dotnet publish -c Release -r win-arm64 --self-contained true -p:PublishSingleFile=true

# Linux x64
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true

# macOS x64
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true

# macOS ARM64 (Apple Silicon)
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true
```

### Q: 如何添加自动更新功能？

**A:** 可以集成以下库：

- **Squirrel.Windows** - Windows 自动更新
- **AutoUpdater.NET** - 跨平台自动更新
- **Velopack** - 现代化的自动更新框架

示例（使用 AutoUpdater.NET）：

```csharp
// 在 App.xaml.cs 中
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    AutoUpdater.Start("https://your-domain.com/update.xml");
}
```

### Q: 如何减少首次启动时间？

**A:** 使用 ReadyToRun (R2R) 编译：

```bash
dotnet publish -c Release -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishReadyToRun=true
```

注意：会增加文件大小，但提升启动速度。

---

## 自动化发布（GitHub Actions）

创建 `.github/workflows/release.yml`：

```yaml
name: Release

on:
  push:
    tags:
      - 'v*'

jobs:
  build-and-release:
    runs-on: windows-latest

    steps:
    - name: Checkout code
      uses: actions/checkout@v3

    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'

    - name: Publish Windows x64
      run: |
        cd Stratum.Desktop
        dotnet publish -c Release -r win-x64 --self-contained true `
          -p:PublishSingleFile=true `
          -p:IncludeNativeLibrariesForSelfExtract=true `
          -p:PublishTrimmed=true `
          -p:TrimMode=partial `
          -p:EnableCompressionInSingleFile=true

    - name: Create ZIP
      run: |
        $version = "${{ github.ref_name }}"
        $exePath = "Stratum.Desktop/bin/Release/net9.0-windows/win-x64/publish/Stratum.exe"
        $zipPath = "Stratum-Windows-x64-$version.zip"
        Compress-Archive -Path $exePath -DestinationPath $zipPath

    - name: Create Release
      uses: softprops/action-gh-release@v1
      with:
        files: |
          Stratum.Desktop/bin/Release/net9.0-windows/win-x64/publish/Stratum.exe
          Stratum-Windows-x64-${{ github.ref_name }}.zip
        body: |
          ## Stratum Desktop ${{ github.ref_name }}

          ### 下载
          - **Windows x64:** Stratum.exe
          - **Windows x64 (ZIP):** Stratum-Windows-x64-${{ github.ref_name }}.zip

          ### 系统要求
          - Windows 10/11 (x64)
          - 无需安装 .NET 运行时

          ### 完整更新日志
          详见 [IMPLEMENTATION_SUMMARY.md](https://github.com/${{ github.repository }}/blob/master/IMPLEMENTATION_SUMMARY.md)
      env:
        GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

**使用方法：**

```bash
# 创建并推送 tag
git tag v1.0.0
git push origin v1.0.0

# GitHub Actions 会自动：
# 1. 编译 Release 版本
# 2. 创建 ZIP 压缩包
# 3. 创建 GitHub Release
# 4. 上传发布文件
```

---

## 总结

**推荐的发布流程：**

1. **开发完成** → 测试所有功能
2. **更新版本号** → 修改 `.csproj` 文件
3. **运行发布脚本** → `publish-release.bat`
4. **测试发布文件** → 确保 exe 可正常运行
5. **创建 Git tag** → `git tag v1.0.0 && git push origin v1.0.0`
6. **创建 GitHub Release** → 使用 `gh` CLI 或网页
7. **上传文件** → exe 和 zip
8. **填写 Release Notes** → 使用模板
9. **发布** → 点击 Publish release
10. **通知用户** → 社交媒体/论坛/邮件

**文件清单：**
- ✅ `publish-release.bat` - Windows 发布脚本
- ✅ `publish-release.sh` - Linux/macOS 发布脚本
- ✅ `RELEASE_GUIDE.md` - 本发布指南
- ✅ Release Notes 模板
- ✅ GitHub Actions 配置（可选）

祝发布顺利！🚀
