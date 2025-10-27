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
        private TextBox textBoxMessage;
        private Button button1, button2, button3;
        public LocalizedMessageBoxForm(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.Sizable; // 可调大小
            this.MaximizeBox = true;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.ClientSize = new Size(500, 220);
            this.Font = SystemFonts.MessageBoxFont;
            this.MinimumSize = new Size(400, 180);

            textBoxMessage = new TextBox
            {
                Text = message,
                Location = new Point(20, 20),
                Size = new Size(440, 100),
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom
            };
            this.Controls.Add(textBoxMessage);

            button1 = new Button { Size = new Size(80, 28) };
            button2 = new Button { Size = new Size(80, 28) };
            button3 = new Button { Size = new Size(80, 28) };

            int btnY = 140;
            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    button1.Text = Localization.T("Button.OK");
                    button1.Location = new Point(210, btnY);
                    button1.DialogResult = DialogResult.OK;
                    this.Controls.Add(button1);
                    this.AcceptButton = button1;
                    this.CancelButton = button1;
                    break;
                case MessageBoxButtons.OKCancel:
                    button1.Text = Localization.T("Button.OK");
                    button2.Text = Localization.T("Button.Cancel");
                    button1.Location = new Point(160, btnY);
                    button2.Location = new Point(260, btnY);
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
                    button1.Location = new Point(160, btnY);
                    button2.Location = new Point(260, btnY);
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
                    button1.Location = new Point(110, btnY);
                    button2.Location = new Point(210, btnY);
                    button3.Location = new Point(310, btnY);
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
            // 响应窗口大小变化，自动调整文本框大小
            this.Resize += (s, e) =>
            {
                textBoxMessage.Width = this.ClientSize.Width - 40;
                textBoxMessage.Height = this.ClientSize.Height - 80;
                int btnBaseY = textBoxMessage.Bottom + 20;
                button1.Top = button2.Top = button3.Top = btnBaseY;
            };
        }
    }
}
