using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using winC2D.Core;
using winC2D.UI;

namespace winC2D.Views
{
    // ══════════════════════════════════════════════════════════════════════════
    // SoftwarePage — 软件迁移功能页
    // ══════════════════════════════════════════════════════════════════════════
    public class SoftwarePage : Panel
    {
    private readonly winC2D.Core.MigrationEngineLogger _logger;
    private readonly MigrationEngine _engine;

        // ── 扫描路径 ──────────────────────────────────────────────────────────
        private List<ScanPathItem> _scanPaths = new();

        // ── UI 控件 ───────────────────────────────────────────────────────────
    private winC2D.UI.PageHeader      _header;
    private Panel                     _toolbar;
    private winC2D.UI.ModernButton    _btnRefresh;
    private winC2D.UI.ModernButton    _btnMigrate;
    private winC2D.UI.ModernButton    _btnRollback;
    private winC2D.UI.ModernButton    _btnScanPaths;
    private winC2D.UI.ModernButton    _btnCheckSuspicious;
    private winC2D.UI.ThemedListView  _list;
    private winC2D.UI.ModernProgressBar _progress;
    private Label                     _statusLabel;
    private winC2D.UI.ModernTextBox   _targetPathBox;
    private winC2D.UI.ModernButton    _btnBrowseTarget;
    private winC2D.UI.CardPanel       _targetCard;

        // ── 状态 ──────────────────────────────────────────────────────────────
        private CancellationTokenSource _cts;
        private bool _isBusy;
        private List<InstalledSoftware> _allSoftware = new();
        private List<MigrationTask>     _lastResults  = new();

        public SoftwarePage(winC2D.Core.MigrationEngineLogger logger)
        {
            _logger = logger;
            _engine = new MigrationEngine(logger);
            BuildUI();
        }

        // ════════════════════════════════════════════════════════════════════
        // UI 构建
        // ════════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5, ColumnCount = 1,
                Padding = Padding.Empty, Margin = Padding.Empty
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));   // 页头
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));   // 目标路径卡片
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));   // 工具栏
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // 列表
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));   // 状态栏
            Controls.Add(layout);

            // ── 页头 ────────────────────────────────────────────────────────
            _header = new winC2D.UI.PageHeader
            {
                Dock     = DockStyle.Fill,
                Title    = Localization.T("Nav.Software"),
                Subtitle = Localization.T("Desc.Software")
            };
            layout.Controls.Add(_header, 0, 0);

            // ── 目标路径卡片 ─────────────────────────────────────────────────
            _targetCard = new winC2D.UI.CardPanel { Dock = DockStyle.Fill, CornerRadius = 6, Padding = new Padding(10, 6, 10, 6) };
            var targetRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = Padding.Empty, Margin = Padding.Empty
            };
            targetRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            targetRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            targetRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

            var targetLabel = new Label
            {
                Text = Localization.T("Label.TargetDrive"),
                Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft
            };
            _targetPathBox    = new winC2D.UI.ModernTextBox { Dock = DockStyle.Fill, PlaceholderText = @"D:\Software" };
            _btnBrowseTarget  = new winC2D.UI.ModernButton  { Text = Localization.T("Button.Browse"), Dock = DockStyle.Fill };
            _btnBrowseTarget.Click += BrowseTarget_Click;

            targetRow.Controls.Add(targetLabel,     0, 0);
            targetRow.Controls.Add(_targetPathBox,  1, 0);
            targetRow.Controls.Add(_btnBrowseTarget,2, 0);
            _targetCard.Controls.Add(targetRow);
            layout.Controls.Add(_targetCard, 0, 1);

            // ── 工具栏 ──────────────────────────────────────────────────────
            _toolbar = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) };

            _btnRefresh          = MakeBtn("Button.Refresh",         winC2D.UI.ModernButton.ButtonStyle.Default);
            _btnMigrate          = MakeBtn("Button.MigrateSelected", winC2D.UI.ModernButton.ButtonStyle.Accent);
            _btnRollback         = MakeBtn("Button.Rollback",        winC2D.UI.ModernButton.ButtonStyle.Ghost);
            _btnCheckSuspicious  = MakeBtn("Button.CheckSuspicious", winC2D.UI.ModernButton.ButtonStyle.Ghost);
            _btnScanPaths        = MakeBtn("Button.ManageScanPaths", winC2D.UI.ModernButton.ButtonStyle.Ghost);

            _btnRefresh.Click         += async (s, e) => await RefreshAsync();
            _btnMigrate.Click         += async (s, e) => await MigrateSelectedAsync();
            _btnRollback.Click        += async (s, e) => await RollbackSelectedAsync();
            _btnCheckSuspicious.Click += async (s, e) => await CheckSuspiciousAsync();
            _btnScanPaths.Click       += ManageScanPaths_Click;

            _toolbar.Controls.AddRange(new Control[] {
                _btnRefresh, _btnMigrate, _btnRollback, _btnCheckSuspicious, _btnScanPaths
            });
            ArrangeToolbar(_toolbar);
            layout.Controls.Add(_toolbar, 0, 2);

            // ── 列表 ────────────────────────────────────────────────────────
            _list = new winC2D.UI.ThemedListView { Dock = DockStyle.Fill };
            _list.Columns.Add(Localization.T("Column.SoftwareName"), 200);
            _list.Columns.Add(Localization.T("Column.InstallPath"),  300);
            _list.Columns.Add(Localization.T("Column.Size"),          80);
            _list.Columns.Add(Localization.T("Column.Status"),        90);
            _list.CheckBoxes = true;
            _list.ColumnSortRequested += OnSortRequested;
            layout.Controls.Add(_list, 0, 3);

            // ── 状态栏 ──────────────────────────────────────────────────────
            var statusBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = Padding.Empty
            };
            statusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            statusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            _statusLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            _progress    = new winC2D.UI.ModernProgressBar { Dock = DockStyle.Fill, Value = 0 };
            statusBar.Controls.Add(_statusLabel, 0, 0);
            statusBar.Controls.Add(_progress,    1, 0);
            layout.Controls.Add(statusBar, 0, 4);
        }

        // ════════════════════════════════════════════════════════════════════
        // 数据加载
        // ════════════════════════════════════════════════════════════════════
        public void LoadData() => _ = RefreshAsync();

        private async Task RefreshAsync()
        {
            if (_isBusy) return;
            SetBusy(true, Localization.T("Msg.Loading"));
            _list.Items.Clear();

            try
            {
                var paths = GetActiveScanPaths();
                _allSoftware = await Task.Run(() => SoftwareScanner.GetInstalledSoftwareOnC(paths));

                _list.BeginUpdate();
                try
                {
                    foreach (var sw in _allSoftware)
                        _list.Items.Add(BuildItem(sw));
                    _list.ApplySort(0, true);
                    AutosizeColumns();
                }
                finally { _list.EndUpdate(); }

                SetStatus($"{Localization.T("Msg.Found")} {_allSoftware.Count} {Localization.T("Msg.Items")}");
            }
            catch (Exception ex)
            {
                SetStatus($"Error: {ex.Message}", isError: true);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // 迁移
        // ════════════════════════════════════════════════════════════════════
        private async Task MigrateSelectedAsync()
        {
            if (_isBusy) return;
            var checked_ = GetCheckedSoftware();
            if (checked_.Count == 0)
            {
                LocalizedMessageBox.Show(Localization.T("Msg.NothingSelected"),
                    Localization.T("Title.Tip"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string targetRoot = _targetPathBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(targetRoot) || !Directory.Exists(Path.GetPathRoot(targetRoot)))
            {
                LocalizedMessageBox.Show(Localization.T("Msg.InvalidTargetPath"),
                    Localization.T("Title.Tip"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 确认
            string confirmMsg = string.Format(Localization.T("Msg.ConfirmMigrateFmt"),
                checked_.Count, targetRoot);
            if (LocalizedMessageBox.Show(confirmMsg, Localization.T("Title.Confirm"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            SetBusy(true, Localization.T("Msg.Migrating"));
            _cts = new CancellationTokenSource();
            _progress.Value = 0;

            var tasks = checked_.Select(sw => new MigrationTask
            {
                Name          = sw.Name,
                SourcePath    = sw.InstallLocation,
                TargetPath    = BuildTargetPath(sw, targetRoot),
                CreateSymlink = true,
                UpdateRegistry = true
            }).ToList();

            var progress = new Progress<MigrationProgress>(rpt =>
            {
                if (InvokeRequired) { Invoke(new Action(() => OnProgress(rpt))); return; }
                OnProgress(rpt);
            });

            try
            {
                _lastResults = await _engine.MigrateAllAsync(tasks, progress, _cts.Token);
                int ok  = _lastResults.Count(r => r.IsSuccess);
                int err = _lastResults.Count(r => !r.IsSuccess);
                SetStatus(string.Format(Localization.T("Msg.MigrateResultFmt"), ok, err));

                if (err > 0)
                {
                    var errMsg = string.Join("\n", _lastResults.Where(r => !r.IsSuccess)
                        .Select(r => $"• {r.Name}: {r.Error}"));
                    LocalizedMessageBox.Show(errMsg, Localization.T("Title.Error"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                await RefreshAsync();
            }
            catch (OperationCanceledException)
            {
                SetStatus(Localization.T("Msg.Cancelled"));
            }
            finally
            {
                SetBusy(false);
                _progress.Value = 0;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // 回滚
        // ════════════════════════════════════════════════════════════════════
        private async Task RollbackSelectedAsync()
        {
            if (_isBusy || _lastResults.Count == 0)
            {
                LocalizedMessageBox.Show(Localization.T("Msg.NoMigrationToRollback"),
                    Localization.T("Title.Tip"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (LocalizedMessageBox.Show(Localization.T("Msg.ConfirmRollback"),
                    Localization.T("Title.Confirm"), MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            SetBusy(true, Localization.T("Msg.RollingBack"));
            var successful = _lastResults.Where(r => r.IsSuccess).ToList();
            var errors = new List<string>();

            await Task.Run(() =>
            {
                foreach (var task in successful)
                {
                    try { _engine.RollbackTask(task); }
                    catch (Exception ex) { errors.Add($"{task.Name}: {ex.Message}"); }
                }
            });

            if (errors.Count > 0)
                LocalizedMessageBox.Show(string.Join("\n", errors), Localization.T("Title.Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                LocalizedMessageBox.Show(Localization.T("Msg.RollbackSuccess"), Localization.T("Title.Tip"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

            _lastResults.Clear();
            SetBusy(false);
            await RefreshAsync();
        }

        // ════════════════════════════════════════════════════════════════════
        // 检查可疑目录
        // ════════════════════════════════════════════════════════════════════
        private async Task CheckSuspiciousAsync()
        {
            if (_isBusy) return;
            var suspicious = _allSoftware
                .Where(sw => sw.Status == SoftwareStatus.Suspicious)
                .ToList();
            if (suspicious.Count == 0) { SetStatus(Localization.T("Msg.NoSuspicious")); return; }

            SetBusy(true, Localization.T("Msg.Checking"));
            await Task.Run(() =>
            {
                foreach (var sw in suspicious)
                    SoftwareScanner.CheckSuspiciousDirectory(sw);
            });

            // 更新列表项
            foreach (ListViewItem item in _list.Items)
            {
                if (item.Tag is InstalledSoftware sw && sw.Status != SoftwareStatus.Suspicious)
                {
                    item.SubItems[2].Text = sw.SizeText;
                    item.SubItems[3].Text = StatusText(sw.Status);
                }
            }
            _list.Invalidate();
            SetBusy(false);
        }

        // ════════════════════════════════════════════════════════════════════
        // 辅助方法
        // ════════════════════════════════════════════════════════════════════
        private void OnProgress(MigrationProgress rpt)
        {
            _progress.Value  = (int)rpt.Percent;
            _statusLabel.Text = $"[{rpt.Current}/{rpt.Total}] {rpt.CurrentName} — {rpt.Stage}";
        }

        private static ListViewItem BuildItem(InstalledSoftware sw)
        {
            var item = new ListViewItem(sw.Name);
            item.SubItems.Add(sw.InstallLocation);
            item.SubItems.Add(sw.SizeText);
            item.SubItems.Add(StatusText(sw.Status));
            item.Tag = sw;
            return item;
        }

        private static string StatusText(SoftwareStatus s) => s switch
        {
            SoftwareStatus.Symlink    => Localization.T("Status.Symlink"),
            SoftwareStatus.Suspicious => Localization.T("Status.Suspicious"),
            SoftwareStatus.Empty      => Localization.T("Status.Empty"),
            SoftwareStatus.Residual   => Localization.T("Status.Residual"),
            _                         => Localization.T("Status.Directory")
        };

        private List<InstalledSoftware> GetCheckedSoftware()
        {
            var result = new List<InstalledSoftware>();
            foreach (ListViewItem item in _list.CheckedItems)
                if (item.Tag is InstalledSoftware sw) result.Add(sw);
            return result;
        }

        private IEnumerable<string> GetActiveScanPaths()
        {
            EnsureDefaultScanPaths();
            return _scanPaths.Where(p => p.Enabled).Select(p => p.Path)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private void EnsureDefaultScanPaths()
        {
            if (_scanPaths.Count > 0) return;
            var defs = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)
            };
            foreach (var d in defs.Where(d => !string.IsNullOrEmpty(d)))
                _scanPaths.Add(new ScanPathItem { Path = d, Enabled = true, IsDefault = true });
        }

        private static string BuildTargetPath(InstalledSoftware sw, string targetRoot)
        {
            string folderName = Path.GetFileName(
                sw.InstallLocation.TrimEnd(Path.DirectorySeparatorChar));
            if (string.IsNullOrEmpty(folderName)) folderName = sw.Name;
            foreach (char c in Path.GetInvalidFileNameChars())
                folderName = folderName.Replace(c, '_');
            return Path.Combine(targetRoot, folderName.TrimEnd('.'));
        }

        private void SetBusy(bool busy, string msg = "")
        {
            _isBusy = busy;
            _btnMigrate.Enabled    = !busy;
            _btnRefresh.Enabled    = !busy;
            _btnRollback.Enabled   = !busy;
            if (!string.IsNullOrEmpty(msg)) SetStatus(msg);
        }

        private void SetStatus(string msg, bool isError = false)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetStatus(msg, isError))); return; }
            _statusLabel.Text      = msg;
            _statusLabel.ForeColor = isError
                ? ThemeManager.Current.StatusError
                : ThemeManager.Current.ForegroundMuted;
        }

        private void AutosizeColumns()
        {
            if (_list.Items.Count == 0) return;
            foreach (ColumnHeader col in _list.Columns)
                col.Width = -2;
        }

        private void OnSortRequested(object sender, ThemedListViewSortEventArgs e)
        {
            _list.ApplySort(e.Column, e.Ascending);
        }

        private void BrowseTarget_Click(object sender, EventArgs e)
        {
            using var dlg = new FolderBrowserDialog { Description = Localization.T("Msg.SelectTargetDir") };
            if (dlg.ShowDialog() == DialogResult.OK)
                _targetPathBox.Text = dlg.SelectedPath;
        }

        private void ManageScanPaths_Click(object sender, EventArgs e)
        {
            using var dlg = new ScanPathsForm(_scanPaths);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _scanPaths = dlg.ResultPaths;
                _ = RefreshAsync();
            }
        }

        // ── 工具栏排列辅助 ────────────────────────────────────────────────────
        private static void ArrangeToolbar(Panel toolbar)
        {
            int x = 0;
            foreach (Control c in toolbar.Controls)
            {
                c.Left = x;
                c.Top  = (toolbar.Height - c.Height) / 2;
                x += c.Width + 6;
            }
            toolbar.Resize += (s, e) =>
            {
                int xx = 0;
                foreach (Control cc in toolbar.Controls)
                {
                    cc.Left = xx;
                    cc.Top  = (toolbar.Height - cc.Height) / 2;
                    xx += cc.Width + 6;
                }
            };
        }

    private static winC2D.UI.ModernButton MakeBtn(string locKey, winC2D.UI.ModernButton.ButtonStyle style)
        {
            return new winC2D.UI.ModernButton
            {
                Text   = Localization.T(locKey),
                Style = (winC2D.UI.ModernButton.ButtonStyle)style,
                Width  = 130,
                Height = 34
            };
        }

        // ════════════════════════════════════════════════════════════════════
        // 主题 / 本地化
        // ════════════════════════════════════════════════════════════════════
        public void ApplyTheme(ThemePalette p)
        {
            BackColor = p.Background;
            ForeColor = p.Foreground;
            _header.Invalidate();
            _targetCard.Invalidate();
            _targetPathBox.ApplyTheme();
            _list.RefreshTheme();
            _statusLabel.ForeColor = p.ForegroundMuted;
            Invalidate(true);
        }

        public void RefreshLocalization()
        {
            _header.Title    = Localization.T("Nav.Software");
            _header.Subtitle = Localization.T("Desc.Software");
            _btnRefresh.Text         = Localization.T("Button.Refresh");
            _btnMigrate.Text         = Localization.T("Button.MigrateSelected");
            _btnRollback.Text        = Localization.T("Button.Rollback");
            _btnCheckSuspicious.Text = Localization.T("Button.CheckSuspicious");
            _btnScanPaths.Text       = Localization.T("Button.ManageScanPaths");
            _list.Columns[0].Text    = Localization.T("Column.SoftwareName");
            _list.Columns[1].Text    = Localization.T("Column.InstallPath");
            _list.Columns[2].Text    = Localization.T("Column.Size");
            _list.Columns[3].Text    = Localization.T("Column.Status");
            Invalidate(true);
        }
    }
}
