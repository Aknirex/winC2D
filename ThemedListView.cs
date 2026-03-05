using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using winC2D.Core;

namespace winC2D
{
    public class ThemedListView : ListView
    {
        private HeaderWindow headerWindow;

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            AttachHeader();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            headerWindow?.ReleaseHandle();
            headerWindow = null;
            base.OnHandleDestroyed(e);
        }

        private void AttachHeader()
        {
            headerWindow?.ReleaseHandle();
            var headerHandle = NativeMethods.SendMessage(Handle, NativeMethods.LVM_GETHEADER, IntPtr.Zero, IntPtr.Zero);
            if (headerHandle != IntPtr.Zero)
            {
                headerWindow = new HeaderWindow(this);
                headerWindow.AssignHandle(headerHandle);
            }
        }

        private void PaintHeaderBackground(Graphics graphics)
        {
            var palette = ThemeManager.GetPalette(ThemeManager.CurrentTheme);
            var headerBackground = palette.ListViewHeaderBackground.A == 0 ? palette.ControlBackground : palette.ListViewHeaderBackground;
            var bounds = GetHeaderBounds();
            using (var brush = new SolidBrush(headerBackground))
            {
                graphics.FillRectangle(brush, bounds);
            }
        }

        private void PaintHeaderBottomLine(Graphics graphics)
        {
            var palette = ThemeManager.GetPalette(ThemeManager.CurrentTheme);
            var bounds = GetHeaderBounds();
            using (var pen = new Pen(palette.ButtonBorder))
            {
                graphics.DrawLine(pen, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);
            }
        }

        private Rectangle GetHeaderBounds()
        {
            if (headerWindow == null)
            {
                return Rectangle.Empty;
            }

            NativeMethods.GetClientRect(headerWindow.Handle, out var rect);
            return Rectangle.FromLTRB(rect.Left, rect.Top, rect.Right, rect.Bottom);
        }

        private sealed class HeaderWindow : NativeWindow
        {
            private readonly ThemedListView owner;

            public HeaderWindow(ThemedListView owner)
            {
                this.owner = owner;
            }

            protected override void WndProc(ref Message m)
            {
                if (ThemeManager.CurrentTheme == AppTheme.Dark)
                {
                    switch (m.Msg)
                    {
                        case NativeMethods.WM_ERASEBKGND:
                            using (var graphics = Graphics.FromHdc(m.WParam))
                            {
                                owner.PaintHeaderBackground(graphics);
                            }
                            m.Result = (IntPtr)1;
                            return;
                        case NativeMethods.WM_PAINT:
                            base.WndProc(ref m);
                            using (var graphics = Graphics.FromHwnd(Handle))
                            {
                                owner.PaintHeaderBottomLine(graphics);
                            }
                            return;
                    }
                }

                base.WndProc(ref m);
            }
        }

        private static class NativeMethods
        {
            public const int LVM_FIRST = 0x1000;
            public const int LVM_GETHEADER = LVM_FIRST + 31;
            public const int WM_ERASEBKGND = 0x0014;
            public const int WM_PAINT = 0x000F;

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;
            }
        }
    }
}
