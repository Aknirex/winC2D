# About 页面版本号自动同步方案

## 问题诊断

当前版本号分散在 4 处硬编码，且互不一致：

| 位置 | 当前值 | 问题 |
|---|---|---|
| `winC2D.App/winC2D.App.csproj:12` | `4.2.0` | 硬编码，需手动更新 |
| `winC2D.Cli/winC2D.Cli.csproj:9` | `4.2.0` | 硬编码，与 App 重复定义 |
| `installer/setup.iss:6` | `4.3.0` | 硬编码，与 csproj 不一致 |
| `Translations.cs` (7 语言) | `2.0.0` / `4.2.0` | 硬编码，各语言不一致 |

**根因**：About 页面通过 `_localizationService.GetString("About.Version")` 读取翻译字符串显示版本号，而非从程序集元数据读取。

## 方案

### 1. 统一版本定义 — `Directory.Build.props`

在仓库根目录创建 `Directory.Build.props`，所有项目自动继承：

```xml
<Project>
  <PropertyGroup>
    <Version>4.3.0</Version>
    <AssemblyVersion>4.3.0.0</AssemblyVersion>
    <FileVersion>4.3.0.0</FileVersion>
  </PropertyGroup>
</Project>
```

从 `winC2D.App.csproj` 和 `winC2D.Cli.csproj` 中移除重复的 `<Version>` / `<AssemblyVersion>` / `<FileVersion>`。

### 2. About 页面运行时读取版本号

修改 `AboutView.xaml.cs`，从程序集元数据读取：

```csharp
public string VersionDisplay
{
    get
    {
        var version = typeof(App).Assembly.GetName().Version;
        // 格式: "4.3.0"
        return version != null
            ? $"{version.Major}.{version.Minor}.{version.Build}"
            : "unknown";
    }
}
```

XAML 中版本文本改为使用 `VersionDisplay` 属性替代翻译字符串。

### 3. 翻译字符串去版本号

将 `Translations.cs` 中所有 `About.Version` 的值改为不含数字的格式字符串：

| 语言 | 旧值 | 新值 |
|---|---|---|
| en | `"Version: 2.0.0"` | `"Version: {0}"` |
| zh-CN | `"版本：4.2.0"` | `"版本：{0}"` |
| zh-Hant | `"版本：2.0.0"` | `"版本：{0}"` |
| ja | `"バージョン：2.0.0"` | `"バージョン：{0}"` |
| ko | `"버전: 2.0.0"` | `"버전: {0}"` |
| ru | `"Версия: 2.0.0"` | `"Версия: {0}"` |
| pt-BR | `"Versão: 2.0.0"` | `"Versão: {0}"` |

AboutView 中使用 `string.Format(localizedFormat, VersionDisplay)` 组合。

### 4. 安装器版本同步

`setup.iss` 中 `#define AppVersion "4.3.0"` 改为从 `Directory.Build.props` 解析的 CI 参数。当前 CI 已通过 `iscc /DAppVersion=$v` 传入，但 `#define` 硬编码值为回退默认值。

### 5. (可选) Git 标签自动推导

如果希望版本号完全由 Git 标签驱动，可引入 [Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning)：

- 安装 `nbgv` CLI 工具
- 在项目根生成 `version.json`
- 版本号从 Git 标签自动计算，无需手动修改任何文件

### 改动文件清单

| 操作 | 文件 |
|---|---|
| 新建 | `Directory.Build.props` |
| 修改 | `winC2D.App/winC2D.App.csproj` — 移除 Version/AssemblyVersion/FileVersion |
| 修改 | `winC2D.Cli/winC2D.Cli.csproj` — 同上 |
| 修改 | `winC2D.App/Views/AboutView.xaml` — 版本绑定改为 VersionDisplay |
| 修改 | `winC2D.App/Views/AboutView.xaml.cs` — 添加 VersionDisplay 属性 |
| 修改 | `winC2D.Infrastructure/Localization/Translations.cs` — 7 处 About.Version 值 |
| 修改 | `installer/setup.iss` — AppVersion 宏注释为回退值 |
