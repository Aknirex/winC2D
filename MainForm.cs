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

            // 在构造函数或MainForm_Load中添加列排序事件绑定
            listViewAppData.ColumnClick += listViewAppData_ColumnClick;
            listViewSoftware.ColumnClick += listViewSoftware_ColumnClick;
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
                // 获取当前设置和系统默认路径
                var paths = new List<string>();
                string user64 = textBoxProgramFiles.InvokeRequired ? (string)textBoxProgramFiles.Invoke(new Func<string>(() => textBoxProgramFiles.Text.Trim())) : textBoxProgramFiles.Text.Trim();
                string userX86 = textBoxProgramFilesX86.InvokeRequired ? (string)textBoxProgramFilesX86.Invoke(new Func<string>(() => textBoxProgramFilesX86.Text.Trim())) : textBoxProgramFilesX86.Text.Trim();
                // 用户设置
                if (!string.IsNullOrEmpty(user64)) paths.Add(user64);
                if (Environment.Is64BitOperatingSystem && !string.IsNullOrEmpty(userX86)) paths.Add(userX86);
                // 系统默认
                string sys64 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string sysX86 = Environment.Is64BitOperatingSystem ? Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86) : null;
                if (!string.IsNullOrEmpty(sys64) && !paths.Contains(sys64, StringComparer.OrdinalIgnoreCase)) paths.Add(sys64);
                if (!string.IsNullOrEmpty(sysX86) && !paths.Contains(sysX86, StringComparer.OrdinalIgnoreCase)) paths.Add(sysX86);
                // 始终包含C盘标准路径
                if (!paths.Contains(@"C:\Program Files", StringComparer.OrdinalIgnoreCase)) paths.Add(@"C:\Program Files");
                if (Environment.Is64BitOperatingSystem && !paths.Contains(@"C:\Program Files (x86)", StringComparer.OrdinalIgnoreCase)) paths.Add(@"C:\Program Files (x86)");

                var list = SoftwareScanner.GetInstalledSoftwareOnC(paths);
                this.Invoke(new Action(() => {
                    listViewSoftware.Items.Clear();
                    foreach (var sw in list)
                    {
                        string name = sw.Name;
                        string path = sw.InstallLocation;
                        string statusText = string.Empty;
                        try
                        {
                            if (Directory.Exists(path))
                            {
                                var isSymlink = (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
                                statusText = isSymlink ? Localization.T("Status.Symlink") : Localization.T("Status.Directory");
                            }
                            else
                            {
                                statusText = Localization.T("Status.Directory");
                            }
                        }
                        catch { }
                        var item = new ListViewItem(new string[] { name, sw.InstallLocation, sw.SizeText, statusText });
                        item.Tag = sw;
                        listViewSoftware.Items.Add(item);
                    }
                    softwareSortColumn = 0;
                    softwareSortAsc = true;
                    listViewSoftware.ListViewItemSorter = new ListViewItemComparerSoftware(softwareSortColumn, softwareSortAsc);
                    listViewSoftware.Sort();
                    UpdateColumnHeaderSortIndicator(listViewSoftware, softwareSortColumn, softwareSortAsc);
                }));
            }
            catch (Exception ex)
            {
                this.Invoke(new Action(() => {
                    listViewSoftware.Items.Clear();
                    LocalizedMessageBox.Show(string.Format(Localization.T("Msg.ScanSoftwareFailedFmt"), ex.Message), Localization.T("Title.Tip"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                                string statusText = string.Empty;
                                try
                                {
                                    if (Directory.Exists(appData.Path))
                                    {
                                        var isSymlink = (File.GetAttributes(appData.Path) & FileAttributes.ReparsePoint) != 0;
                                        statusText = isSymlink ? Localization.T("Status.Symlink") : Localization.T("Status.Directory");
                                    }
                                    else
                                    {
                                        statusText = Localization.T("Status.Directory");
                                    }
                                }
                                catch { }
                                var item = new ListViewItem(new string[]
                                {
                                    appData.Name,
                                    appData.Path,
                                    appData.SizeText,
                                    statusText
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
                    // 默认按名称升序
                    appDataSortColumn = 0;
                    appDataSortAsc = true;
                    listViewAppData.ListViewItemSorter = new ListViewItemComparer(appDataSortColumn, appDataSortAsc);
                    listViewAppData.Sort();
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
            // 不再弹出迁移成功弹窗
        }

        private void buttonRefreshSoftware_Click(object sender, EventArgs e)
        {
            listViewSoftware.Items.Clear();
            listViewSoftware.Items.Add(new ListViewItem(Localization.T("Msg.Loading")));
            Task.Run(() => LoadInstalledSoftwareSafe());
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
            columnHeaderStatus.Text = Localization.T("Column.Status");

            // AppData 列表列名（与软件迁移列名一致）
            columnHeaderAppName.Text = Localization.T("Column.SoftwareName");
            columnHeaderAppPath.Text = Localization.T("Column.InstallPath");
            columnHeaderAppSize.Text = Localization.T("Column.Size");
            columnHeaderAppStatus.Text = Localization.T("Column.Status");

            // Buttons
            buttonMigrateSoftware.Text = Localization.T("Button.MigrateSelected");
            buttonMigrateAppData.Text = Localization.T("Button.MigrateSelected");
            buttonRefreshAppData.Text = Localization.T("Button.RefreshAppData");
            buttonRefreshSoftware.Text = Localization.T("Button.RefreshAppData");
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
                    string name = sw.Name;
                    string path = sw.InstallLocation;
                    string statusText = string.Empty;
                    try
                    {
                        if (Directory.Exists(path))
                        {
                            var isSymlink = (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
                            statusText = isSymlink ? Localization.T("Status.Symlink") : Localization.T("Status.Directory");
                        }
                        else
                        {
                            statusText = Localization.T("Status.Directory");
                        }
                    }
                    catch { }
                    var item = new ListViewItem(new string[] { name, sw.InstallLocation, sw.SizeText, statusText });
                    item.Tag = sw;
                    listViewSoftware.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                LocalizedMessageBox.Show(string.Format(Localization.T("Msg.ScanSoftwareFailedFmt"), ex.Message), Localization.T("Title.Tip"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                LocalizedMessageBox.Show(Localization.T("Msg.SelectSoftware"), Localization.T("Title.Tip"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    var errorList = new List<(string name, string message)>();
                    using (var wait = new WaitForm(Localization.T("Msg.Migrating")))
                    {
                        Task.Run(() =>
                        {
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
                                catch (UnauthorizedAccessException)
                                {
                                    fail++;
                                    MigrationLogger.Log(new MigrationLogEntry
                                    {
                                        Time = DateTime.Now,
                                        SoftwareName = sw.Name,
                                        OldPath = sw.InstallLocation,
                                        NewPath = Path.Combine(targetRoot, Path.GetFileName(sw.InstallLocation)),
                                        Status = "Fail",
                                        Message = Localization.T("Msg.AccessDenied")
                                    });
                                    errorList.Add((sw.Name, Localization.T("Msg.AccessDenied")));
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
                                    errorList.Add((sw.Name, ex.Message));
                                }
                            }
                            this.Invoke(new Action(() => wait.Close()));
                        });
                        wait.ShowDialog();
                    }
                    // 统一在UI线程弹窗，避免卡顿
                    foreach (var err in errorList)
                    {
                        LocalizedMessageBox.Show(string.Format(Localization.T("Msg.MigrateFailedFmt"), err.name, err.message), Localization.T("Title.Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    LocalizedMessageBox.Show(string.Format(Localization.T("Msg.MigrateCompleted"), success, fail), Localization.T("Title.Tip"), MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                            // 初始时禁用自定义
                            checkBoxCustomX86.Checked = false;
                            textBoxProgramFilesX86.Enabled = false;
                            buttonBrowseProgramFilesX86.Enabled = false;
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
                textBoxProgramFilesX86.Text = textBoxProgramFiles.Text;
            }
        }

        private void checkBoxCustomX86_CheckedChanged(object sender, EventArgs e)
        {
            bool customEnabled = checkBoxCustomX86.Checked;
            labelProgramFilesX86.Enabled = customEnabled;
            textBoxProgramFilesX86.Enabled = customEnabled;
            buttonBrowseProgramFilesX86.Enabled = customEnabled;

            if (!customEnabled && Environment.Is64BitOperatingSystem)
            {
                // 关闭自定义时，32位路径等于64位路径，且不可编辑
                textBoxProgramFilesX86.Text = textBoxProgramFiles.Text;
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

            // 自动创建不存在的目录
            try
            {
                if (!string.IsNullOrEmpty(path64) && !Directory.Exists(path64))
                {
                    Directory.CreateDirectory(path64);
                }
                if (Environment.Is64BitOperatingSystem && !string.IsNullOrEmpty(pathX86) && !Directory.Exists(pathX86))
                {
                    Directory.CreateDirectory(pathX86);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Localization.T("Msg.SettingsFailed"), ex.Message),
                    Localization.T("Title.Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

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
                            string statusText = string.Empty;
                            try
                            {
                                if (Directory.Exists(appData.Path))
                                {
                                    var isSymlink = (File.GetAttributes(appData.Path) & FileAttributes.ReparsePoint) != 0;
                                    statusText = isSymlink ? Localization.T("Status.Symlink") : Localization.T("Status.Directory");
                                }
                                else
                                {
                                    statusText = Localization.T("Status.Directory");
                                }
                            }
                            catch { }

                            var item = new ListViewItem(new string[]
                            {
                                appData.Name,
                                appData.Path,
                                appData.SizeText,
                                statusText
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

        // 排序器类
        class ListViewItemComparer : System.Collections.IComparer
        {
            private int col;
            private bool ascending;
            public ListViewItemComparer(int column, bool asc)
            {
                col = column;
                ascending = asc;
            }
            public int Compare(object x, object y)
            {
                var itemX = x as ListViewItem;
                var itemY = y as ListViewItem;
                // 按大小列时用数字比较
                if (col == 2) // Size列
                {
                    long sx = 0, sy = 0;
                    if (itemX.Tag is AppDataInfo infoX) sx = infoX.Size;
                    if (itemY.Tag is AppDataInfo infoY) sy = infoY.Size;
                    int cmp = sx.CompareTo(sy);
                    return ascending ? cmp : -cmp;
                }
                else
                {
                    int cmp = string.Compare(itemX.SubItems[col].Text, itemY.SubItems[col].Text, StringComparison.CurrentCultureIgnoreCase);
                    return ascending ? cmp : -cmp;
                }
            }
        }

        class ListViewItemComparerSoftware : System.Collections.IComparer
        {
            private int col;
            private bool ascending;
            public ListViewItemComparerSoftware(int column, bool asc)
            {
                col = column;
                ascending = asc;
            }
            public int Compare(object x, object y)
            {
                var itemX = x as ListViewItem;
                var itemY = y as ListViewItem;
                // 按大小列时用数字比较
                if (col == 2) // Size列
                {
                    long sx = 0, sy = 0;
                    if (itemX.Tag is InstalledSoftware infoX) sx = infoX.SizeBytes;
                    if (itemY.Tag is InstalledSoftware infoY) sy = infoY.SizeBytes;
                    int cmp = sx.CompareTo(sy);
                    return ascending ? cmp : -cmp;
                }
                else
                {
                    int cmp = string.Compare(itemX.SubItems[col].Text, itemY.SubItems[col].Text, StringComparison.CurrentCultureIgnoreCase);
                    return ascending ? cmp : -cmp;
                }
            }
        }

        private int appDataSortColumn = 0;
        private bool appDataSortAsc = true;
        private int softwareSortColumn = 0;
        private bool softwareSortAsc = true;

        private void listViewAppData_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == appDataSortColumn)
                appDataSortAsc = !appDataSortAsc;
            else
            {
                appDataSortColumn = e.Column;
                appDataSortAsc = true;
            }
            listViewAppData.ListViewItemSorter = new ListViewItemComparer(appDataSortColumn, appDataSortAsc);
            listViewAppData.Sort();
            UpdateColumnHeaderSortIndicator(listViewAppData, appDataSortColumn, appDataSortAsc);
        }

        private void listViewSoftware_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (e.Column == softwareSortColumn)
                softwareSortAsc = !softwareSortAsc;
            else
            {
                softwareSortColumn = e.Column;
                softwareSortAsc = true;
            }
            listViewSoftware.ListViewItemSorter = new ListViewItemComparerSoftware(softwareSortColumn, softwareSortAsc);
            listViewSoftware.Sort();
            UpdateColumnHeaderSortIndicator(listViewSoftware, softwareSortColumn, softwareSortAsc);
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

        private void UpdateColumnHeaderSortIndicator(ListView listView, int sortColumn, bool ascending)
        {
            // 清除所有列名的三角符号
            for (int i = 0; i < listView.Columns.Count; i++)
            {
                var col = listView.Columns[i];
                string text = col.Text;
                text = text.Replace(" ▲", "").Replace(" ▼", "");
                col.Text = text;
            }
            // 给当前排序列加三角
            if (sortColumn >= 0 && sortColumn < listView.Columns.Count)
            {
                var col = listView.Columns[sortColumn];
                col.Text = col.Text + (ascending ? " ▲" : " ▼");
            }
        }
    }
}
