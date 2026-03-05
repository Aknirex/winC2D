using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using winC2D.Core;

namespace winC2D.UI
{
    // ══════════════════════════════════════════════════════════════════════════
    // ThemedListView — 完全自绘 ListView，彻底解决亮/暗色主题适配问题
    // 特性：
    //   - 完全 OwnerDraw（列头 + 行 + 子项全自绘）
    //   - 奇偶行区分、Hover 高亮、多选高亮
    //   - 列头点击排序指示器
    //   - 状态标签（带圆角色块）
    //   - 零闪烁双缓冲
    // ══════════════════════════════════════════════════════════════════════════
    public class ThemedListView : ListView
    {
        // ── 公共事件 ──────────────────────────────────────────────────────────
        public event EventHandler<ThemedListViewSortEventArgs> ColumnSortRequested;

        // ── 内部状态 ──────────────────────────────────────────────────────────
        private int _hoverIndex = -1;
        private int _sortColumn = -1;
        private bool _sortAscending = true;

        // ── 构造 ──────────────────────────────────────────────────────────────
        public ThemedListView()
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint  |
                ControlStyles.UserPaint,
                true);
            UpdateStyles();

            View          = View.Details;
            FullRowSelect = true;
            GridLines     = false;
            OwnerDraw     = true;
            BorderStyle   = BorderStyle.None;

            DrawColumnHeader += OnDrawColumnHeader;
            DrawItem         += OnDrawItem;
            DrawSubItem      += OnDrawSubItem;
            ColumnClick      += OnColumnClick;
            MouseMove        += OnMouseMove;
            MouseLeave       += OnMouseLeave;
        }

        // ── 排序状态 ──────────────────────────────────────────────────────────
        public int SortColumn
        {
            get => _sortColumn;
            set { _sortColumn = value; Invalidate(); }
        }
        public bool SortAscending
        {
            get => _sortAscending;
            set { _sortAscending = value; Invalidate(); }
        }

        // ── 主题刷新 ──────────────────────────────────────────────────────────
        public void RefreshTheme()
        {
            var p = ThemeManager.Current;
            BackColor = p.ListBackground;
            ForeColor = p.ListForeground;
            Invalidate();
        }

        // ══════════════════════════════════════════════════════════════════════
        // 绘制列头
        // ══════════════════════════════════════════════════════════════════════
        private void OnDrawColumnHeader(object sender, DrawListViewColumnHeaderEventArgs e)
        {
            var p  = ThemeManager.Current;
            var g  = e.Graphics;
            var rc = e.Bounds;

            g.FillRectangle(new SolidBrush(p.ListHeaderBg), rc);

            // 分隔线（右侧）
            if (e.ColumnIndex < Columns.Count - 1)
                g.DrawLine(new Pen(p.ListGridLine), rc.Right - 1, rc.Top + 4, rc.Right - 1, rc.Bottom - 4);

            // 底部分隔线
            g.DrawLine(new Pen(p.Separator), rc.Left, rc.Bottom - 1, rc.Right, rc.Bottom - 1);

            // 列头文字
            var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            flags |= e.Header.TextAlign switch
            {
                HorizontalAlignment.Center => TextFormatFlags.HorizontalCenter,
                HorizontalAlignment.Right  => TextFormatFlags.Right,
                _                          => TextFormatFlags.Left
            };
            var textRc = new Rectangle(rc.Left + 8, rc.Top, rc.Width - 24, rc.Height);
            TextRenderer.DrawText(g, e.Header.Text, Font, textRc, p.ListHeaderFg, flags);

            // 排序指示器
            if (e.ColumnIndex == _sortColumn)
            {
                string arrow = _sortAscending ? "▲" : "▼";
                var arrowRc  = new Rectangle(rc.Right - 20, rc.Top, 18, rc.Height);
                TextRenderer.DrawText(g, arrow, Font, arrowRc,
                    p.Accent, TextFormatFlags.VerticalCenter | TextFormatFlags.Right);
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // 绘制行（背景）
        // ══════════════════════════════════════════════════════════════════════
        private void OnDrawItem(object sender, DrawListViewItemEventArgs e)
        {
            // 子项绘制时才真正填充内容，此处只设背景
            var p = ThemeManager.Current;
            var bg = GetRowBackground(p, e.ItemIndex, e.Item.Selected);
            e.Graphics.FillRectangle(new SolidBrush(bg), e.Bounds);
        }

        // ══════════════════════════════════════════════════════════════════════
        // 绘制子项（主要绘制入口）
        // ══════════════════════════════════════════════════════════════════════
        private void OnDrawSubItem(object sender, DrawListViewSubItemEventArgs e)
        {
            var p  = ThemeManager.Current;
            var g  = e.Graphics;
            var rc = e.Bounds;
            bool sel = e.Item.Selected;

            // 背景
            var bg = GetRowBackground(p, e.ItemIndex, sel);
            g.FillRectangle(new SolidBrush(bg), rc);

            // 网格线（底部）
            g.DrawLine(new Pen(p.ListGridLine, 1), rc.Left, rc.Bottom - 1, rc.Right, rc.Bottom - 1);

            // 文字颜色
            var fg = sel ? p.ListRowSelectedFg : p.ListForeground;

            // 第 0 列：主名称（加 padding）
            var text = e.SubItem?.Text ?? string.Empty;

            // 状态列 → 画彩色标签
            if (IsStatusColumn(e.ColumnIndex) && TryGetStatusColor(text, p, out Color statusColor))
            {
                DrawStatusTag(g, rc, text, statusColor, fg);
                return;
            }

            var textRc = new Rectangle(rc.Left + (e.ColumnIndex == 0 ? 10 : 6),
                                       rc.Top, rc.Width - 12, rc.Height);
            TextRenderer.DrawText(g, text, Font, textRc, fg,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.Left);
        }

        // ── 状态标签 ──────────────────────────────────────────────────────────
        private static void DrawStatusTag(Graphics g, Rectangle rc, string text, Color tagColor, Color fallbackFg)
        {
            var tagFg  = GetTagForeground(tagColor);
            int tagW   = Math.Min(rc.Width - 16, TextRenderer.MeasureText(text, SystemFonts.DefaultFont).Width + 18);
            int tagH   = 20;
            int tagX   = rc.Left + 8;
            int tagY   = rc.Top + (rc.Height - tagH) / 2;
            var tagRc  = new Rectangle(tagX, tagY, tagW, tagH);

            using var bgBrush = new SolidBrush(Color.FromArgb(40, tagColor));
            using var fgBrush = new SolidBrush(tagColor);
            using var path    = RoundRect(tagRc, 4);
            g.SmoothingMode   = SmoothingMode.AntiAlias;
            g.FillPath(bgBrush, path);
            g.DrawPath(new Pen(Color.FromArgb(100, tagColor)), path);
            g.SmoothingMode   = SmoothingMode.Default;
            TextRenderer.DrawText(g, text, SystemFonts.DefaultFont, tagRc, tagColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
        }

        private static Color GetTagForeground(Color bg)
        {
            double luminance = (0.299 * bg.R + 0.587 * bg.G + 0.114 * bg.B) / 255;
            return luminance > 0.5 ? Color.FromArgb(30, 30, 30) : Color.White;
        }

        // ── 背景色计算 ────────────────────────────────────────────────────────
        private Color GetRowBackground(ThemePalette p, int index, bool selected)
        {
            if (selected)       return p.ListRowSelected;
            if (index == _hoverIndex) return p.ListRowHover;
            return index % 2 == 0 ? p.ListRowOdd : p.ListRowEven;
        }

        // ── 鼠标 Hover ────────────────────────────────────────────────────────
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var hit = HitTest(e.Location);
            int newHover = hit.Item?.Index ?? -1;
            if (newHover != _hoverIndex)
            {
                _hoverIndex = newHover;
                Invalidate();
            }
        }

        private void OnMouseLeave(object sender, EventArgs e)
        {
            if (_hoverIndex != -1) { _hoverIndex = -1; Invalidate(); }
        }

        // ── 列头点击 ──────────────────────────────────────────────────────────
        private void OnColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (_sortColumn == e.Column)
                _sortAscending = !_sortAscending;
            else
            {
                _sortColumn    = e.Column;
                _sortAscending = true;
            }
            Invalidate();
            ColumnSortRequested?.Invoke(this, new ThemedListViewSortEventArgs(e.Column, _sortAscending));
        }

        // ── 辅助 ──────────────────────────────────────────────────────────────
        private bool IsStatusColumn(int col)
        {
            // 约定：最后一列为状态列
            return col == Columns.Count - 1;
        }

        private static readonly Dictionary<string, Func<ThemePalette, Color>> _statusMap = new(StringComparer.OrdinalIgnoreCase);

        private bool TryGetStatusColor(string text, ThemePalette p, out Color color)
        {
            // 通过字符串匹配本地化状态文本，回退用 Accent
            // 调用方也可直接在 Tag 中存 SoftwareStatus 枚举
            color = default;
            if (string.IsNullOrEmpty(text)) return false;

            // 尝试从 Item.Tag 推断
            // 由于此处是 SubItem，改为用关键词匹配
            if (text.Contains("Symlink") || text.Contains("符号链接") || text.Contains("ショートカット"))
            { color = p.StatusSymlink;   return true; }
            if (text.Contains("Suspicious") || text.Contains("可疑") || text.Contains("疑わしい"))
            { color = p.StatusSuspicious; return true; }
            if (text.Contains("Empty") || text.Contains("空目录") || text.Contains("空"))
            { color = p.StatusEmpty;     return true; }
            if (text.Contains("Residual") || text.Contains("残留") || text.Contains("残存"))
            { color = p.StatusResidual;  return true; }
            if (text.Contains("Directory") || text.Contains("目录") || text.Contains("ディレクトリ"))
            { color = p.StatusNormal;    return true; }

            return false;
        }

        private static GraphicsPath RoundRect(Rectangle rc, int r)
        {
            var path = new GraphicsPath();
            path.AddArc(rc.Left, rc.Top, r * 2, r * 2, 180, 90);
            path.AddArc(rc.Right - r * 2, rc.Top, r * 2, r * 2, 270, 90);
            path.AddArc(rc.Right - r * 2, rc.Bottom - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(rc.Left, rc.Bottom - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        // ══════════════════════════════════════════════════════════════════════
        // 外部排序辅助
        // ══════════════════════════════════════════════════════════════════════
        /// <summary>
        /// 对指定列执行默认字符串比较排序
        /// </summary>
        public void ApplySort(int column, bool ascending)
        {
            ListViewItemSorter = new GenericListViewSorter(column, ascending);
            Sort();
            SortColumn    = column;
            SortAscending = ascending;
        }
    }

    // ── 排序事件参数 ──────────────────────────────────────────────────────────
    public class ThemedListViewSortEventArgs : EventArgs
    {
        public int Column { get; }
        public bool Ascending { get; }
        public ThemedListViewSortEventArgs(int col, bool asc) { Column = col; Ascending = asc; }
    }

    // ── 通用字符串排序器 ──────────────────────────────────────────────────────
    public class GenericListViewSorter : System.Collections.IComparer
    {
        private readonly int _col;
        private readonly int _dir;
        public GenericListViewSorter(int col, bool asc) { _col = col; _dir = asc ? 1 : -1; }

        public int Compare(object x, object y)
        {
            var ix = (ListViewItem)x;
            var iy = (ListViewItem)y;
            string tx = _col < ix.SubItems.Count ? ix.SubItems[_col].Text : "";
            string ty = _col < iy.SubItems.Count ? iy.SubItems[_col].Text : "";

            // 尝试数字比较（用于大小列）
            if (long.TryParse(StripUnit(tx), out long lx) && long.TryParse(StripUnit(ty), out long ly))
                return _dir * lx.CompareTo(ly);

            return _dir * string.Compare(tx, ty, StringComparison.CurrentCultureIgnoreCase);
        }

        private static string StripUnit(string s)
        {
            return s.Split(' ')[0].Replace(",", "");
        }
    }
}
