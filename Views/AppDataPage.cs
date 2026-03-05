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
    // AppDataPage — AppData 目录迁移页
    // ══════════════════════════════════════════════════════════════════════════
    public class AppDataPage : Panel
    {
    private readonly winC2D.Core.MigrationEngineLogger _logger;
    private readonly MigrationEngine _engine;

        private winC2D.UI.PageHeader      _header;
        private Panel                     _toolbar;
        private winC2D.UI.ModernButton    _btnRefresh;
        private winC2D.UI.ModernButton    _btnMigrate;
        private winC2D.UI.ModernButton    _btnRollback;
        private winC2D.UI.ModernButton    _btnCheck;
        private winC2D.UI.ModernTextBox   _targetPathBox;
        private winC2D.UI.CardPanel       _targetCard;
        private winC2D.UI.ThemedListView  _list;
        private winC2D.UI.ModernProgressBar _progress;
        private Label                     _statusLabel;

        private List<AppDataInfo>     _allItems    = new();
        private List<MigrationTask>   _lastResults = new();
        private CancellationTokenSource _cts;
        private bool _isBusy;

        public AppDataPage(winC2D.Core.MigrationEngineLogger logger)
        {
            _logger = logger;
            _engine = new MigrationEngine(logger);
            BuildUI();
        }

        private void BuildUI()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, RowCount = 5, ColumnCount = 1,
                Padding = Padding.Empty, Margin = Padding.Empty
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent,  100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            Controls.Add(layout);

            // 页头
            _header = new PageHeader
            {
                Dock = DockStyle.Fill,
                Title    = Localization.T("Nav.AppData"),
                Subtitle = Localization.T("Desc.AppData")
            };
            layout.Controls.Add(_header, 0, 0);

            // 目标路径卡片
            _targetCard = new winC2D.UI.CardPanel { Dock = DockStyle.Fill, CornerRadius = 6, Padding = new Padding(10, 6, 10, 6) };
            var targetRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Padding = Padding.Empty, Margin = Padding.Empty
            };
            targetRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));
            targetRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            targetRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            var lbl = new Label { Text = Localization.T("Label.TargetDrive"), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            _targetPathBox = new winC2D.UI.ModernTextBox { Dock = DockStyle.Fill, PlaceholderText = @"D:\AppData" };
            var btnBrowse  = new winC2D.UI.ModernButton { Text = Localization.T("Button.Browse"), Dock = DockStyle.Fill };
            btnBrowse.Click += (s, e) =>
            {
                using var dlg = new FolderBrowserDialog();
                if (dlg.ShowDialog() == DialogResult.OK) _targetPathBox.Text = dlg.SelectedPath;
            };
            targetRow.Controls.Add(lbl, 0, 0);
            targetRow.Controls.Add(_targetPathBox, 1, 0);
            targetRow.Controls.Add(btnBrowse, 2, 0);
            _targetCard.Controls.Add(targetRow);
            layout.Controls.Add(_targetCard, 0, 1);

            // 工具栏
            _toolbar = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) };
            _btnRefresh = new winC2D.UI.ModernButton { Text = Localization.T("Button.Refresh"),         Style = winC2D.UI.ModernButton.ButtonStyle.Default, Width = 110, Height = 34 };
            _btnMigrate = new winC2D.UI.ModernButton { Text = Localization.T("Button.MigrateSelected"), Style = winC2D.UI.ModernButton.ButtonStyle.Accent,  Width = 130, Height = 34 };
            _btnRollback= new winC2D.UI.ModernButton { Text = Localization.T("Button.Rollback"),        Style = winC2D.UI.ModernButton.ButtonStyle.Ghost,   Width = 100, Height = 34 };
            _btnCheck   = new winC2D.UI.ModernButton { Text = Localization.T("Button.CheckSuspicious"), Style = winC2D.UI.ModernButton.ButtonStyle.Ghost,   Width = 130, Height = 34 };
            _btnRefresh.Click  += async (s, e) => await RefreshAsync();
            _btnMigrate.Click  += async (s, e) => await MigrateSelectedAsync();
            _btnRollback.Click += async (s, e) => await RollbackSelectedAsync();
            _btnCheck.Click    += async (s, e) => await CheckSelectedAsync();
            _toolbar.Controls.AddRange(new Control[] { _btnRefresh, _btnMigrate, _btnRollback, _btnCheck });
            ArrangeToolbar();
            layout.Controls.Add(_toolbar, 0, 2);

            // 列表
            _list = new winC2D.UI.ThemedListView { Dock = DockStyle.Fill };
            _list.Columns.Add(Localization.T("Column.SoftwareName"), 200);
            _list.Columns.Add(Localization.T("Column.Type"),          70);
            _list.Columns.Add(Localization.T("Column.InstallPath"),  260);
            _list.Columns.Add(Localization.T("Column.Size"),          80);
            _list.Columns.Add(Localization.T("Column.Status"),        90);
            _list.CheckBoxes = true;
            _list.ColumnSortRequested += (s, e) => _list.ApplySort(e.Column, e.Ascending);
            layout.Controls.Add(_list, 0, 3);

            // 状态栏
            var bar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Padding = Padding.Empty
            };
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            _statusLabel = new Label { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            _progress    = new winC2D.UI.ModernProgressBar { Dock = DockStyle.Fill };
            bar.Controls.Add(_statusLabel, 0, 0);
            bar.Controls.Add(_progress,    1, 0);
            layout.Controls.Add(bar, 0, 4);
        }

        public void LoadData() => _ = RefreshAsync();

        private async Task RefreshAsync()
        {
            if (_isBusy) return;
            SetBusy(true, Localization.T("Msg.Loading"));
            _list.Items.Clear();

            try
            {
                var items = await Task.Run(() => ScanAppData());
                _allItems = items;

                _list.BeginUpdate();
                try
                {
                    foreach (var info in _allItems)
                        _list.Items.Add(BuildItem(info));
                    _list.ApplySort(0, true);
                }
                finally { _list.EndUpdate(); }

                SetStatus($"{Localization.T("Msg.Found")} {_allItems.Count} {Localization.T("Msg.Items")}");
            }
            catch (Exception ex) { SetStatus($"Error: {ex.Message}", true); }
            finally { SetBusy(false); }
        }

        private static List<AppDataInfo> ScanAppData()
        {
            var result = new List<AppDataInfo>();
            void Scan(string basePath, string type)
            {
                if (!Directory.Exists(basePath)) return;
                try
                {
                    foreach (var dir in Directory.GetDirectories(basePath))
                    {
                        bool isSymlink = (File.GetAttributes(dir) & System.IO.FileAttributes.ReparsePoint) != 0;
                        result.Add(new AppDataInfo
                        {
                            Name      = Path.GetFileName(dir),
                            Path      = dir,
                            Type      = type,
                            Status    = isSymlink ? SoftwareStatus.Symlink : SoftwareStatus.Suspicious,
                            SizeBytes = 0,
                            SizeChecked = false
                        });
                    }
                }
                catch { }
            }
            Scan(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Roaming");
            Scan(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Local");
            Scan(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData\\LocalLow"), "LocalLow");
            return result;
        }

        private async Task MigrateSelectedAsync()
        {
            if (_isBusy) return;
            var checked_ = GetCheckedItems();
            if (checked_.Count == 0)
            {
                LocalizedMessageBox.Show(Localization.T("Msg.NothingSelected"), Localization.T("Title.Tip"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string targetRoot = _targetPathBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(targetRoot))
            {
                LocalizedMessageBox.Show(Localization.T("Msg.InvalidTargetPath"), Localization.T("Title.Tip"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (LocalizedMessageBox.Show(
                    string.Format(Localization.T("Msg.ConfirmMigrateFmt"), checked_.Count, targetRoot),
                    Localization.T("Title.Confirm"),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            SetBusy(true, Localization.T("Msg.Migrating"));
            _cts = new CancellationTokenSource();
            _progress.Value = 0;

            var tasks = checked_.Select(info => new MigrationTask
            {
                Name          = info.Name,
                SourcePath    = info.Path,
                TargetPath    = Path.Combine(targetRoot, "AppData", info.Type, info.Name),
                CreateSymlink = true,
                UpdateRegistry = false
            }).ToList();

            var prog = new Progress<MigrationProgress>(rpt =>
            {
                if (InvokeRequired) Invoke(new Action(() => OnProgress(rpt)));
                else OnProgress(rpt);
            });

            try
            {
                _lastResults = await _engine.MigrateAllAsync(tasks, prog, _cts.Token);
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
            catch (OperationCanceledException) { SetStatus(Localization.T("Msg.Cancelled")); }
            finally { SetBusy(false); _progress.Value = 0; }
        }

        private async Task RollbackSelectedAsync()
        {
            if (_isBusy || _lastResults.Count == 0) return;
            if (LocalizedMessageBox.Show(Localization.T("Msg.ConfirmRollback"),
                    Localization.T("Title.Confirm"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                return;

            SetBusy(true, Localization.T("Msg.RollingBack"));
            var errors = new List<string>();
            await Task.Run(() =>
            {
                foreach (var t in _lastResults.Where(r => r.IsSuccess))
                {
                    try { _engine.RollbackTask(t); }
                    catch (Exception ex) { errors.Add($"{t.Name}: {ex.Message}"); }
                }
            });

            if (errors.Count > 0)
                LocalizedMessageBox.Show(string.Join("\n", errors), Localization.T("Title.Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);

            _lastResults.Clear();
            SetBusy(false);
            await RefreshAsync();
        }

        private async Task CheckSelectedAsync()
        {
            if (_isBusy) return;
            var toCheck = _allItems.Where(i => i.Status == SoftwareStatus.Suspicious).ToList();
            if (toCheck.Count == 0) { SetStatus(Localization.T("Msg.NoSuspicious")); return; }

            SetBusy(true, Localization.T("Msg.Checking"));
            await Task.Run(() => { foreach (var info in toCheck) AppDataMigrator.CheckAppDataDirectory(info); });

            foreach (ListViewItem item in _list.Items)
            {
                if (item.Tag is AppDataInfo info)
                {
                    item.SubItems[3].Text = info.SizeText;
                    item.SubItems[4].Text = StatusText(info.Status);
                }
            }
            _list.Invalidate();
            SetBusy(false);
        }

        private static ListViewItem BuildItem(AppDataInfo info)
        {
            var item = new ListViewItem(info.Name);
            item.SubItems.Add(info.Type);
            item.SubItems.Add(info.Path);
            item.SubItems.Add(info.SizeText);
            item.SubItems.Add(StatusText(info.Status));
            item.Tag = info;
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

        private List<AppDataInfo> GetCheckedItems()
        {
            var result = new List<AppDataInfo>();
            foreach (ListViewItem item in _list.CheckedItems)
                if (item.Tag is AppDataInfo i) result.Add(i);
            return result;
        }

        private void OnProgress(MigrationProgress rpt)
        {
            _progress.Value   = (int)rpt.Percent;
            _statusLabel.Text = $"[{rpt.Current}/{rpt.Total}] {rpt.CurrentName} — {rpt.Stage}";
        }

        private void SetBusy(bool busy, string msg = "")
        {
            _isBusy = busy;
            _btnMigrate.Enabled  = !busy;
            _btnRefresh.Enabled  = !busy;
            _btnRollback.Enabled = !busy;
            if (!string.IsNullOrEmpty(msg)) SetStatus(msg);
        }

        private void SetStatus(string msg, bool isError = false)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetStatus(msg, isError))); return; }
            _statusLabel.Text      = msg;
            _statusLabel.ForeColor = isError ? ThemeManager.Current.StatusError : ThemeManager.Current.ForegroundMuted;
        }

        private void ArrangeToolbar()
        {
            int x = 0;
            foreach (Control c in _toolbar.Controls)
            {
                c.Left = x; c.Top = (_toolbar.Height - c.Height) / 2;
                x += c.Width + 6;
            }
            _toolbar.Resize += (s, e) =>
            {
                int xx = 0;
                foreach (Control cc in _toolbar.Controls)
                {
                    cc.Left = xx; cc.Top = (_toolbar.Height - cc.Height) / 2;
                    xx += cc.Width + 6;
                }
            };
        }

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
            _header.Title    = Localization.T("Nav.AppData");
            _header.Subtitle = Localization.T("Desc.AppData");
            _btnRefresh.Text  = Localization.T("Button.Refresh");
            _btnMigrate.Text  = Localization.T("Button.MigrateSelected");
            _btnRollback.Text = Localization.T("Button.Rollback");
            _btnCheck.Text    = Localization.T("Button.CheckSuspicious");
        }
    }
}
