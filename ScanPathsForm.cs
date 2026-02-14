using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms.VisualStyles;

namespace winC2D
{
    public class ScanPathsForm : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private readonly List<ScanPathItem> paths;
        private readonly ListView listView;
        private readonly Button buttonAdd;
        private readonly Button buttonRemove;
        private readonly Button buttonOk;
        private readonly Button buttonCancel;
        private readonly Label labelHint;
        private int hoverIndex = -1;

        public List<ScanPathItem> Paths => paths;

        public ScanPathsForm(List<ScanPathItem> items)
        {
            this.paths = items ?? new List<ScanPathItem>();

            Text = Localization.T("Dialog.ScanPaths.Title");
            Width = 720;
            Height = 480;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            
            this.Font = new Font("Segoe UI Variable", 9F, FontStyle.Regular, GraphicsUnit.Point);

            listView = new ThemedListView
            {
                View = View.Details,
                CheckBoxes = true,
                FullRowSelect = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Location = new System.Drawing.Point(10, 40),
                Size = new System.Drawing.Size(680, 340)
            };
            listView.OwnerDraw = true;
            listView.DrawColumnHeader += ListView_DrawColumnHeader;
            listView.DrawItem += ListView_DrawItem;
            listView.DrawSubItem += ListView_DrawSubItem;
            listView.MouseMove += (s, e) =>
            {
                var hit = listView.HitTest(e.Location);
                UpdateListViewHover(hit.Item?.Index ?? -1);
            };
            listView.MouseLeave += (s, e) => UpdateListViewHover(-1);
            listView.Columns.Add(Localization.T("Column.Path"), 480);
            listView.Columns.Add(Localization.T("Column.Removable"), 160);
            listView.SizeChanged += (s, e) => UpdateLastColumnWidth();

            labelHint = new Label
            {
                Text = Localization.T("Dialog.ScanPaths.Hint"),
                AutoSize = false,
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(680, 24)
            };

            buttonAdd = new Button
            {
                Text = Localization.T("Button.AddPath"),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Location = new System.Drawing.Point(10, 390),
                Size = new System.Drawing.Size(120, 30)
            };
            buttonAdd.Click += ButtonAdd_Click;

            buttonRemove = new Button
            {
                Text = Localization.T("Button.RemovePath"),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Location = new System.Drawing.Point(140, 390),
                Size = new System.Drawing.Size(120, 30)
            };
            buttonRemove.Click += ButtonRemove_Click;

            buttonOk = new Button
            {
                Text = Localization.T("Button.Save"),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new System.Drawing.Point(500, 390),
                Size = new System.Drawing.Size(90, 30)
            };
            buttonOk.Click += (s, e) => { ApplyListViewChanges(); DialogResult = DialogResult.OK; Close(); };

            buttonCancel = new Button
            {
                Text = Localization.T("Button.Cancel"),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                Location = new System.Drawing.Point(600, 390),
                Size = new System.Drawing.Size(90, 30)
            };
            buttonCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.Add(listView);
            Controls.Add(labelHint);
            Controls.Add(buttonAdd);
            Controls.Add(buttonRemove);
            Controls.Add(buttonOk);
            Controls.Add(buttonCancel);
            
            ApplyTheme(ThemeManager.CurrentTheme);
            ThemeManager.ThemeChanged += (s, e) => ApplyTheme(ThemeManager.CurrentTheme);
            this.FormClosing += (s, e) => ThemeManager.ThemeChanged -= (EventHandler)((s2, e2) => ApplyTheme(ThemeManager.CurrentTheme));

            Load += (s, e) => { RefreshList(); };
            UpdateLastColumnWidth();
        }

        private void UpdateLastColumnWidth()
        {
            if (listView.Columns.Count == 0)
            {
                return;
            }

            var lastColumn = listView.Columns[listView.Columns.Count - 1];
            var otherWidth = listView.Columns.Cast<ColumnHeader>().Where(c => c != lastColumn).Sum(c => c.Width);
            var newWidth = Math.Max(50, listView.ClientSize.Width - otherWidth);
            if (newWidth != lastColumn.Width)
            {
                lastColumn.Width = newWidth;
            }
        }

        private void RefreshList()
        {
            listView.BeginUpdate();
            try
            {
                listView.Items.Clear();
                foreach (var p in paths)
                {
                    var item = new ListViewItem(new[]
                    {
                        p.Path,
                        p.IsDefault ? Localization.T("Msg.No") : Localization.T("Msg.Yes")
                    })
                    {
                        Checked = p.Enabled,
                        Tag = p
                    };
                    listView.Items.Add(item);
                }
            }
            finally
            {
                listView.EndUpdate();
            }
        }

        private void ApplyListViewChanges()
        {
            foreach (ListViewItem item in listView.Items)
            {
                if (item.Tag is ScanPathItem p)
                {
                    p.Enabled = item.Checked;
                }
            }
        }

        private void ButtonAdd_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
            fbd.Description = Localization.T("Msg.SelectFolder");
            fbd.ShowNewFolderButton = true;
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                var path = fbd.SelectedPath.Trim();
                if (paths.Any(x => string.Equals(x.Path, path, StringComparison.OrdinalIgnoreCase)))
                    return;
                paths.Add(new ScanPathItem { Path = path, Enabled = true, IsDefault = false });
                RefreshList();
            }
        }

        private void ButtonRemove_Click(object sender, EventArgs e)
        {
            var selected = listView.SelectedItems.Cast<ListViewItem>().ToList();
            if (selected.Count == 0) return;
            foreach (var item in selected)
            {
                if (item.Tag is ScanPathItem p)
                {
                    if (p.IsDefault)
                    {
                        MessageBox.Show(Localization.T("Msg.CannotDeleteDefault"), Localization.T("Title.Tip"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        continue;
                    }
                    paths.Remove(p);
                }
            }
            RefreshList();
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
            
            labelHint.ForeColor = palette.Foreground;
            
            listView.BackColor = palette.ListViewBackground;
            listView.ForeColor = palette.ListViewForeground;
            listView.BorderStyle = BorderStyle.None;
            var themeName = ThemeManager.CurrentTheme == AppTheme.Dark ? "DarkMode_Explorer" : "Explorer";
            SetWindowTheme(listView.Handle, themeName, null);
            
            StyleButton(buttonAdd, palette);
            StyleButton(buttonRemove, palette);
            StyleButton(buttonOk, palette);
            StyleButton(buttonCancel, palette);
        }

        private void StyleButton(Button btn, ThemePalette palette)
        {
            btn.FlatStyle = FlatStyle.Flat;
            btn.BackColor = palette.ControlBackground;
            btn.ForeColor = palette.Foreground;
            btn.FlatAppearance.BorderColor = palette.ButtonBorder;
            btn.FlatAppearance.BorderSize = 1;
        }

        private void ListView_DrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
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

        private void ListView_DrawItem(object sender, DrawListViewItemEventArgs e)
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

        private void ListView_DrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            if (ThemeManager.CurrentTheme != AppTheme.Dark)
            {
                e.DrawDefault = true;
                return;
            }

            var listView = (ListView)sender;
            var palette = ThemeManager.GetPalette(ThemeManager.CurrentTheme);
            var textBounds = e.Bounds;

            if (e.ColumnIndex == 0 && listView.CheckBoxes)
            {
                var checkState = e.Item.Checked ? CheckBoxState.CheckedNormal : CheckBoxState.UncheckedNormal;
                var checkSize = CheckBoxRenderer.GetGlyphSize(e.Graphics, checkState);
                var checkLocation = new Point(textBounds.Left + 4, textBounds.Top + (textBounds.Height - checkSize.Height) / 2);
                CheckBoxRenderer.DrawCheckBox(e.Graphics, checkLocation, checkState);
                textBounds.X += checkSize.Width + 8;
                textBounds.Width -= checkSize.Width + 8;
            }

            TextRenderer.DrawText(e.Graphics, e.SubItem.Text, this.Font, textBounds, palette.ListViewForeground,
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

            if (currentIndex >= 0 && currentIndex < listView.Items.Count)
            {
                var bounds = GetListViewRowBounds(listView, listView.Items[currentIndex].Bounds);
                listView.Invalidate(bounds);
            }

            if (hoverIndex >= 0 && hoverIndex < listView.Items.Count)
            {
                var bounds = GetListViewRowBounds(listView, listView.Items[hoverIndex].Bounds);
                listView.Invalidate(bounds);
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
    }

    public class ScanPathItem
    {
        public string Path { get; set; }
        public bool Enabled { get; set; }
        public bool IsDefault { get; set; }

        public ScanPathItem Clone() => new ScanPathItem
        {
            Path = this.Path,
            Enabled = this.Enabled,
            IsDefault = this.IsDefault
        };
    }
}
