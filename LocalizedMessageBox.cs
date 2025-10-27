using System;
using System.Drawing;
using System.Windows.Forms;

namespace winC2D
{
    public static class LocalizedMessageBox
    {
        public static DialogResult Show(string message, string title, MessageBoxButtons buttons = MessageBoxButtons.OK, MessageBoxIcon icon = MessageBoxIcon.None)
        {
            using (var dlg = new LocalizedMessageBoxForm(message, title, buttons, icon))
            {
                return dlg.ShowDialog();
            }
        }
    }

    public class LocalizedMessageBoxForm : Form
    {
        private Label labelMessage;
        private Button button1, button2, button3;
        public LocalizedMessageBoxForm(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(400, 140);
            this.Font = SystemFonts.MessageBoxFont;

            labelMessage = new Label
            {
                Text = message,
                Location = new Point(20, 20),
                Size = new Size(360, 60),
                AutoSize = false
            };
            this.Controls.Add(labelMessage);

            button1 = new Button { Size = new Size(80, 28) };
            button2 = new Button { Size = new Size(80, 28) };
            button3 = new Button { Size = new Size(80, 28) };

            int btnY = 90;
            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    button1.Text = Localization.T("Button.OK");
                    button1.Location = new Point(160, btnY);
                    button1.DialogResult = DialogResult.OK;
                    this.Controls.Add(button1);
                    this.AcceptButton = button1;
                    this.CancelButton = button1;
                    break;
                case MessageBoxButtons.OKCancel:
                    button1.Text = Localization.T("Button.OK");
                    button2.Text = Localization.T("Button.Cancel");
                    button1.Location = new Point(110, btnY);
                    button2.Location = new Point(210, btnY);
                    button1.DialogResult = DialogResult.OK;
                    button2.DialogResult = DialogResult.Cancel;
                    this.Controls.Add(button1);
                    this.Controls.Add(button2);
                    this.AcceptButton = button1;
                    this.CancelButton = button2;
                    break;
                case MessageBoxButtons.YesNo:
                    button1.Text = Localization.T("Button.Yes");
                    button2.Text = Localization.T("Button.No");
                    button1.Location = new Point(110, btnY);
                    button2.Location = new Point(210, btnY);
                    button1.DialogResult = DialogResult.Yes;
                    button2.DialogResult = DialogResult.No;
                    this.Controls.Add(button1);
                    this.Controls.Add(button2);
                    this.AcceptButton = button1;
                    this.CancelButton = button2;
                    break;
                case MessageBoxButtons.YesNoCancel:
                    button1.Text = Localization.T("Button.Yes");
                    button2.Text = Localization.T("Button.No");
                    button3.Text = Localization.T("Button.Cancel");
                    button1.Location = new Point(60, btnY);
                    button2.Location = new Point(160, btnY);
                    button3.Location = new Point(260, btnY);
                    button1.DialogResult = DialogResult.Yes;
                    button2.DialogResult = DialogResult.No;
                    button3.DialogResult = DialogResult.Cancel;
                    this.Controls.Add(button1);
                    this.Controls.Add(button2);
                    this.Controls.Add(button3);
                    this.AcceptButton = button1;
                    this.CancelButton = button3;
                    break;
            }
        }
    }
}
