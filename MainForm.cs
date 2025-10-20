using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace winC2D
{
    public partial class MainForm : Form
    {
        private const string DEFAULT_PROGRAM_FILES = @"C:\Program Files";
        private const string DEFAULT_PROGRAM_FILES_X86 = @"C:\Program Files (x86)";
        private const string REG_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion";

        public MainForm()
        {
            InitializeComponent();
            this.Load += MainForm_Load;
            this.FormClosing += MainForm_FormClosing;
            Localization.LanguageChanged += OnLanguageChanged;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Localization.LanguageChanged -= OnLanguageChanged;
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            ApplyLocalization();
            UpdateLanguageMenuItems();
            LoadSystemSettings();
            LoadInstalledSoftware();
            LoadUserFolders();
            LoadAppDataFolders();
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLocalization();
            UpdateLanguageMenuItems();
            LoadUserFolders(); // Refresh to update localized folder names
            LoadAppDataFolders(); // Refresh AppData list
        }

        private void ApplyLocalization()
        {
            // Window title
            this.Text = Localization.T("Title.MainWindow");

            // Menu items
            menuLog.Text = Localization.T("Menu.Log");
            menuLanguage.Text = Localization.T("Menu.Language");

            // Tab pages
            tabPageSettings.Text = Localization.T("GroupBox.SystemSettings");
            tabPageSoftware.Text = Localization.T("Tab.SoftwareMigration");
            tabPageFolders.Text = Localization.T("Tab.UserFolders");
            tabPageAppData.Text = Localization.T("Tab.AppData");

            // Settings tab
            groupBoxProgramFiles.Text = Localization.T("Settings.ProgramFilesSection");
            labelProgramFiles.Text = Localization.T("Settings.ProgramFilesPath");
            labelProgramFilesX86.Text = Localization.T("Settings.ProgramFilesPathX86");
            checkBoxCustomX86.Text = Localization.T("Settings.CustomX86Path");
            labelProgramFilesNote.Text = Localization.T("Settings.ProgramFilesNote");
            groupBoxStoragePolicy.Text = Localization.T("Settings.StoragePolicySection");
            labelStoragePolicyNote.Text = Localization.T("Settings.StoragePolicyNote");
            buttonOpenWindowsStorage.Text = Localization.T("Button.OpenWindowsStorage");
            buttonBrowseProgramFiles.Text = Localization.T("Button.Browse");
            buttonBrowseProgramFilesX86.Text = Localization.T("Button.Browse");
            buttonApplyProgramFiles.Text = Localization.T("Button.Apply");
            buttonResetProgramFiles.Text = Localization.T("Button.Reset");

            // Column headers for software
            columnHeaderName.Text = Localization.T("Column.SoftwareName");
            columnHeaderPath.Text = Localization.T("Column.InstallPath");
            columnHeaderSize.Text = Localization.T("Column.Size");

            // Column headers for folders
            columnHeaderFolderName.Text = Localization.T("Column.FolderName");
            columnHeaderFolderPath.Text = Localization.T("Column.CurrentPath");

            // Column headers for AppData
            columnHeaderAppName.Text = Localization.T("Column.AppName");
            columnHeaderAppPath.Text = Localization.T("Column.AppPath");
            columnHeaderAppSize.Text = Localization.T("Column.AppSize");
            columnHeaderAppType.Text = Localization.T("Column.AppType");

            // Buttons
            buttonMigrateSoftware.Text = Localization.T("Button.MigrateSelected");
            buttonMigrateFolders.Text = Localization.T("Button.MigrateSelected");
            buttonMigrateAppData.Text = Localization.T("Button.MigrateSelected");
            buttonRefreshAppData.Text = Localization.T("Button.RefreshAppData");

            // Labels
            labelMklinkNote.Text = Localization.T("Msg.MklinkNote");
        }

        private void UpdateLanguageMenuItems()
        {
            string currentLang = Localization.CurrentLanguage;

            // Update all language menu items
            menuLanguageEnglish.Enabled = (currentLang != "en");
            menuLanguageEnglish.Checked = (currentLang == "en");

            menuLanguageChinese.Enabled = (currentLang != "zh-CN");
            menuLanguageChinese.Checked = (currentLang == "zh-CN");

            menuLanguageJapanese.Enabled = (currentLang != "ja");
            menuLanguageJapanese.Checked = (currentLang == "ja");

            menuLanguageKorean.Enabled = (currentLang != "ko");
            menuLanguageKorean.Checked = (currentLang == "ko");

            menuLanguageFrench.Enabled = (currentLang != "fr");
            menuLanguageFrench.Checked = (currentLang == "fr");

            menuLanguageGerman.Enabled = (currentLang != "de");
            menuLanguageGerman.Checked = (currentLang == "de");

            menuLanguageSpanish.Enabled = (currentLang != "es");
            menuLanguageSpanish.Checked = (currentLang == "es");

            menuLanguageRussian.Enabled = (currentLang != "ru");
            menuLanguageRussian.Checked = (currentLang == "ru");
        }

        private class UserFolderInfo
        {
            public string Name { get; set; }
            public string Path { get; set; }
        }

        private void LoadUserFolders()
        {
            var folders = new List<UserFolderInfo>
            {
                new UserFolderInfo { Name = "文档", Path = GetUserShellFolderPath("Personal") },
                new UserFolderInfo { Name = "图片", Path = GetUserShellFolderPath("My Pictures") },
                new UserFolderInfo { Name = "下载", Path = GetUserShellFolderPath("{374DE290-123F-4565-9164-39C4925E467B}") },
                new UserFolderInfo { Name = "视频", Path = GetUserShellFolderPath("My Video") },
                new UserFolderInfo { Name = "桌面", Path = GetUserShellFolderPath("Desktop") }
            };
            listViewFolders.Items.Clear();
            foreach (var f in folders)
            {
                if (!string.IsNullOrEmpty(f.Path))
                {
                    // 显示本地化名称，但内部Name保持为键值以便迁移逻辑使用
                    var displayName = GetDisplayNameForFolderKey(f.Name);
                    var item = new ListViewItem(new string[] { displayName, f.Path });
                    item.Tag = f;
                    listViewFolders.Items.Add(item);
                }
            }
        } // end of LoadUserFolders

        private static string GetDisplayNameForFolderKey(string key)
        {
            return key switch
            {
                "文档" => Localization.T("Folder.Documents"),
                "图片" => Localization.T("Folder.Pictures"),
                "下载" => Localization.T("Folder.Downloads"),
                "视频" => Localization.T("Folder.Videos"),
                "桌面" => Localization.T("Folder.Desktop"),
                _ => key
            };
        }

        // 读取注册表User Shell Folders，获取实际路径
        private static string GetUserShellFolderPath(string regName)
        {
            try
            {
                var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Explorer\\User Shell Folders", false);
                if (key != null)
                {
                    var val = key.GetValue(regName) as string;
                    if (!string.IsNullOrEmpty(val))
                    {
                        // 展开环境变量
                        return Environment.ExpandEnvironmentVariables(val);
                    }
                }
            }
            catch { }
            return string.Empty;
        }

        // 获取KnownFolder路径（如下载）
        private static string GetKnownFolder(string knownFolderId)
        {
            try
            {
                var type = Type.GetTypeFromCLSID(new Guid("{4DF0C730-DF9D-4AE3-9153-AA6B82E9795A}"));
                dynamic shell = Activator.CreateInstance(type);
                var folder = shell.GetKnownFolderPath(knownFolderId);
                return folder;
            }
            catch
            {
                // 兼容性处理
                string user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrEmpty(user))
                {
                    return Path.Combine(user, "Downloads");
                }
                return string.Empty;
            }
        }

        private void LoadInstalledSoftware()
        {
            // 读取注册表，获取C盘已安装软件列表
            try
            {
                var list = SoftwareScanner.GetInstalledSoftwareOnC();
                listViewSoftware.Items.Clear();
                foreach (var sw in list)
                {
                    var item = new ListViewItem(new string[] { sw.Name, sw.InstallLocation, sw.SizeText });
                    item.Tag = sw;
                    listViewSoftware.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Localization.T("Msg.ScanSoftwareFailedFmt"), ex.Message), Localization.T("Title.Tip"));
            }
        }

        private void menuLog_Click(object sender, EventArgs e)
        {
            using (var logForm = new LogForm())
            {
                logForm.ShowDialog();
            }
        }

        private void buttonMigrateFolders_Click(object sender, EventArgs e)
        {
            var selected = listViewFolders.CheckedItems.Cast<ListViewItem>().Select(i => i.Tag as UserFolderInfo).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(Localization.T("Msg.SelectFolders"), Localization.T("Title.Tip"));
                return;
            }
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = Localization.T("Desc.TargetFolderForUserFolders");
                fbd.ShowNewFolderButton = true;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string targetRoot = fbd.SelectedPath;
                    int success = 0, fail = 0;
                    foreach (var folder in selected)
                    {
                        try
                        {
                            UserFolderMigrator.MigrateAndRedirect(folder.Name, folder.Path, targetRoot);
                            success++;
                        }
                        catch (Exception ex)
                        {
                            fail++;
                            MessageBox.Show(string.Format(Localization.T("Msg.MigrateFailedFmt"), folder.Name, ex.Message), Localization.T("Title.Error"));
                        }
                    }
                    MessageBox.Show(string.Format(Localization.T("Msg.MigrateCompleted"), success, fail), Localization.T("Title.Tip"));
                    LoadUserFolders();
                }
            }
        }

        private void buttonMigrateSoftware_Click(object sender, EventArgs e)
        {
            var selected = listViewSoftware.CheckedItems.Cast<ListViewItem>().Select(i => i.Tag as InstalledSoftware).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(Localization.T("Msg.SelectSoftware"), Localization.T("Title.Tip"));
                return;
            }
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = Localization.T("Desc.TargetFolderForSoftware");
                fbd.ShowNewFolderButton = true;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string targetRoot = fbd.SelectedPath;
                    int success = 0, fail = 0;
                    foreach (var sw in selected)
                    {
                        try
                        {
                            SoftwareMigrator.MigrateSoftware(sw, targetRoot);
                            success++;
                            MigrationLogger.Log(new MigrationLogEntry
                            {
                                Time = DateTime.Now,
                                SoftwareName = sw.Name,
                                OldPath = sw.InstallLocation,
                                NewPath = Path.Combine(targetRoot, Path.GetFileName(sw.InstallLocation)),
                                Status = "Success",
                                Message = string.Empty
                            });
                        }
                        catch (Exception ex)
                        {
                            fail++;
                            MigrationLogger.Log(new MigrationLogEntry
                            {
                                Time = DateTime.Now,
                                SoftwareName = sw.Name,
                                OldPath = sw.InstallLocation,
                                NewPath = Path.Combine(targetRoot, Path.GetFileName(sw.InstallLocation)),
                                Status = "Fail",
                                Message = ex.Message
                            });
                            MessageBox.Show(string.Format(Localization.T("Msg.MigrateFailedFmt"), sw.Name, ex.Message), Localization.T("Title.Error"));
                        }
                    }
                    MessageBox.Show(string.Format(Localization.T("Msg.MigrateCompleted"), success, fail), Localization.T("Title.Tip"));
                    LoadInstalledSoftware(); // 刷新列表
                }
            }
        }

        private void menuLanguageEnglish_Click(object sender, EventArgs e)
        {
            if (Localization.CurrentLanguage != "en")
            {
                Localization.SetLanguage("en");
            }
        }

        private void menuLanguageChinese_Click(object sender, EventArgs e)
        {
            if (Localization.CurrentLanguage != "zh-CN")
            {
                Localization.SetLanguage("zh-CN");
            }
        }

        private void menuLanguageJapanese_Click(object sender, EventArgs e)
        {
            if (Localization.CurrentLanguage != "ja")
            {
                Localization.SetLanguage("ja");
            }
        }

        private void menuLanguageKorean_Click(object sender, EventArgs e)
        {
            if (Localization.CurrentLanguage != "ko")
            {
                Localization.SetLanguage("ko");
            }
        }

        private void menuLanguageFrench_Click(object sender, EventArgs e)
        {
            if (Localization.CurrentLanguage != "fr")
            {
                Localization.SetLanguage("fr");
            }
        }

        private void menuLanguageGerman_Click(object sender, EventArgs e)
        {
            if (Localization.CurrentLanguage != "de")
            {
                Localization.SetLanguage("de");
            }
        }

        private void menuLanguageSpanish_Click(object sender, EventArgs e)
        {
            if (Localization.CurrentLanguage != "es")
            {
                Localization.SetLanguage("es");
            }
        }

        private void menuLanguageRussian_Click(object sender, EventArgs e)
        {
            if (Localization.CurrentLanguage != "ru")
            {
                Localization.SetLanguage("ru");
            }
        }

        private void LoadSystemSettings()
        {
            try
            {
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(REG_PATH, false))
                {
                    if (key != null)
                    {
                        // 读取 64 位路径
                        string path64 = key.GetValue("ProgramFilesDir") as string;
                        if (!string.IsNullOrEmpty(path64))
                            textBoxProgramFiles.Text = path64;
                        else
                            textBoxProgramFiles.Text = DEFAULT_PROGRAM_FILES;

                        // 读取 32 位路径
                        if (Environment.Is64BitOperatingSystem)
                        {
                            string path32 = key.GetValue("ProgramFilesDir (x86)") as string;
                            if (!string.IsNullOrEmpty(path32))
                                textBoxProgramFilesX86.Text = path32;
                            else
                                textBoxProgramFilesX86.Text = DEFAULT_PROGRAM_FILES_X86;
                        }
                        else
                        {
                            // 32位系统隐藏32位选项
                            checkBoxCustomX86.Visible = false;
                            labelProgramFilesX86.Visible = false;
                            textBoxProgramFilesX86.Visible = false;
                            buttonBrowseProgramFilesX86.Visible = false;
                        }
                    }
                }
            }
            catch { }
        }

        private void textBoxProgramFiles_TextChanged(object sender, EventArgs e)
        {
            // 如果未勾选自定义32位路径，自动设置
            if (!checkBoxCustomX86.Checked && Environment.Is64BitOperatingSystem)
            {
                string path64 = textBoxProgramFiles.Text;
                if (!string.IsNullOrEmpty(path64))
                {
                    string pathX86 = path64.Replace("Program Files", "Program Files (x86)");
                    textBoxProgramFilesX86.Text = pathX86;
                }
            }
        }

        private void checkBoxCustomX86_CheckedChanged(object sender, EventArgs e)
        {
            bool customEnabled = checkBoxCustomX86.Checked;
            labelProgramFilesX86.Enabled = customEnabled;
            textBoxProgramFilesX86.Enabled = customEnabled;
            buttonBrowseProgramFilesX86.Enabled = customEnabled;

            if (!customEnabled)
            {
                textBoxProgramFiles_TextChanged(null, null);
            }
        }

        private void buttonBrowseProgramFiles_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = Localization.T("Msg.SelectFolder");
                fbd.ShowNewFolderButton = true;
                fbd.SelectedPath = textBoxProgramFiles.Text;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    textBoxProgramFiles.Text = fbd.SelectedPath;
                }
            }
        }

        private void buttonBrowseProgramFilesX86_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = Localization.T("Msg.SelectFolder");
                fbd.ShowNewFolderButton = true;
                fbd.SelectedPath = textBoxProgramFilesX86.Text;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    textBoxProgramFilesX86.Text = fbd.SelectedPath;
                }
            }
        }

        private void buttonApplyProgramFiles_Click(object sender, EventArgs e)
        {
            string path64 = textBoxProgramFiles.Text.Trim();
            string pathX86 = textBoxProgramFilesX86.Text.Trim();

            if (string.IsNullOrEmpty(path64) || !Directory.Exists(path64))
            {
                MessageBox.Show(
                    Localization.T("Msg.InvalidPath"),
                    Localization.T("Title.Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (Environment.Is64BitOperatingSystem)
            {
                if (string.IsNullOrEmpty(pathX86) || !Directory.Exists(pathX86))
                {
                    MessageBox.Show(
                        Localization.T("Msg.InvalidPath"),
                        Localization.T("Title.Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                SetProgramFilesPaths(path64, pathX86);
                MessageBox.Show(
                    Localization.T("Msg.SettingsApplied"),
                    Localization.T("Title.Tip"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Localization.T("Msg.SettingsFailed"), ex.Message),
                    Localization.T("Title.Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void buttonResetProgramFiles_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                Localization.T("Msg.ResetConfirm"),
                Localization.T("Title.Tip"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                try
                {
                    SetProgramFilesPaths(DEFAULT_PROGRAM_FILES, DEFAULT_PROGRAM_FILES_X86);
                    textBoxProgramFiles.Text = DEFAULT_PROGRAM_FILES;
                    textBoxProgramFilesX86.Text = DEFAULT_PROGRAM_FILES_X86;
                    checkBoxCustomX86.Checked = false;
                    MessageBox.Show(
                        Localization.T("Msg.SettingsApplied"),
                        Localization.T("Title.Tip"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        string.Format(Localization.T("Msg.SettingsFailed"), ex.Message),
                        Localization.T("Title.Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        private void SetProgramFilesPaths(string path64, string pathX86)
        {
            using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(REG_PATH, true))
            {
                if (key == null)
                    throw new Exception("Unable to open registry key");

                key.SetValue("ProgramFilesDir", path64);

                if (Environment.Is64BitOperatingSystem)
                {
                    key.SetValue("ProgramW6432Dir", path64);
                    key.SetValue("ProgramFilesDir (x86)", pathX86);
                }
            }
        }

        private void buttonEditSettings_Click(object sender, EventArgs e)
        {
            using (var settingsForm = new SettingsForm())
            {
                settingsForm.ShowDialog();
                // 刷新显示的设置值
                LoadSystemSettings();
            }
        }

        private void buttonOpenWindowsStorage_Click(object sender, EventArgs e)
        {
            try
            {
                // 打开 Windows 设置 -> 系统 -> 存储 -> 高级存储设置 -> 新内容的保存位置
                // Windows 10/11 使用 ms-settings URI scheme
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ms-settings:savelocations",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"无法打开 Windows 设置：{ex.Message}\n\n您可以手动打开：\n设置 → 系统 → 存储 → 高级存储设置 → 新内容的保存位置",
                    Localization.T("Title.Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        private void LoadAppDataFolders()
        {
            listViewAppData.Items.Clear();

            // 获取 AppData 各个路径
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string localLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\LocalLow");

            // 扫描 Roaming
            ScanAppDataFolder(roaming, "Roaming");

            // 扫描 Local
            ScanAppDataFolder(local, "Local");

            // 扫描 LocalLow
            if (Directory.Exists(localLow))
            {
                ScanAppDataFolder(localLow, "LocalLow");
            }
        }

        private void ScanAppDataFolder(string basePath, string type)
        {
            try
            {
                foreach (string dir in Directory.GetDirectories(basePath))
                {
                    try
                    {
                        DirectoryInfo di = new DirectoryInfo(dir);
                        long size = AppDataMigrator.GetDirectorySize(dir);

                        // 只显示大于 1MB 的文件夹
                        if (size > 1024 * 1024)
                        {
                            var appData = new AppDataInfo
                            {
                                Name = di.Name,
                                Path = dir,
                                Size = size,
                                Type = type
                            };

                            var item = new ListViewItem(new string[]
                            {
                                appData.Name,
                                appData.Path,
                                appData.SizeText,
                                appData.Type
                            });
                            item.Tag = appData;
                            listViewAppData.Items.Add(item);
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private void buttonRefreshAppData_Click(object sender, EventArgs e)
        {
            LoadAppDataFolders();
            MessageBox.Show(Localization.T("Msg.MigrateCompleted").Replace("{0}", listViewAppData.Items.Count.ToString()).Replace("{1}", "0"), 
                Localization.T("Title.Tip"));
        }

        private void buttonMigrateAppData_Click(object sender, EventArgs e)
        {
            var selected = listViewAppData.CheckedItems.Cast<ListViewItem>().Select(i => i.Tag as AppDataInfo).ToList();
            if (selected.Count == 0)
            {
                MessageBox.Show(Localization.T("Msg.SelectAppData"), Localization.T("Title.Tip"));
                return;
            }

            // 显示 mklink 提示
            var result = MessageBox.Show(
                Localization.T("Msg.MklinkNote"),
                Localization.T("Title.Tip"),
                MessageBoxButtons.OKCancel,
                MessageBoxIcon.Information);

            if (result != DialogResult.OK)
                return;

            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = Localization.T("Desc.TargetFolderForAppData");
                fbd.ShowNewFolderButton = true;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string targetRoot = fbd.SelectedPath;
                    int success = 0, fail = 0;
                    foreach (var app in selected)
                    {
                        try
                        {
                            AppDataMigrator.MigrateWithMklink(app.Name, app.Path, targetRoot);
                            success++;
                            MigrationLogger.Log(new MigrationLogEntry
                            {
                                Time = DateTime.Now,
                                SoftwareName = $"{app.Name} (AppData-{app.Type})",
                                OldPath = app.Path,
                                NewPath = Path.Combine(targetRoot, "AppData", app.Name),
                                Status = "Success",
                                Message = "Migrated using mklink"
                            });
                        }
                        catch (Exception ex)
                        {
                            fail++;
                            MigrationLogger.Log(new MigrationLogEntry
                            {
                                Time = DateTime.Now,
                                SoftwareName = $"{app.Name} (AppData-{app.Type})",
                                OldPath = app.Path,
                                NewPath = Path.Combine(targetRoot, "AppData", app.Name),
                                Status = "Fail",
                                Message = ex.Message
                            });
                            MessageBox.Show(string.Format(Localization.T("Msg.MigrateFailedFmt"), app.Name, ex.Message), Localization.T("Title.Error"));
                        }
                    }
                    MessageBox.Show(string.Format(Localization.T("Msg.MigrateCompleted"), success, fail), Localization.T("Title.Tip"));
                    LoadAppDataFolders(); // 刷新列表
                }
            }
        }
    }
}
