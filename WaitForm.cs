using System;
using System.Windows.Forms;

namespace winC2D
{
    public class WaitForm : Form
    {
        private Label label;
        public WaitForm(string message)
        {
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Width = 320;
            this.Height = 120;
            this.ControlBox = false;
            this.ShowInTaskbar = false;
            label = new Label
            {
                Text = message,
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill
            };
            this.Controls.Add(label);
        }
    }
}
