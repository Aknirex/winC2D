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
                MessageBox.Show("请先选择要回滚的迁移记录。", "提示");
                return;
            }
            var item = listViewLog.SelectedItems[0];
            string oldPath = item.SubItems[2].Text;
            string newPath = item.SubItems[3].Text;
            string name = item.SubItems[1].Text;
            if (!Directory.Exists(newPath))
            {
                MessageBox.Show("新路径目录不存在，无法回滚。", "错误");
                return;
            }
            if (Directory.Exists(oldPath))
            {
                MessageBox.Show("原路径已存在，无法回滚。", "错误");
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
                    Message = "回滚成功"
                });
                MessageBox.Show("回滚成功！");
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
                MessageBox.Show("回滚失败：" + ex.Message);
            }
            LogForm_Load(null, null); // 刷新日志
        }
    }
}
