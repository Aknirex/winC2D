# winC2D 翻译指南 / Translation Guide

## 当前语言支持状态 / Current Language Support Status

| 语言 / Language | 代码 / Code | 状态 / Status | 完成度 / Completion |
|-----------------|-------------|--------------|-------------------|
| English | `en` | ? 完整 / Complete | 100% |
| 简体中文 / Simplified Chinese | `zh-CN` | ? 完整 / Complete | 100% |
| 日本語 / Japanese | `ja` | ?? 待翻译 / Needs Translation | 0% (English fallback) |
| ??? / Korean | `ko` | ?? 待翻译 / Needs Translation | 0% (English fallback) |
| Fran?ais / French | `fr` | ?? 待翻译 / Needs Translation | 0% (English fallback) |
| Deutsch / German | `de` | ?? 待翻译 / Needs Translation | 0% (English fallback) |
| Espa?ol / Spanish | `es` | ?? 待翻译 / Needs Translation | 0% (English fallback) |
| Русский / Russian | `ru` | ?? 待翻译 / Needs Translation | 0% (English fallback) |

## 如何添加翻译 / How to Add Translations

### 步骤 1: 编辑资源文件 / Step 1: Edit Resource File

根据您要翻译的语言，编辑对应的资源文件：
Edit the corresponding resource file for your target language:

- 日语 / Japanese: `Strings.ja.resx`
- 韩语 / Korean: `Strings.ko.resx`
- 法语 / French: `Strings.fr.resx`
- 德语 / German: `Strings.de.resx`
- 西班牙语 / Spanish: `Strings.es.resx`
- 俄语 / Russian: `Strings.ru.resx`

### 步骤 2: 翻译字符串 / Step 2: Translate Strings

在资源文件中，找到所有 `<value>` 标签中的英文文本并翻译成目标语言。
In the resource file, find all English text within `<value>` tags and translate to your target language.

例如 / Example:

```xml
<!-- 英文 / English -->
<data name="Title.MainWindow" xml:space="preserve">
  <value>winC2D</value>
</data>

<!-- 翻译为日语 / Translate to Japanese -->
<data name="Title.MainWindow" xml:space="preserve">
  <value>winC2D</value>
</data>

<!-- 翻译为法语 / Translate to French -->
<data name="Title.MainWindow" xml:space="preserve">
  <value>winC2D</value>
</data>
```

### 步骤 3: 重要字符串列表 / Step 3: Important Strings List

以下是需要翻译的主要字符串类别：
Here are the main categories of strings to translate:

#### 窗口标题 / Window Titles
- `Title.MainWindow` - 主窗口标题
- `Title.Tip` - 提示
- `Title.Error` - 错误
- `Log.Title` - 迁移日志
- `Settings.Title` - 设置

#### 菜单项 / Menu Items
- `Menu.Log` - 迁移日志
- `Menu.Language` - 语言

#### 选项卡 / Tabs
- `Tab.SoftwareMigration` - 软件迁移
- `Tab.UserFolders` - 用户文件夹
- `Tab.AppData` - AppData (mklink)
- `GroupBox.SystemSettings` - 系统设置

#### 按钮 / Buttons
- `Button.MigrateSelected` - 迁移所选
- `Button.RefreshAppData` - 刷新列表
- `Button.Browse` - 浏览
- `Button.Apply` - 应用
- `Button.Reset` - 恢复默认
- `Button.Rollback` - 回滚所选
- `Button.OpenSettings` - 打开设置
- `Button.OpenWindowsStorage` - 打开 Windows 设置

#### 列标题 / Column Headers
- `Column.SoftwareName` - 软件名称
- `Column.InstallPath` - 安装路径
- `Column.Size` - 大小
- `Column.FolderName` - 文件夹名称
- `Column.CurrentPath` - 当前路径
- `Column.AppName` - 应用名称
- `Column.AppPath` - 路径
- `Column.AppSize` - 大小
- `Column.AppType` - 类型

#### 文件夹名称 / Folder Names
- `Folder.Documents` - 文档
- `Folder.Pictures` - 图片
- `Folder.Downloads` - 下载
- `Folder.Videos` - 视频
- `Folder.Desktop` - 桌面

#### 设置 / Settings
- `Settings.ProgramFilesSection` - Program Files 位置（传统桌面程序）
- `Settings.ProgramFilesPath` - Program Files 默认位置（64位）
- `Settings.ProgramFilesPathX86` - Program Files 默认位置（32位）
- `Settings.CustomX86Path` - 自定义 32位 程序路径
- `Settings.ProgramFilesNote` - 提示信息
- `Settings.StoragePolicySection` - 新内容保存位置
- `Settings.StoragePolicyNote` - 提示信息

#### 消息 / Messages
- `Msg.SelectFolders` - 请选择要迁移的文件夹
- `Msg.SelectSoftware` - 请选择要迁移的软件
- `Msg.SelectAppData` - 请选择要迁移的AppData文件夹
- `Msg.MigrateCompleted` - 迁移完成！成功：{0}，失败：{1}
- `Msg.MklinkNote` - 注意：此操作使用 mklink 创建符号链接
- `Msg.InvalidPath` - 请输入有效的文件夹路径
- `Msg.SettingsApplied` - 设置已成功应用！
- `Msg.SelectLogEntry` - 请先选择要回滚的迁移记录
- `Msg.RollbackSuccess` - 回滚成功！

### 步骤 4: 测试翻译 / Step 4: Test Translation

1. 编译项目 / Build the project
2. 运行程序 / Run the application
3. 在菜单中选择您的语言 / Select your language from the menu
4. 检查所有UI元素是否正确显示 / Check if all UI elements display correctly

### 步骤 5: 提交贡献 / Step 5: Submit Contribution

如果您完成了某种语言的翻译，欢迎提交 Pull Request！
If you've completed a translation, feel free to submit a Pull Request!

## 翻译注意事项 / Translation Notes

1. **保持格式占位符** / Keep format placeholders
   - 如 `{0}`, `{1}` 等必须保留在翻译后的文本中
   - Placeholders like `{0}`, `{1}` must be kept in the translated text

2. **特殊字符** / Special characters
   - `?` 信息图标应保留 / Info icon should be kept
   - `\n` 换行符应保留 / Newline characters should be kept

3. **长度限制** / Length constraints
   - 按钮文本应尽量简短 / Button text should be concise
   - 列标题应适合列宽 / Column headers should fit column width

4. **上下文** / Context
   - 如有疑问，请参考英文和中文版本 / When in doubt, refer to English and Chinese versions
   - 保持UI的一致性和专业性 / Maintain UI consistency and professionalism

## 联系方式 / Contact

如有翻译相关问题，请在 GitHub Issues 中提出。
For translation-related questions, please create an issue on GitHub.

## 翻译示例 / Translation Examples

### 日语翻译示例 / Japanese Translation Example

```xml
<!-- 英文 English -->
<data name="Button.Browse" xml:space="preserve">
  <value>Browse</value>
</data>

<!-- 日语 Japanese -->
<data name="Button.Browse" xml:space="preserve">
  <value>参照</value>
</data>
```

### 常用术语对照表 / Common Terms Translation Table

| English | 中文 | 日本語 | ??? | Fran?ais | Deutsch | Espa?ol | Русский |
|---------|------|--------|--------|----------|---------|---------|---------|
| Browse | 浏览 | 参照 | ???? | Parcourir | Durchsuchen | Examinar | Обзор |
| Apply | 应用 | 適用 | ?? | Appliquer | Anwenden | Aplicar | Применить |
| Reset | 恢复默认 | リセット | ??? | Réinitialiser | Zurücksetzen | Restablecer | Сбросить |
| Documents | 文档 | ドキュメント | ?? | Documents | Dokumente | Documentos | Документы |
| Pictures | 图片 | ピクチャ | ?? | Images | Bilder | Imágenes | Изображения |
| Downloads | 下载 | ダウンロード | ???? | Téléchargements | Downloads | Descargas | Загрузки |
| Videos | 视频 | ビデオ | ??? | Vidéos | Videos | Vídeos | Видео |
| Desktop | 桌面 | デスクトップ | ?? ?? | Bureau | Desktop | Escritorio | Рабочий стол |
| Migration | 迁移 | 移行 | ?????? | Migration | Migration | Migración | Миграция |
| Software | 软件 | ソフトウェア | ????? | Logiciel | Software | Software | Программное обеспечение |
| Settings | 设置 | 設定 | ?? | Paramètres | Einstellungen | Configuración | Настройки |
| Language | 语言 | 言語 | ?? | Langue | Sprache | Idioma | Язык |

## 使用 Visual Studio 编辑资源文件 / Editing with Visual Studio

**推荐方法 / Recommended Method**:
1. 在 Visual Studio 中打开项目
2. 双击 `.resx` 文件会打开可视化资源编辑器
3. 在编辑器中直接修改翻译文本
4. Visual Studio 会自动处理编码问题

**重要提示 / Important Notes**:
- ?? 不要使用普通文本编辑器编辑 `.resx` 文件，可能导致编码错误
- ? 推荐使用 Visual Studio 或 Visual Studio Code 的 resx 编辑器扩展
- ? 所有 resx 文件必须使用 UTF-8 with BOM 编码

## 当前翻译完成度 / Current Translation Completion

| 语言 | 基础框架 | 核心UI | 消息提示 | 设置页面 | 日志页面 | 总完成度 |
|------|----------|--------|----------|----------|----------|----------|
| English (en) | ? | ? | ? | ? | ? | **100%** |
| 简体中文 (zh-CN) | ? | ? | ? | ? | ? | **100%** |
| 日本語 (ja) | ? | ?? | ?? | ?? | ?? | **20%** |
| ??? (ko) | ? | ? | ? | ? | ? | **0%** |
| Fran?ais (fr) | ? | ? | ? | ? | ? | **0%** |
| Deutsch (de) | ? | ? | ? | ? | ? | **0%** |
| Espa?ol (es) | ? | ? | ? | ? | ? | **0%** |
| Русский (ru) | ? | ? | ? | ? | ? | **0%** |

## 感谢贡献者 / Thanks to Contributors

感谢所有为 winC2D 国际化做出贡献的翻译者！
Thanks to all translators who contribute to winC2D internationalization!

### 如何成为贡献者 / How to Become a Contributor

1. Fork 本项目 / Fork this repository
2. 选择一种语言开始翻译 / Choose a language to translate
3. 编辑对应的 `Strings.xx.resx` 文件 / Edit the corresponding `Strings.xx.resx` file
4. 测试翻译效果 / Test your translation
5. 提交 Pull Request / Submit a Pull Request
6. 您的名字将出现在贡献者列表中 / Your name will be added to the contributors list!

## 翻译工具推荐 / Recommended Translation Tools

- **Visual Studio 2022** - 内置 resx 编辑器 / Built-in resx editor
- **Resxer** - 免费的 resx 编辑工具 / Free resx editor tool
- **DeepL** - 机器翻译参考 / Machine translation reference (质量较好 / Good quality)
- **Google Translate** - 机器翻译参考 / Machine translation reference

**注意**: 机器翻译仅供参考，请务必人工审核和修正
**Note**: Machine translations are for reference only, please review and correct manually
