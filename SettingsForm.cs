using System;
using System.Windows.Forms;

namespace winC2D
{
    public partial class SettingsForm : Form
    {
        public SettingsForm()
        {
            InitializeComponent();
            this.Load += SettingsForm_Load;
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            textBoxAppInstall.Text = GetCurrentAppInstallPath();
        }

        private void buttonBrowseAppInstall_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = "请选择新应用默认安装位置";
                fbd.ShowNewFolderButton = true;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    textBoxAppInstall.Text = fbd.SelectedPath;
                }
            }
        }

        private void buttonSetAppInstall_Click(object sender, EventArgs e)
        {
            string path = textBoxAppInstall.Text.Trim();
            if (string.IsNullOrEmpty(path) || !System.IO.Directory.Exists(path))
            {
                MessageBox.Show("请输入有效的文件夹路径。", "提示");
                return;
            }
            try
            {
                SetAppInstallPath(path);
                MessageBox.Show("设置成功！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("设置失败：" + ex.Message);
            }
        }

        // 获取当前新应用默认安装位置（注册表）
        private string GetCurrentAppInstallPath()
        {
            var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion", false);
            if (key == null) return string.Empty;
            return key.GetValue("ProgramFilesDir") as string ?? string.Empty;
        }

        // 设置新应用默认安装位置（注册表）
        private void SetAppInstallPath(string path)
        {
            var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                "SOFTWARE\\Microsoft\\Windows\\CurrentVersion", true);
            if (key == null) throw new Exception("无法打开注册表");
            key.SetValue("ProgramFilesDir", path);
        }
    }
}