using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace winC2D
{
    public class AppDetailForm : Form
    {
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public AppDetailForm(Icon appIcon)
        {
            this.Text = "关于 winC2D";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(400, 220);
            
            // Apply Font
            this.Font = new Font("Segoe UI Variable", 9F, FontStyle.Regular, GraphicsUnit.Point);

            ApplyTheme(ThemeManager.CurrentTheme);
            ThemeManager.ThemeChanged += (s, e) => ApplyTheme(ThemeManager.CurrentTheme);
            this.FormClosing += (s, e) => ThemeManager.ThemeChanged -= (EventHandler)((s2, e2) => ApplyTheme(ThemeManager.CurrentTheme)); 
            // Note: lambda subscription unsubscribe tricky, simplified here assumption form is short lived modal

            var bestBitmap = ExtractBestIconBitmap();
            var pictureBox = new PictureBox
            {
                Image = bestBitmap ?? IconToBitmapEdge(appIcon, 64, 64),
                SizeMode = PictureBoxSizeMode.Normal,
                Location = new Point(20, 20),
                Size = new Size(64, 64)
            };
            this.Controls.Add(pictureBox);

            var labelTitle = new Label
            {
                Text = "winC2D",
                Font = new Font("Segoe UI Variable", 12, FontStyle.Bold),
                Location = new Point(100, 20),
                AutoSize = true
            };
            this.Controls.Add(labelTitle);

            var labelAuthor = new Label
            {
                Text = "Author: Aknirex",
                Location = new Point(100, 55),
                AutoSize = true
            };
            this.Controls.Add(labelAuthor);

            var labelGithub = new LinkLabel
            {
                Text = "GitHub: https://github.com/Aknirex/winC2D",
                Location = new Point(100, 85),
                AutoSize = true
            };
            labelGithub.LinkClicked += (s, e) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/Aknirex/winC2D",
                    UseShellExecute = true
                });
            };
            this.Controls.Add(labelGithub);

            var labelCopyright = new Label
            {
                Text = "Copyright Aknirex",
                Location = new Point(20, 120),
                AutoSize = true
            };
            this.Controls.Add(labelCopyright);
            
            ApplyTheme(ThemeManager.CurrentTheme); // Apply again to ensure controls created are styled
        }

        private void ApplyTheme(AppTheme theme)
        {
            var palette = ThemeManager.GetPalette(theme);
            if (Environment.OSVersion.Version.Major >= 10)
            {
                int useImmersiveDarkMode = (theme == AppTheme.Dark) ? 1 : 0;
                DwmSetWindowAttribute(this.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useImmersiveDarkMode, sizeof(int));
            }
            this.BackColor = palette.FormBackground;
            this.ForeColor = palette.Foreground;
            
            foreach(Control c in this.Controls)
            {
                if (c is Label lbl) lbl.ForeColor = palette.Foreground;
                if (c is LinkLabel ll) ll.LinkColor = palette.Accent;
            }
        }

        // 优先提取256x256的Bitmap（如有），否则返回null
        private Bitmap ExtractBestIconBitmap()
        {
            try
            {
                var asm = typeof(AppDetailForm).Assembly;
                using var stream = asm.GetManifestResourceStream("winC2D.winc2d.ico");
                if (stream != null)
                {
                    using var icon = new Icon(stream, new Size(256, 256));
                    var bmp = icon.ToBitmap();
                    if (bmp.Width >= 128 && bmp.Height >= 128)
                    {
                        // 用边缘采样缩放到64x64
                        return ResizeBitmapEdge(bmp, 64, 64);
                    }
                }
            }
            catch { }
            return null;
        }

        // 使用边缘采样缩放Bitmap，避免模糊
        private Bitmap ResizeBitmapEdge(Bitmap src, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.Clear(Color.Transparent);
                g.DrawImage(src, new Rectangle(0, 0, width, height));
            }
            return bmp;
        }

        // 兼容原有Icon缩放
        private Bitmap IconToBitmapEdge(Icon icon, int width, int height)
        {
            Bitmap bmp = new Bitmap(width, height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
                g.Clear(Color.Transparent);
                g.DrawImage(icon.ToBitmap(), new Rectangle(0, 0, width, height));
            }
            return bmp;
        }
    }
}
