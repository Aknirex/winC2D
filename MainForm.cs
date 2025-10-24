using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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
            // 通过嵌入资源方式设置窗口图标，确保任务栏和窗口左上角都显示
            try
            {
                var asm = typeof(MainForm).Assembly;
                using var stream = asm.GetManifestResourceStream("winC2D.winc2d.ico");
                if (stream != null)
                    this.Icon = new System.Drawing.Icon(stream);
            }
            catch { }
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
            // 异步加载数据
            LoadAllDataAsync();

            // 绑定窗口图标点击事件
            this.MouseDown += MainForm_MouseDown;
        }

        private void MainForm_MouseDown(object sender, MouseEventArgs e)
        {
            // 检查是否点击了窗口左上角图标区域
            if (e.Button == MouseButtons.Left && e.X <= SystemInformation.FrameBorderSize.Width + SystemInformation.SmallIconSize.Width && e.Y <= SystemInformation.CaptionHeight)
            {
                ShowAppDetailDialog();
            }
        }

        private void ShowAppDetailDialog()
        {
            using (var dlg = new AppDetailForm(this.Icon))
            {
                dlg.ShowDialog();
            }
        }

        private async void LoadAllDataAsync()
        {
            // 软件列表
            listViewSoftware.Items.Clear();
            listViewSoftware.Items.Add(new ListViewItem(Localization.T("Msg.Loading")));
            // AppData
            listViewAppData.Items.Clear();
            listViewAppData.Items.Add(new ListViewItem(Localization.T("Msg.Loading")));

            await Task.WhenAll(
                Task.Run(() => LoadInstalledSoftwareSafe()),
                Task.Run(() => LoadAppDataFoldersSafe())
            );
        }

        private void LoadInstalledSoftwareSafe()
        {
            try
            {
                var list = SoftwareScanner.GetInstalledSoftwareOnC();
                this.Invoke(new Action(() => {
                    listViewSoftware.Items.Clear();
                    foreach (var sw in list)
                    {
                        var item = new ListViewItem(new string[] { sw.Name, sw.InstallLocation, sw.SizeText });
                        item.Tag = sw;
                        listViewSoftware.Items.Add(item);
                    }
                }));
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() => {
                    listViewSoftware.Items.Clear();
                    MessageBox.Show(string.Format(Localization.T("Msg.ScanSoftwareFailedFmt"), ex.Message), Localization.T("Title.Tip"));
                }));
            }
        }

        private void LoadAppDataFoldersSafe()
        {
            try
            {
                var items = new List<ListViewItem>();
                string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string localLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\LocalLow");
                void Scan(string basePath, string type)
                {
                    try
                    {
                        foreach (string dir in Directory.GetDirectories(basePath))
                        {
                            DirectoryInfo di = new DirectoryInfo(dir);
                            long size = AppDataMigrator.GetDirectorySize(dir);
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
                                items.Add(item);
                            }
                        }
                    }
                    catch { }
                }
                Scan(roaming, "Roaming");
                Scan(local, "Local");
                if (Directory.Exists(localLow))
                    Scan(localLow, "LocalLow");
                this.Invoke(new Action(() => {
                    listViewAppData.Items.Clear();
                    foreach (var item in items)
                        listViewAppData.Items.Add(item);
                }));
            }
            catch
            {
                this.Invoke(new Action(() => {
                    listViewAppData.Items.Clear();
                }));
            }
        }

        // 刷新按钮也异步
        private async void buttonRefreshAppData_Click(object sender, EventArgs e)
        {
            listViewAppData.Items.Clear();
            listViewAppData.Items.Add(new ListViewItem(Localization.T("Msg.Loading")));
            await Task.Run(() => LoadAppDataFoldersSafe());
            MessageBox.Show(Localization.T("Msg.MigrateCompleted").Replace("{0}", listViewAppData.Items.Count.ToString()).Replace("{1}", "0"),
                Localization.T("Title.Tip"));
        }

        private void OnLanguageChanged(object sender, EventArgs e)
        {
            ApplyLocalization();
            UpdateLanguageMenuItems();
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

            // Buttons
            buttonMigrateSoftware.Text = Localization.T("Button.MigrateSelected");
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

            menuLanguageChineseTraditional.Enabled = (currentLang != "zh-Hant");
            menuLanguageChineseTraditional.Checked = (currentLang == "zh-Hant");

            menuLanguageJapanese.Enabled = (currentLang != "ja");
            menuLanguageJapanese.Checked = (currentLang == "ja");

            menuLanguageKorean.Enabled = (currentLang != "ko");
            menuLanguageKorean.Checked = (currentLang == "ko");

            menuLanguageRussian.Enabled = (currentLang != "ru");
            menuLanguageRussian.Checked = (currentLang == "ru");

            menuLanguagePortuguese.Enabled = (currentLang != "pt-BR");
            menuLanguagePortuguese.Checked = (currentLang == "pt-BR");
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

        private void menuLanguageChineseTraditional_Click(object sender, EventArgs e)
        {
            if (Localization.CurrentLanguage != "zh-Hant")
            {
                Localization.SetLanguage("zh-Hant");
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

        private void menuLanguageRussian_Click(object sender, EventArgs e)
        {
            if (Localization.CurrentLanguage != "ru")
            {
                Localization.SetLanguage("ru");
            }
        }

        private void menuLanguagePortuguese_Click(object sender, EventArgs e)
        {
            if (Localization.CurrentLanguage != "pt-BR")
            {
                Localization.SetLanguage("pt-BR");
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

        protected override void WndProc(ref Message m)
        {
            const int WM_NCLBUTTONDOWN = 0x00A1;
            const int WM_NCLBUTTONDBLCLK = 0x00A3;
            const int HTSYSMENU = 3;

            if (m.Msg == WM_NCLBUTTONDOWN && (m.WParam.ToInt32() == HTSYSMENU))
            {
                ShowAppDetailDialog();
                return;
            }
            if (m.Msg == WM_NCLBUTTONDBLCLK && (m.WParam.ToInt32() == HTSYSMENU))
            {
                // 阻止双击左上角关闭窗口
                return;
            }
            base.WndProc(ref m);
        }
    }
}
