using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using winC2D.Core;

namespace winC2D
{
    public class ModernTabControl : TabControl
    {
        public ModernTabControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.DrawMode = TabDrawMode.OwnerDrawFixed;
            this.Padding = new Point(24, 4); // Increase padding for larger click area
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit; // Fix blurry text
            
            // Fill background
            using (var brush = new SolidBrush(this.Parent.BackColor))
            {
                g.FillRectangle(brush, this.ClientRectangle);
            }

            // Draw tabs
            for (int i = 0; i < this.TabCount; i++)
            {
                DrawTab(g, i);
            }

            // Draw content area border/background if needed
            if (SelectedTab != null)
            {
                // The TabPage itself draws its background.
                // We might want to draw a border around the tab page content
                /*
                var pageRect = SelectedTab.Bounds;
                pageRect.Inflate(1, 1);
                using (var pen = new Pen(ThemeManager.GetPalette(ThemeManager.CurrentTheme).ButtonBorder))
                {
                    g.DrawRectangle(pen, pageRect);
                }
                */
            }
        }

        private void DrawTab(Graphics g, int index)
        {
            var bounds = GetTabRect(index);
            var rect = new RectangleF(bounds.X, bounds.Y, bounds.Width, bounds.Height);
            bool isSelected = this.SelectedIndex == index;
            var palette = ThemeManager.GetPalette(ThemeManager.CurrentTheme);

            // Text
            var text = this.TabPages[index].Text;
            var font = this.Font;
            
            // Win11 style: No background pill for unselected. 
            // Selected: Simple text bold/colored + bottom indicator line OR light background pill.
            // Using "Pill" style for better visibility in this app context.
            
            if (isSelected)
            {
                // Selected background (Rounded Rect)
                // Slightly smaller than bounds
                var bgRect = rect;
                bgRect.Inflate(-2, -2);
                
                // If Light theme: light gray bg. If Dark theme: dark gray bg.
                // Using ControlBackground or a specific color
                var bgFill = ThemeManager.CurrentTheme == AppTheme.Dark 
                    ? Color.FromArgb(60, 60, 60) 
                    : Color.White;

                using (var path = GetRoundedRect(bgRect, 4))
                using (var brush = new SolidBrush(bgFill))
                {
                    g.FillPath(brush, path);
                }

                // Bottom indicator stripe (Accent)
                float stripeHeight = 3;
                var stripeRect = new RectangleF(bgRect.X + 8, bgRect.Bottom - stripeHeight - 2, bgRect.Width - 16, stripeHeight);
                using (var brush = new SolidBrush(palette.Accent))
                {
                    g.FillRectangle(brush, stripeRect);
                }
            }
            else
            {
                // Hover state could be handled if we tracked mouse, skipping for simplicity now
            }

            // Draw Text
            var textColor = isSelected ? palette.Foreground : Color.Gray; // Unselected text is grayish
            // Assuming bold for selected
            var textStyle = isSelected ? FontStyle.Bold : FontStyle.Regular;
            using (var fontToUse = new Font(font, textStyle))
            using (var brush = new SolidBrush(textColor))
            {
                var format = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(text, fontToUse, brush, rect, format);
            }
        }

        private GraphicsPath GetRoundedRect(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
        
        // Remove background of the control itself to let parent show through?
        // But we are UserPaint, so we control it.
    }

    public class ModernButton : Button
    {
        public ModernButton()
        {
            this.FlatStyle = FlatStyle.Flat;
            this.FlatAppearance.BorderSize = 0;
            this.Size = new Size(150, 40);
            // We use Paint event, so standard FlatStyle props might be ignored for drawing, 
            // but key for behavior.
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // Set Region to clip corners to fix white corners on dark background
            if (this.Region != null) this.Region.Dispose();
            
            // Create a path slightly larger to clip edges smoothly but ensure background is fully covered
            using (var clipPath = GetRoundedRect(new RectangleF(0, 0, this.Width, this.Height), 4))
            {
                this.Region = new Region(clipPath);
            }

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            var palette = ThemeManager.GetPalette(ThemeManager.CurrentTheme);
            
            // Determine state colors
            Color backColor = this.BackColor; 
            Color foreColor = this.ForeColor;

            // Handle disabled state visually
            if (!this.Enabled)
            {
                // Dimmed colors for disabled state
                backColor = ThemeManager.CurrentTheme == AppTheme.Dark 
                    ? Color.FromArgb(40, 40, 40) 
                    : Color.FromArgb(230, 230, 230);
                foreColor = Color.Gray;
            }

            var rect = new RectangleF(0, 0, this.Width, this.Height);
            
            // Background
            // Note: because we set Region, we can just fill the rectangle, but filling path is safer for anti-aliasing edges
            using (var path = GetRoundedRect(rect, 4))
            using (var brush = new SolidBrush(backColor))
            {
                g.FillPath(brush, path);
                
                bool isAccent = (backColor.ToArgb() == palette.Accent.ToArgb());
                
                // Draw border if needed (and enabled)
                if (!isAccent && this.Enabled)
                {
                    using (var pen = new Pen(palette.ButtonBorder, 1))
                    {
                        g.DrawPath(pen, path);
                    }
                }
            }

            // Text
            var format = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            
            using (var brush = new SolidBrush(foreColor))
            {
                g.DrawString(this.Text, this.Font, brush, rect, format);
            }
        }

        private GraphicsPath GetRoundedRect(RectangleF rect, float radius)
        {
            // Correction to fit within bounds
            rect.Width -= 1;
            rect.Height -= 1;
            
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
