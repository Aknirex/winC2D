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
        private const string REG_PATH = @"SOFTWARE\\Microsoft\\Windows\\CurrentVersion";

        private List<ScanPathItem> scanPaths = new();

        private static ScanPathItem ClonePath(ScanPathItem p) => new ScanPathItem
        {
            Path = p.Path,
            Enabled = p.Enabled,
            IsDefault = p.IsDefault
        };

        private readonly ToolTip listToolTip = new ToolTip();
        private ContextMenuStrip softwareContextMenu;

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
            //listViewSoftware.ItemActivate += listViewSoftware_ItemActivate; // 双击检查改为右键菜单
            listViewSoftware.MouseMove += listViewSoftware_MouseMove;
            listViewSoftware.MouseLeave += (s, e) => listToolTip.Hide(listViewSoftware);
            InitializeSoftwareContextMenu();
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
            EnsureDefaultScanPaths();
            // 异步加载数据
            LoadAllDataAsync();

            // 绑定窗口图标点击事件
            this.MouseDown += MainForm_MouseDown;
        }

        private void EnsureDefaultScanPaths()
        {
            string Normalize(string path)
            {
                if (string.IsNullOrWhiteSpace(path)) return null;
                var p = path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (p.Length == 2 && p[1] == ':') p += "\\";
                return p;
            }

            // Normalize existing paths first
            scanPaths = scanPaths
                .Select(p => new ScanPathItem
                {
                    Path = Normalize(p.Path),
                    Enabled = p.Enabled,
                    IsDefault = p.IsDefault
                })
                .Where(p => !string.IsNullOrEmpty(p.Path))
                .GroupBy(p => p.Path, StringComparer.OrdinalIgnoreCase)
                .Select(g => g.First())
                .ToList();

            var defaults = GetDefaultScanPaths();
            // 清除旧的默认标记
            foreach (var item in scanPaths)
            {
                if (item.IsDefault && !defaults.Any(d => string.Equals(d, item.Path, StringComparison.OrdinalIgnoreCase)))
                    item.IsDefault = false;
            }
            foreach (var def in defaults)
            {
                var existing = scanPaths.FirstOrDefault(p => string.Equals(p.Path, def, StringComparison.OrdinalIgnoreCase));
                if (existing == null)
                {
                    scanPaths.Add(new ScanPathItem { Path = def, Enabled = true, IsDefault = true });
                }
                else
                {
                    existing.IsDefault = true;
                }
            }
            // 去重
            scanPaths = scanPaths.GroupBy(p => p.Path, StringComparer.OrdinalIgnoreCase)
                                 .Select(g => g.First())
                                 .ToList();
        }

        private List<string> GetDefaultScanPaths()
        {
            string Normalize(string path)
            {
                if (string.IsNullOrWhiteSpace(path)) return null;
                var p = path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (p.Length == 2 && p[1] == ':') p += "\\"; // keep drive root with backslash
                return p;
            }

            var paths = new List<string>();
            string user64 = textBoxProgramFiles.InvokeRequired ? (string)textBoxProgramFiles.Invoke(new Func<string>(() => textBoxProgramFiles.Text.Trim())) : textBoxProgramFiles.Text.Trim();
            string userX86 = textBoxProgramFilesX86.InvokeRequired ? (string)textBoxProgramFilesX86.Invoke(new Func<string>(() => textBoxProgramFilesX86.Text.Trim())) : textBoxProgramFilesX86.Text.Trim();

            var nUser64 = Normalize(user64);
            var nUserX86 = Normalize(userX86);

            if (!string.IsNullOrEmpty(nUser64)) paths.Add(nUser64);
            if (Environment.Is64BitOperatingSystem && !string.IsNullOrEmpty(nUserX86)) paths.Add(nUserX86);

            string sys64 = Normalize(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles));
            string sysX86 = Environment.Is64BitOperatingSystem ? Normalize(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)) : null;
            if (!string.IsNullOrEmpty(sys64)) paths.Add(sys64);
            if (!string.IsNullOrEmpty(sysX86)) paths.Add(sysX86);

            var default64 = Normalize(DEFAULT_PROGRAM_FILES);
            var defaultX86 = Normalize(DEFAULT_PROGRAM_FILES_X86);

            if (!paths.Contains(default64, StringComparer.OrdinalIgnoreCase)) paths.Add(default64);
            if (Environment.Is64BitOperatingSystem && !paths.Contains(defaultX86, StringComparer.OrdinalIgnoreCase)) paths.Add(defaultX86);

            // 去重
            return paths.Where(p => !string.IsNullOrEmpty(p)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
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
                EnsureDefaultScanPaths();
                var paths = scanPaths.Where(p => p.Enabled)
                                     .Select(p => p.Path)
                                     .Where(p => !string.IsNullOrWhiteSpace(p))
                                     .Distinct(StringComparer.OrdinalIgnoreCase)
                                     .ToList();

                var list = SoftwareScanner.GetInstalledSoftwareOnC(paths);
                this.Invoke(new Action(() => {
                    // Use BeginUpdate/EndUpdate to reduce UI redraws while populating
                    listViewSoftware.BeginUpdate();
                    try
                    {
                        listViewSoftware.Items.Clear();
                        foreach (var sw in list)
                        {
                            var item = CreateSoftwareListViewItem(sw);
                            listViewSoftware.Items.Add(item);
                        }
                        softwareSortColumn = 0;
                        softwareSortAsc = true;
                        listViewSoftware.ListViewItemSorter = new ListViewItemComparerSoftware(softwareSortColumn, softwareSortAsc);
                        listViewSoftware.Sort();
                        UpdateColumnHeaderSortIndicator(listViewSoftware, softwareSortColumn, softwareSortAsc);
                    }
                    finally
                    {
                        listViewSoftware.EndUpdate();
                    }
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
                    // Use BeginUpdate/EndUpdate to minimize UI redraws while populating
                    listViewAppData.BeginUpdate();
                    try
                    {
                        listViewAppData.Items.Clear();
                        foreach (var item in items)
                            listViewAppData.Items.Add(item);
                        // 默认按名称升序
                        appDataSortColumn = 0;
                        appDataSortAsc = true;
                        listViewAppData.ListViewItemSorter = new ListViewItemComparer(appDataSortColumn, appDataSortAsc);
                        listViewAppData.Sort();
                    }
                    finally
                    {
                        listViewAppData.EndUpdate();
                    }
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
            RefreshSoftwareListLocalization();
            // Refresh AppData list asynchronously to avoid blocking the UI during language switch
            Task.Run(() => LoadAppDataFoldersSafe());
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
            labelStoragePolicyNote.Text = Localization.T("Settings.ProgramFilesNote");
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
            buttonCheckSuspicious.Text = Localization.T("Button.CheckSuspicious");
            buttonManageScanPaths.Text = Localization.T("Button.ManageScanPaths");
            if (softwareContextMenu != null)
            {
                softwareContextMenu.Items[0].Text = Localization.T("Menu.CopyName");
                softwareContextMenu.Items[1].Text = Localization.T("Menu.CopyPath");
                softwareContextMenu.Items[2].Text = Localization.T("Menu.OpenInExplorer");
                softwareContextMenu.Items[3].Text = Localization.T("Menu.Check");
            }
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
                    var item = CreateSoftwareListViewItem(sw);
                    listViewSoftware.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                LocalizedMessageBox.Show(string.Format(Localization.T("Msg.ScanSoftwareFailedFmt"), ex.Message), Localization.T("Title.Tip"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void InitializeSoftwareContextMenu()
        {
            softwareContextMenu = new ContextMenuStrip();
            softwareContextMenu.Items.Add(Localization.T("Menu.CopyName"), null, (s, e) => CopySelectedSoftwareName());
            softwareContextMenu.Items.Add(Localization.T("Menu.CopyPath"), null, (s, e) => CopySelectedSoftwarePath());
            softwareContextMenu.Items.Add(Localization.T("Menu.OpenInExplorer"), null, (s, e) => OpenSelectedSoftwareInExplorer());
            softwareContextMenu.Items.Add(Localization.T("Menu.Check"), null, async (s, e) => await CheckSelectedSoftwareAsync());
            listViewSoftware.ContextMenuStrip = softwareContextMenu;
        }

        private ListViewItem CreateSoftwareListViewItem(InstalledSoftware sw)
        {
            var item = new ListViewItem(new string[]
            {
                sw.Name,
                sw.InstallLocation,
                sw.SizeText,
                GetSoftwareStatusText(sw)
            });
            item.Tag = sw;
            return item;
        }

        private void UpdateSoftwareListViewItem(ListViewItem item)
        {
            if (item?.Tag is not InstalledSoftware sw) return;
            item.SubItems[0].Text = sw.Name;
            item.SubItems[1].Text = sw.InstallLocation;
            item.SubItems[2].Text = sw.SizeText;
            item.SubItems[3].Text = GetSoftwareStatusText(sw);
        }

        private string GetSoftwareStatusText(InstalledSoftware sw)
        {
            if (sw == null) return string.Empty;
            return sw.Status switch
            {
                SoftwareStatus.Directory => Localization.T("Status.Directory"),
                SoftwareStatus.Symlink => Localization.T("Status.Symlink"),
                SoftwareStatus.Suspicious => Localization.T("Status.Suspicious"),
                SoftwareStatus.Empty => Localization.T("Status.Empty"),
                SoftwareStatus.Residual => Localization.T("Status.Residual"),
                _ => string.Empty
            };
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
                    // 刷新安装软件列表（异步）以避免阻塞 UI
                    Task.Run(() => LoadInstalledSoftwareSafe());
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
            EnsureDefaultScanPaths();
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
            EnsureDefaultScanPaths();
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
                    // Refresh AppData list asynchronously to avoid blocking UI
                    Task.Run(() => LoadAppDataFoldersSafe());
                }
            }
        }

        private async void buttonCheckSuspiciousSoftware_Click(object sender, EventArgs e)
        {
            await CheckItemsAsync(listViewSoftware.Items.Cast<ListViewItem>());
        }

        private async Task CheckSelectedSoftwareAsync()
        {
            var items = listViewSoftware.SelectedItems.Cast<ListViewItem>().ToList();
            if (items.Count == 0 && listViewSoftware.FocusedItem != null)
                items.Add(listViewSoftware.FocusedItem);
            if (items.Count == 0) return;
            await CheckItemsAsync(items);
        }

        private async Task CheckItemsAsync(IEnumerable<ListViewItem> items)
        {
            var targetItems = items.Where(i => i?.Tag is InstalledSoftware).ToList();
            if (targetItems.Count == 0) return;

            using (var wait = new WaitForm(Localization.T("Msg.CheckingSuspicious")))
            {
                Task.Run(() =>
                {
                    foreach (var item in targetItems)
                    {
                        if (item.Tag is InstalledSoftware sw)
                        {
                            SoftwareScanner.CheckSuspiciousDirectory(sw);
                        }
                    }
                    try { this.Invoke(new Action(() => wait.Close())); } catch { }
                });
                wait.ShowDialog();
            }

            listViewSoftware.BeginUpdate();
            try
            {
                foreach (var item in targetItems)
                {
                    UpdateSoftwareListViewItem(item);
                }
                listViewSoftware.Sort();
                UpdateColumnHeaderSortIndicator(listViewSoftware, softwareSortColumn, softwareSortAsc);
            }
            finally
            {
                listViewSoftware.EndUpdate();
            }
        }

        // 刷新软件列表本地化
        private void RefreshSoftwareListLocalization()
        {
            listViewSoftware.BeginUpdate();
            try
            {
                foreach (ListViewItem item in listViewSoftware.Items)
                {
                    UpdateSoftwareListViewItem(item);
                }
                listViewSoftware.Sort();
                UpdateColumnHeaderSortIndicator(listViewSoftware, softwareSortColumn, softwareSortAsc);
            }
            finally
            {
                listViewSoftware.EndUpdate();
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

        private void listViewSoftware_MouseMove(object sender, MouseEventArgs e)
        {
            var info = listViewSoftware.HitTest(e.Location);
            if (info.Item == null) { listToolTip.Hide(listViewSoftware); return; }
            var sw = info.Item.Tag as InstalledSoftware;
            if (sw == null) { listToolTip.Hide(listViewSoftware); return; }
            string tooltip = string.Empty;
            // Size column index 2
            if (info.SubItem != null && info.Item.SubItems.IndexOf(info.SubItem) == 2 && sw.SizeBytes <= 0)
            {
                tooltip = Localization.T("Tooltip.SizeUnknown");
            }
            // Status column index 3
            if (info.SubItem != null && info.Item.SubItems.IndexOf(info.SubItem) == 3 && sw.Status == SoftwareStatus.Suspicious)
            {
                tooltip = Localization.T("Tooltip.StatusSuspicious");
            }
            if (!string.IsNullOrEmpty(tooltip))
            {
                listToolTip.Show(tooltip, listViewSoftware, e.Location.X + 15, e.Location.Y + 15, 4000);
            }
            else
            {
                listToolTip.Hide(listViewSoftware);
            }
        }

        private void CopySelectedSoftwareName()
        {
            if (listViewSoftware.FocusedItem?.Tag is InstalledSoftware sw)
            {
                try { Clipboard.SetText(sw.Name ?? string.Empty); } catch { }
            }
        }

        private void CopySelectedSoftwarePath()
        {
            if (listViewSoftware.FocusedItem?.Tag is InstalledSoftware sw)
            {
                try { Clipboard.SetText(sw.InstallLocation ?? string.Empty); } catch { }
            }
        }

        private void OpenSelectedSoftwareInExplorer()
        {
            if (listViewSoftware.FocusedItem?.Tag is InstalledSoftware sw)
            {
                try
                {
                    if (!string.IsNullOrWhiteSpace(sw.InstallLocation) && Directory.Exists(sw.InstallLocation))
                    {
                        Process.Start("explorer.exe", sw.InstallLocation);
                    }
                }
                catch { }
            }
        }

        private void buttonManageScanPaths_Click(object sender, EventArgs e)
        {
            EnsureDefaultScanPaths();
            using var dlg = new ScanPathsForm(scanPaths.Select(p => ClonePath(p)).ToList());
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                scanPaths = dlg.Paths;
                buttonRefreshSoftware.PerformClick();
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
    }
}
