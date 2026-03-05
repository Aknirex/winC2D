using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using winC2D.Core;

namespace winC2D.UI
{
    // ══════════════════════════════════════════════════════════════════════════
    // ModernButton — Rounded Win11-style button with full custom paint
    // ══════════════════════════════════════════════════════════════════════════
    public partial class ModernButton : Button
    {
        public enum ButtonStyle { Default, Accent, Ghost, Danger }

        private ButtonStyle _style = ButtonStyle.Default;
        private bool _hovered;
        private bool _pressed;
        private int _cornerRadius = 6;

        public ButtonStyle Style
        {
            get => _style;
            set { _style = value; Invalidate(); }
        }
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        public ModernButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.Opaque, true);
            // Disable all default Button rendering
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize  = 0;
            UseCompatibleTextRendering = false;
            Font   = new Font("Segoe UI Variable", 9.5f, FontStyle.Regular);
            Cursor = Cursors.Hand;
            Height = 34;
            Padding = new Padding(14, 0, 14, 0);
        }

        protected override void OnMouseEnter(EventArgs e) { _hovered = true;  Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _hovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e)   { _pressed = false; Invalidate(); base.OnMouseUp(e); }
        // Redraw on focus change to prevent system focus rectangle from being painted on top
        protected override void OnGotFocus(EventArgs e)  { Invalidate(); base.OnGotFocus(e); }
        protected override void OnLostFocus(EventArgs e) { Invalidate(); base.OnLostFocus(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Fully owner-drawn — do NOT call base.OnPaint
            var g  = e.Graphics;
            var p  = ThemeManager.Current;
            var rc = ClientRectangle;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Clear with parent background to erase any leftover pixels
            g.Clear(Parent?.BackColor ?? p.Background);

            (Color bg, Color fg, Color border) = GetColors(p);

            // Inset by 0.5px so the antialiased stroke sits fully inside the control bounds
            // (GDI+ draws pen centred on the path; without inset the right/bottom edges get clipped)
            var rcF = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using var path = RoundRectF(rcF, _cornerRadius);

            using (var brush = new SolidBrush(bg))
                g.FillPath(brush, path);

            // Border — skip for transparent (Accent style)
            if (border != Color.Transparent && border.A > 0)
            {
                using var pen = new Pen(border, 1f);
                g.DrawPath(pen, path);
            }

            // Top highlight line — simulates Win11 Fluent light-edge refraction
            // Light mode: 50% white top stroke.  Dark mode: ~15% white top stroke.
            bool isDark = ThemeManager.CurrentTheme == Core.AppTheme.Dark;
            if (_style != ButtonStyle.Accent && _style != ButtonStyle.Danger && Enabled)
            {
                int hiA = isDark ? 38 : 128;  // dark=15%, light=50%
                using var hiPen = new Pen(Color.FromArgb(hiA, 255, 255, 255), 1f);
                float cx = rcF.Left + _cornerRadius;
                float ex = rcF.Right - _cornerRadius;
                if (ex > cx)
                    g.DrawLine(hiPen, cx, rcF.Top, ex, rcF.Top);
            }

            if (!Enabled) fg = p.ForegroundDisabled;

            g.SetClip(path);
            TextRenderer.DrawText(g, Text, Font, rc, fg,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.SingleLine | TextFormatFlags.PreserveGraphicsClipping);
            g.ResetClip();
        }

        // Suppress system background painting — fully owner-drawn
        protected override void OnPaintBackground(PaintEventArgs e) { }

        // RectangleF overload for sub-pixel-precise rounded rect
        private static GraphicsPath RoundRectF(RectangleF rc, int r)
        {
            var path = new GraphicsPath();
            if (r <= 0) { path.AddRectangle(rc); return path; }
            float d = r * 2f;
            path.AddArc(rc.Left,           rc.Top,            d, d, 180, 90);
            path.AddArc(rc.Right - d,      rc.Top,            d, d, 270, 90);
            path.AddArc(rc.Right - d,      rc.Bottom - d,     d, d,   0, 90);
            path.AddArc(rc.Left,           rc.Bottom - d,     d, d,  90, 90);
            path.CloseFigure();
            return path;
        }

        private (Color bg, Color fg, Color border) GetColors(ThemePalette p)
        {
            bool dis = !Enabled;
            return _style switch
            {
                ButtonStyle.Accent => (
                    dis ? p.ForegroundDisabled :
                    _pressed ? p.AccentPressed :
                    _hovered  ? p.AccentHover  : p.Accent,
                    p.AccentForeground,
                    Color.Transparent),

                ButtonStyle.Ghost => (
                    _pressed ? p.ButtonPressed :
                    _hovered  ? p.ButtonHover  : Color.Transparent,
                    p.Foreground, p.Separator),

                ButtonStyle.Danger => (
                    _pressed ? Color.FromArgb(180, 30, 30) :
                    _hovered  ? Color.FromArgb(200, 50, 50) : Color.FromArgb(196, 43, 28),
                    Color.White, Color.Transparent),

                _ => (
                    _pressed ? p.ButtonPressed :
                    _hovered  ? p.ButtonHover  : p.ButtonBackground,
                    p.ButtonForeground, p.ButtonBorder)
            };
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ModernTextBox — Rounded text box with focus border
    // ══════════════════════════════════════════════════════════════════════════
    public partial class ModernTextBox : UserControl
    {
        private readonly TextBox _inner;
        private bool _focused;

        public new string Text { get => _inner.Text; set => _inner.Text = value; }
        public new event EventHandler TextChanged { add => _inner.TextChanged += value; remove => _inner.TextChanged -= value; }
        public new event KeyEventHandler KeyDown  { add => _inner.KeyDown    += value; remove => _inner.KeyDown    -= value; }
        public bool ReadOnly { get => _inner.ReadOnly; set => _inner.ReadOnly = value; }
        public string PlaceholderText { get => _inner.PlaceholderText; set => _inner.PlaceholderText = value; }

        public ModernTextBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            // ResizeRedraw ensures the control fully repaints when the parent resizes,
            // preventing stale pixels (drag artifacts) from remaining on screen.
            DoubleBuffered = true;
            Padding = new Padding(8, 0, 8, 0);

            _inner = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font        = new Font("Segoe UI Variable", 9.5f),
                Dock        = DockStyle.None
            };
            Controls.Add(_inner);
            _inner.GotFocus  += (s, e) => { _focused = true;  Invalidate(); };
            _inner.LostFocus += (s, e) => { _focused = false; Invalidate(); };

            // Set height AFTER _inner is initialized to avoid null ref in OnLayout
            Height = 34;

            ApplyTheme();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_inner == null) return;
            int pad = 8;
            _inner.SetBounds(pad, (Height - _inner.Height) / 2, Width - pad * 2, _inner.Height);
        }

        public void ApplyTheme()
        {
            var p = ThemeManager.Current;
            BackColor        = p.InputBackground;
            ForeColor        = p.InputForeground;
            _inner.BackColor = p.InputBackground;
            _inner.ForeColor = p.InputForeground;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p  = ThemeManager.Current;
            var g  = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(p.InputBackground);
            var border = _focused ? p.InputBorderFocus : p.InputBorder;
            using var path = RoundRect(new Rectangle(0, 0, Width - 1, Height - 1), 5);
            g.DrawPath(new Pen(border, _focused ? 2f : 1f), path);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ModernProgressBar — Owner-drawn progress bar with rounded ends
    // ══════════════════════════════════════════════════════════════════════════
    public partial class ModernProgressBar : Control
    {
        private int _value;
        private int _maximum = 100;

        public int Value   { get => _value;   set { _value   = Math.Max(0, Math.Min(value, _maximum)); Invalidate(); } }
        public int Maximum { get => _maximum; set { _maximum = Math.Max(1, value); Invalidate(); } }

        public ModernProgressBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            Height = 8;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p  = ThemeManager.Current;
            var g  = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            var bgRc = new Rectangle(0, 0, Width - 1, Height - 1);
            using (var path = RoundRect(bgRc, Height / 2))
            using (var brush = new SolidBrush(p.ProgressBackground))
                g.FillPath(brush, path);

            double pct = (double)_value / _maximum;
            int fillW  = (int)(pct * (Width - 1));
            if (fillW > 4)
            {
                var fillRc = new Rectangle(0, 0, fillW, Height - 1);
                using var path  = RoundRect(fillRc, Height / 2);
                using var brush = new LinearGradientBrush(fillRc,
                    p.ProgressFill, p.AccentHover, LinearGradientMode.Horizontal);
                g.FillPath(brush, path);
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SideNavItem — Data model for a sidebar navigation entry
    // ══════════════════════════════════════════════════════════════════════════
    public class SideNavItem
    {
        public string  Key    { get; set; }
        public string  Label  { get; set; }
        public string  Icon   { get; set; }  // Segoe MDL2 / Fluent icon character
        public Control Page   { get; set; }  // Corresponding content panel
    }

    // ══════════════════════════════════════════════════════════════════════════
    // SideNavBar — Modern owner-drawn sidebar navigation control
    // ══════════════════════════════════════════════════════════════════════════
    public partial class SideNavBar : Control
    {
        private readonly System.Collections.Generic.List<SideNavItem> _items = new();
        private int _selectedIndex = 0;
        private int _hoverIndex    = -1;

        public event EventHandler<SideNavItem> SelectionChanged;

        public int ItemHeight { get; set; } = 44;
        public int TopPadding { get; set; } = 60;  // Offset below the logo panel

        public SideNavBar()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.Selectable, true);
            DoubleBuffered = true;
            Width = 200;
        }

        public void AddItem(SideNavItem item) { _items.Add(item); Invalidate(); }
        public void ClearItems() { _items.Clear(); Invalidate(); }

        public SideNavItem SelectedItem => _selectedIndex >= 0 && _selectedIndex < _items.Count
            ? _items[_selectedIndex] : null;

        public void Select(int index)
        {
            if (index < 0 || index >= _items.Count) return;
            _selectedIndex = index;
            Invalidate();
            SelectionChanged?.Invoke(this, _items[index]);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            int idx = GetItemAtY(e.Y);
            if (idx != _hoverIndex) { _hoverIndex = idx; Invalidate(); }
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _hoverIndex = -1; Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            int idx = GetItemAtY(e.Y);
            if (idx >= 0) Select(idx);
            base.OnMouseClick(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p  = ThemeManager.Current;
            var g  = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            // Background
            g.Clear(p.SidebarBackground);

            // Right-edge separator line
            g.DrawLine(new Pen(p.Separator), Width - 1, 0, Width - 1, Height);

            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                int y    = TopPadding + i * ItemHeight;
                var rc   = new Rectangle(6, y, Width - 12, ItemHeight - 4);
                bool sel = i == _selectedIndex;
                bool hov = i == _hoverIndex;

                // Item background
                Color bgColor = sel ? p.NavItemSelected : hov ? p.NavItemHover : Color.Transparent;
                if (bgColor.A > 0)
                {
                    using var path  = RoundRect(rc, 6);
                    using var brush = new SolidBrush(bgColor);
                    g.FillPath(brush, path);
                }

                // Selection indicator bar
                if (sel)
                {
                    int barH = 20;
                    var bar  = new Rectangle(rc.Left + 2, rc.Top + (rc.Height - barH) / 2, 3, barH);
                    using var barPath = RoundRect(bar, 2);
                    g.FillPath(new SolidBrush(p.NavItemSelectedFg), barPath);
                }

                // Icon
                var iconColor = sel ? p.NavItemSelectedFg : p.NavItemFg;
                if (!string.IsNullOrEmpty(item.Icon))
                {
                    using var iconFont = new Font("Segoe MDL2 Assets", 13f);
                    var iconRc = new Rectangle(rc.Left + 10, rc.Top, 28, rc.Height);
                    TextRenderer.DrawText(g, item.Icon, iconFont, iconRc, iconColor,
                        TextFormatFlags.VerticalCenter | TextFormatFlags.HorizontalCenter);
                }

                // Label
                var labelRc = new Rectangle(rc.Left + 44, rc.Top, rc.Width - 48, rc.Height);
                using var labelFont = sel
                    ? new Font("Segoe UI Variable", 9.5f, FontStyle.Bold)
                    : new Font("Segoe UI Variable", 9.5f, FontStyle.Regular);
                TextRenderer.DrawText(g, item.Label, labelFont, labelRc, iconColor,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            }
        }

        private int GetItemAtY(int y)
        {
            int relY = y - TopPadding;
            if (relY < 0) return -1;
            int idx = relY / ItemHeight;
            return idx < _items.Count ? idx : -1;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CardPanel — Rounded card container with optional border
    // ══════════════════════════════════════════════════════════════════════════
    public partial class CardPanel : Panel
    {
        public int CornerRadius { get; set; } = 8;
        public bool ShowBorder  { get; set; } = true;

        public CardPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            Padding = new Padding(12);
            // AutoSize is driven by PerformAutoSize() which we call after child layout
            AutoSize     = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
        }

        // Return the height needed to contain all child controls (respecting our padding).
        // Width is inherited from the parent layout (FlowLayoutPanel sets it explicitly).
        public override Size GetPreferredSize(Size proposedSize)
        {
            int bottom = 0;
            foreach (Control c in Controls)
            {
                int edge = c.Bottom + c.Margin.Bottom;
                if (edge > bottom) bottom = edge;
            }
            return new Size(proposedSize.Width > 0 ? proposedSize.Width : Width,
                            bottom + Padding.Bottom + 4);
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            // Re-measure height when a child is added or its layout changes
            e.Control.Layout  += (s, _) => PerformLayout();
            e.Control.Resize  += (s, _) => PerformLayout();
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (AutoSize) AdjustHeight();
        }

        private void AdjustHeight()
        {
            int bottom = 0;
            foreach (Control c in Controls)
            {
                int edge = c.Bottom + c.Margin.Bottom;
                if (edge > bottom) bottom = edge;
            }
            int needed = bottom + Padding.Bottom + 4;
            if (Height != needed) Height = needed;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // Fill entire bounds with parent background to avoid black corners on transparent regions
            e.Graphics.Clear(Parent?.BackColor ?? ThemeManager.Current.Background);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p  = ThemeManager.Current;
            var g  = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Fill parent background (handles partial invalidation)
            g.Clear(Parent?.BackColor ?? p.Background);

            // Inset by 0.5px so stroke stays fully inside bounds
            var rcF  = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
            using var path = RoundRectF(rcF, CornerRadius);

            // Card fill
            g.FillPath(new SolidBrush(p.SurfaceBackground), path);

            if (ShowBorder)
            {
                // Win11 card border: semi-transparent overlay (not a flat solid colour)
                // Light: ~8% black  #0000000E   Dark: ~12% white  #FFFFFF1E
                bool isDark = ThemeManager.CurrentTheme == Core.AppTheme.Dark;
                Color borderColor = isDark
                    ? Color.FromArgb(30,  255, 255, 255)   // 12% white
                    : Color.FromArgb(20,  0,   0,   0);    // 8% black
                using var pen = new Pen(borderColor, 1f);
                g.DrawPath(pen, path);

                // Top highlight — simulates Fluent edge illumination
                // Light: 60% white top edge  Dark: 18% white top edge
                int hiA = isDark ? 46 : 153;
                using var hiPen = new Pen(Color.FromArgb(hiA, 255, 255, 255), 1f);
                float cx = rcF.Left  + CornerRadius;
                float ex = rcF.Right - CornerRadius;
                if (ex > cx)
                    g.DrawLine(hiPen, cx, rcF.Top, ex, rcF.Top);
            }

            base.OnPaint(e);
        }

        private static GraphicsPath RoundRectF(RectangleF rc, int r)
        {
            var path = new GraphicsPath();
            if (r <= 0) { path.AddRectangle(rc); return path; }
            float d = r * 2f;
            path.AddArc(rc.Left,      rc.Top,       d, d, 180, 90);
            path.AddArc(rc.Right - d, rc.Top,       d, d, 270, 90);
            path.AddArc(rc.Right - d, rc.Bottom - d, d, d,   0, 90);
            path.AddArc(rc.Left,      rc.Bottom - d, d, d,  90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PageHeader — Large page title + subtitle
    // ══════════════════════════════════════════════════════════════════════════
    public class PageHeader : Control
    {
        public string Title    { get; set; } = "";
        public string Subtitle { get; set; } = "";

        private static readonly Font TitleFont    = new Font("Segoe UI Variable", 20f, FontStyle.Regular);
        private static readonly Font SubtitleFont = new Font("Segoe UI Variable", 9.5f, FontStyle.Regular);

        public PageHeader()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            Height = 72;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var p = ThemeManager.Current;
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(p.Background);

            TextRenderer.DrawText(g, Title,    TitleFont,    new Rectangle(0, 8, Width, 36), p.Foreground,
                TextFormatFlags.Left | TextFormatFlags.Top);
            TextRenderer.DrawText(g, Subtitle, SubtitleFont, new Rectangle(0, 44, Width, 22), p.ForegroundMuted,
                TextFormatFlags.Left | TextFormatFlags.Top);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // ModernToolStripRenderer — 主题化菜单渲染
    // ══════════════════════════════════════════════════════════════════════════
    public partial class ModernToolStripRenderer : ToolStripProfessionalRenderer
    {
        private ThemePalette _palette;

        public ModernToolStripRenderer(ThemePalette p) : base(new ModernColorTable(p)) => _palette = p;

        public void UpdatePalette(ThemePalette p)
        {
            _palette = p;
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var g  = e.Graphics;
            var rc = new Rectangle(2, 1, e.Item.Width - 4, e.Item.Height - 2);
            g.SmoothingMode = SmoothingMode.AntiAlias;

            if (e.Item.Selected || e.Item.Pressed)
            {
                using var path  = RoundRect(rc, 4);
                using var brush = new SolidBrush(_palette.MenuItemHover);
                g.FillPath(brush, path);
            }
            else
            {
                g.FillRectangle(new SolidBrush(_palette.MenuBackground), e.Item.Bounds);
            }
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            e.Graphics.Clear(_palette.MenuBackground);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            e.Graphics.DrawLine(new Pen(_palette.Separator),
                6, y, e.Item.Width - 6, y);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? _palette.MenuForeground : _palette.ForegroundDisabled;
            base.OnRenderItemText(e);
        }
    }

    internal class ModernColorTable : ProfessionalColorTable
    {
        private readonly ThemePalette _p;
        public ModernColorTable(ThemePalette p) => _p = p;
        public override Color MenuBorder              => _p.MenuBorder;
        public override Color MenuItemBorder          => Color.Transparent;
        public override Color MenuItemSelected        => _p.MenuItemHover;
        public override Color MenuItemSelectedGradientBegin => _p.MenuItemHover;
        public override Color MenuItemSelectedGradientEnd   => _p.MenuItemHover;
        public override Color MenuItemPressedGradientBegin  => _p.MenuItemSelected;
        public override Color MenuItemPressedGradientEnd    => _p.MenuItemSelected;
        public override Color ToolStripDropDownBackground   => _p.MenuBackground;
        public override Color ImageMarginGradientBegin      => _p.MenuBackground;
        public override Color ImageMarginGradientMiddle     => _p.MenuBackground;
        public override Color ImageMarginGradientEnd        => _p.MenuBackground;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // DrawHelper — Shared rounded-rectangle path builder
    // ══════════════════════════════════════════════════════════════════════════
    internal static class DrawHelper
    {
        public static GraphicsPath RoundRect(Rectangle rc, int r)
        {
            var path = new GraphicsPath();
            if (r <= 0) { path.AddRectangle(rc); return path; }
            r = Math.Min(r, Math.Min(rc.Width / 2, rc.Height / 2));
            path.AddArc(rc.Left, rc.Top, r * 2, r * 2, 180, 90);
            path.AddArc(rc.Right - r * 2, rc.Top, r * 2, r * 2, 270, 90);
            path.AddArc(rc.Right - r * 2, rc.Bottom - r * 2, r * 2, r * 2, 0, 90);
            path.AddArc(rc.Left, rc.Bottom - r * 2, r * 2, r * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // Convenience wrapper so all partial classes can call RoundRect directly
    internal static partial class ControlExtensions
    {
        public static GraphicsPath RoundRect(Rectangle rc, int r) => DrawHelper.RoundRect(rc, r);
    }
}

// Per-class static RoundRect shims (partial class pattern)
namespace winC2D.UI
{
    public partial class ModernButton        { static GraphicsPath RoundRect(Rectangle rc, int r) => DrawHelper.RoundRect(rc, r); }
    public partial class ModernTextBox       { static GraphicsPath RoundRect(Rectangle rc, int r) => DrawHelper.RoundRect(rc, r); }
    public partial class ModernProgressBar   { static GraphicsPath RoundRect(Rectangle rc, int r) => DrawHelper.RoundRect(rc, r); }
    public partial class SideNavBar          { static GraphicsPath RoundRect(Rectangle rc, int r) => DrawHelper.RoundRect(rc, r); }
    public partial class CardPanel           { static GraphicsPath RoundRect(Rectangle rc, int r) => DrawHelper.RoundRect(rc, r); }
    public partial class ModernToolStripRenderer { static GraphicsPath RoundRect(Rectangle rc, int r) => DrawHelper.RoundRect(rc, r); }
}
