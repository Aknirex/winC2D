using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace winC2D
{
    public partial class LogForm : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private int hoverIndex = -1;

        public LogForm()
        {
            InitializeComponent();
            this.Load += LogForm_Load;
            this.FormClosing += (s, e) => ThemeManager.ThemeChanged -= ThemeManager_ThemeChanged;
            
            // Apply font
            this.Font = new Font("Segoe UI Variable", 9F, FontStyle.Regular, GraphicsUnit.Point);
            
            ApplyLocalization();
            ApplyTheme(ThemeManager.CurrentTheme);
            ThemeManager.ThemeChanged += ThemeManager_ThemeChanged;

            // 设置 listViewLog 的 Anchor 属性，使其随窗口大小变化
            listViewLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listViewLog.MouseMove += (s, e) =>
            {
                var hit = listViewLog.HitTest(e.Location);
                UpdateListViewHover(hit.Item?.Index ?? -1);
            };
            listViewLog.MouseLeave += (s, e) => UpdateListViewHover(-1);
            listViewLog.SizeChanged += (s, e) => UpdateLastColumnWidth();

            // 设置 buttonRollback 的 Anchor 属性，使其保持在窗口左下角
            buttonRollback.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            UpdateLastColumnWidth();
        }

        private void UpdateLastColumnWidth()
        {
            if (listViewLog.Columns.Count == 0)
            {
                return;
            }

            var lastColumn = listViewLog.Columns[listViewLog.Columns.Count - 1];
            var otherWidth = listViewLog.Columns.Cast<ColumnHeader>().Where(c => c != lastColumn).Sum(c => c.Width);
            var newWidth = Math.Max(50, listViewLog.ClientSize.Width - otherWidth);
            if (newWidth != lastColumn.Width)
            {
                lastColumn.Width = newWidth;
            }
        }

        private void ThemeManager_ThemeChanged(object sender, EventArgs e)
        {
            ApplyTheme(ThemeManager.CurrentTheme);
        }

        private void ApplyTheme(AppTheme theme)
        {
            var palette = ThemeManager.GetPalette(theme);
            
            // Title Bar
            if (Environment.OSVersion.Version.Major >= 10)
            {
                int useImmersiveDarkMode = (theme == AppTheme.Dark) ? 1 : 0;
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));
            }

            this.BackColor = palette.FormBackground;
            this.ForeColor = palette.Foreground;

            listViewLog.BackColor = palette.ListViewBackground;
            listViewLog.ForeColor = palette.ListViewForeground;
            listViewLog.BorderStyle = BorderStyle.None;
            listViewLog.OwnerDraw = true;
            listViewLog.DrawColumnHeader += ListViewLog_DrawColumnHeader;
            listViewLog.DrawItem += ListViewLog_DrawItem;
            listViewLog.DrawSubItem += ListViewLog_DrawSubItem;
            // Enable Explorer theme for dark mode scrollbars
            var themeName = ThemeManager.CurrentTheme == AppTheme.Dark ? "DarkMode_Explorer" : "Explorer";
            SetWindowTheme(listViewLog.Handle, themeName, null);

            buttonRollback.FlatStyle = FlatStyle.Flat;
            buttonRollback.BackColor = palette.ControlBackground;
            buttonRollback.ForeColor = palette.Foreground;
            buttonRollback.FlatAppearance.BorderColor = palette.ButtonBorder;
            buttonRollback.FlatAppearance.BorderSize = 1;
        }

        private void ListViewLog_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            if (ThemeManager.CurrentTheme != AppTheme.Dark)
            {
                e.DrawDefault = true;
                return;
            }

            var listView = (ListView)sender;
            var palette = ThemeManager.GetPalette(ThemeManager.CurrentTheme);
            var headerBackground = palette.ListViewHeaderBackground.A == 0 ? palette.ControlBackground : palette.ListViewHeaderBackground;
            using (var brush = new SolidBrush(headerBackground))
            {
                e.Graphics.FillRectangle(brush, e.Bounds);
            }

            using (var pen = new Pen(palette.ButtonBorder))
            {
                e.Graphics.DrawLine(pen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
            }

            if (e.ColumnIndex == listView.Columns.Count - 1)
            {
                var remainingWidth = listView.ClientSize.Width - e.Bounds.Right;
                if (remainingWidth > 0)
                {
                    var extraBounds = new Rectangle(e.Bounds.Right, e.Bounds.Top, remainingWidth, e.Bounds.Height);
                    using (var brush = new SolidBrush(headerBackground))
                    {
                        e.Graphics.FillRectangle(brush, extraBounds);
                    }

                    using (var pen = new Pen(palette.ButtonBorder))
                    {
                        e.Graphics.DrawLine(pen, extraBounds.Left, extraBounds.Bottom - 1, extraBounds.Right, extraBounds.Bottom - 1);
                    }
                }
            }

            TextRenderer.DrawText(e.Graphics, e.Header.Text, this.Font, e.Bounds, palette.ListViewHeaderForeground,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void ListViewLog_DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (ThemeManager.CurrentTheme != AppTheme.Dark)
            {
                e.DrawDefault = true;
                return;
            }

            var listView = (ListView)sender;
            var palette = ThemeManager.GetPalette(ThemeManager.CurrentTheme);
            var bounds = GetListViewRowBounds(listView, e.Bounds);
            var backColor = GetListViewRowBackColor(e.ItemIndex, e.Item.Selected, palette);
            using (var brush = new SolidBrush(backColor))
            {
                e.Graphics.FillRectangle(brush, bounds);
            }

            using (var pen = new Pen(palette.ListViewGridColor))
            {
                e.Graphics.DrawLine(pen, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
            }
        }

        private void ListViewLog_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (ThemeManager.CurrentTheme != AppTheme.Dark)
            {
                e.DrawDefault = true;
                return;
            }

            var palette = ThemeManager.GetPalette(ThemeManager.CurrentTheme);
            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, this.Font, e.Bounds, palette.ListViewForeground,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

            using (var pen = new Pen(palette.ListViewGridColor))
            {
                e.Graphics.DrawLine(pen, e.Bounds.Right - 1, e.Bounds.Top, e.Bounds.Right - 1, e.Bounds.Bottom);
            }
        }

        private Rectangle GetListViewRowBounds(ListView listView, Rectangle itemBounds)
        {
            return new Rectangle(0, itemBounds.Top, listView.ClientSize.Width, itemBounds.Height);
        }

        private void UpdateListViewHover(int newIndex)
        {
            if (hoverIndex == newIndex) return;
            int currentIndex = hoverIndex;
            hoverIndex = newIndex;

            if (currentIndex >= 0 && currentIndex < listViewLog.Items.Count)
            {
                var bounds = GetListViewRowBounds(listViewLog, listViewLog.Items[currentIndex].Bounds);
                listViewLog.Invalidate(bounds);
            }

            if (hoverIndex >= 0 && hoverIndex < listViewLog.Items.Count)
            {
                var bounds = GetListViewRowBounds(listViewLog, listViewLog.Items[hoverIndex].Bounds);
                listViewLog.Invalidate(bounds);
            }
        }

        private Color GetListViewRowBackColor(int itemIndex, bool selected, ThemePalette palette)
        {
            if (selected)
            {
                return palette.ListViewRowSelectedBackground;
            }

            if (hoverIndex == itemIndex)
            {
                return palette.ListViewRowHoverBackground;
            }

            return (itemIndex % 2 == 0) ? palette.ListViewRowBackground : palette.ListViewRowAlternateBackground;
        }

        private void ApplyLocalization()
        {
            // 窗口标题
            this.Text = Localization.T("Log.Title");

            // 列标题
            columnTime.Text = Localization.T("Log.Time");
            columnName.Text = Localization.T("Log.SoftwareName");
            columnOldPath.Text = Localization.T("Log.OldPath");
            columnNewPath.Text = Localization.T("Log.NewPath");
            columnStatus.Text = Localization.T("Log.Status");
            columnMsg.Text = Localization.T("Log.Message");

            // 按钮
            buttonRollback.Text = Localization.T("Button.Rollback");
        }

        private void LogForm_Load(object sender, EventArgs e)
        {
            listViewLog.Items.Clear();
            var logs = MigrationLogger.ReadAll();
            foreach (var entry in logs)
            {
                var item = new ListViewItem(new string[]
                {
                    entry.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                    entry.SoftwareName,
                    entry.OldPath,
                    entry.NewPath,
                    entry.Status,
                    entry.Message
                });
                listViewLog.Items.Add(item);
            }
        }

        private void buttonRollback_Click(object sender, EventArgs e)
        {
            if (listViewLog.SelectedItems.Count == 0)
            {
                MessageBox.Show(
                    Localization.T("Msg.SelectLogEntry"), 
                    Localization.T("Title.Tip"));
                return;
            }
            var item = listViewLog.SelectedItems[0];
            string oldPath = item.SubItems[2].Text;
            string newPath = item.SubItems[3].Text;
            string name = item.SubItems[1].Text;
            if (!Directory.Exists(newPath))
            {
                MessageBox.Show(
                    Localization.T("Msg.NewPathNotExist"), 
                    Localization.T("Title.Error"));
                return;
            }
            // 合法性检查：如果旧路径存在且为符号链接，先删除符号链接；如果存在且不是符号链接，提示错误
            if (Directory.Exists(oldPath))
            {
                if ((File.GetAttributes(oldPath) & FileAttributes.ReparsePoint) != 0)
                {
                    // 是符号链接，删除
                    Directory.Delete(oldPath);
                }
                else
                {
                    MessageBox.Show(
                        Localization.T("Msg.OldPathExists"),
                        Localization.T("Title.Error"));
                    return;
                }
            }
            try
            {
                // 跨卷回滚：复制+删除
                CopyDirectory(newPath, oldPath);
                Directory.Delete(newPath, true);
                SoftwareMigrator.UpdateRegistryInstallLocation(oldPath, oldPath); // 恢复注册表
                try { ShortcutHelper.FixShortcuts(newPath, oldPath); } catch { }
                MigrationLogger.Log(new MigrationLogEntry
                {
                    Time = DateTime.Now,
                    SoftwareName = name,
                    OldPath = newPath,
                    NewPath = oldPath,
                    Status = "Rollback",
                    Message = Localization.T("Msg.RollbackSuccess")
                });
                MessageBox.Show(Localization.T("Msg.RollbackSuccess"));
            }
            catch (Exception ex)
            {
                MigrationLogger.Log(new MigrationLogEntry
                {
                    Time = DateTime.Now,
                    SoftwareName = name,
                    OldPath = newPath,
                    NewPath = oldPath,
                    Status = "RollbackFail",
                    Message = ex.Message
                });
                MessageBox.Show(string.Format(Localization.T("Msg.RollbackFailedFmt"), ex.Message));
            }
            LogForm_Load(null, null); // 刷新日志
        }

        private void CopyDirectory(string sourceDir, string targetDir)
        {
            Directory.CreateDirectory(targetDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                string targetFile = Path.Combine(targetDir, Path.GetFileName(file));
                File.Copy(file, targetFile);
            }
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                string targetSubDir = Path.Combine(targetDir, Path.GetFileName(directory));
                CopyDirectory(directory, targetSubDir);
            }
        }
    }
}

