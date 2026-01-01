using System;
using System.Windows.Forms;
using System.Drawing;

namespace winC2D
{
    public class WaitForm : Form
    {
        private Label label;
        private ProgressBar progress;
        public WaitForm(string message)
        {
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Width = 360;
            this.Height = 140;
            this.ControlBox = false;
            this.ShowInTaskbar = false;

            label = new Label
            {
                Text = message,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Height = 60
            };

            progress = new ProgressBar
            {
                Style = ProgressBarStyle.Marquee,
                MarqueeAnimationSpeed = 30,
                Dock = DockStyle.Bottom,
                Height = 24
            };

            this.Padding = new Padding(12, 12, 12, 12);
            var panel = new Panel
            {
                Dock = DockStyle.Fill
            };
            panel.Controls.Add(label);
            panel.Controls.Add(progress);

            this.Controls.Add(panel);
        }
    }
}
