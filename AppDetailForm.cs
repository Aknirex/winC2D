using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace winC2D
{
    public class AppDetailForm : Form
    {
        public AppDetailForm(Icon appIcon)
        {
            this.Text = "关于 winC2D";
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(400, 220);

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
                Font = new Font(FontFamily.GenericSansSerif, 12, FontStyle.Bold),
                Location = new Point(100, 20),
                AutoSize = true
            };
            this.Controls.Add(labelTitle);

            var labelAuthor = new Label
            {
                Text = "Author: SKR7lex",
                Location = new Point(100, 55),
                AutoSize = true
            };
            this.Controls.Add(labelAuthor);

            var labelGithub = new LinkLabel
            {
                Text = "GitHub: https://github.com/SKR7lex/winC2D",
                Location = new Point(100, 85),
                AutoSize = true
            };
            labelGithub.LinkClicked += (s, e) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/SKR7lex/winC2D",
                    UseShellExecute = true
                });
            };
            this.Controls.Add(labelGithub);

            var labelCopyright = new Label
            {
                Text = "Copyright SKR7lex",
                Location = new Point(20, 120),
                AutoSize = true
            };
            this.Controls.Add(labelCopyright);
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
