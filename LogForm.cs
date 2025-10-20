using System;
using System.IO;
using System.Windows.Forms;

namespace winC2D
{
    public partial class LogForm : Form
    {
        public LogForm()
        {
            InitializeComponent();
            this.Load += LogForm_Load;
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            // 窗口标题
            this.Text = Localization.T("Log.Title");

            // 列标题
            columnTime.Text = Localization.T("Log.Time");
            columnName.Text = Localization.T("Log.SoftwareName");
            columnOldPath.Text = Localization.T("Log.OldPath");
            columnNewPath.Text = Localization.T("Log.NewPath");
            columnStatus.Text = Localization.T("Log.Status");
            columnMsg.Text = Localization.T("Log.Message");

            // 按钮
            buttonRollback.Text = Localization.T("Button.Rollback");
        }

        private void LogForm_Load(object sender, EventArgs e)
        {
            listViewLog.Items.Clear();
            var logs = MigrationLogger.ReadAll();
            foreach (var entry in logs)
            {
                var item = new ListViewItem(new string[]
                {
                    entry.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                    entry.SoftwareName,
                    entry.OldPath,
                    entry.NewPath,
                    entry.Status,
                    entry.Message
                });
                listViewLog.Items.Add(item);
            }
        }

        private void buttonRollback_Click(object sender, EventArgs e)
        {
            if (listViewLog.SelectedItems.Count == 0)
            {
                MessageBox.Show(
                    Localization.T("Msg.SelectLogEntry"), 
                    Localization.T("Title.Tip"));
                return;
            }
            var item = listViewLog.SelectedItems[0];
            string oldPath = item.SubItems[2].Text;
            string newPath = item.SubItems[3].Text;
            string name = item.SubItems[1].Text;
            if (!Directory.Exists(newPath))
            {
                MessageBox.Show(
                    Localization.T("Msg.NewPathNotExist"), 
                    Localization.T("Title.Error"));
                return;
            }
            if (Directory.Exists(oldPath))
            {
                MessageBox.Show(
                    Localization.T("Msg.OldPathExists"), 
                    Localization.T("Title.Error"));
                return;
            }
            try
            {
                Directory.Move(newPath, oldPath);
                SoftwareMigrator.UpdateRegistryInstallLocation(oldPath, oldPath); // 恢复注册表
                try { ShortcutHelper.FixShortcuts(newPath, oldPath); } catch { }
                MigrationLogger.Log(new MigrationLogEntry
                {
                    Time = DateTime.Now,
                    SoftwareName = name,
                    OldPath = newPath,
                    NewPath = oldPath,
                    Status = "Rollback",
                    Message = Localization.T("Msg.RollbackSuccess")
                });
                MessageBox.Show(Localization.T("Msg.RollbackSuccess"));
            }
            catch (Exception ex)
            {
                MigrationLogger.Log(new MigrationLogEntry
                {
                    Time = DateTime.Now,
                    SoftwareName = name,
                    OldPath = newPath,
                    NewPath = oldPath,
                    Status = "RollbackFail",
                    Message = ex.Message
                });
                MessageBox.Show(string.Format(Localization.T("Msg.RollbackFailedFmt"), ex.Message));
            }
            LogForm_Load(null, null); // 刷新日志
        }
    }
}
