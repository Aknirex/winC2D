using System;
using System.Windows.Forms;
using Microsoft.Win32;

namespace winC2D
{
    public partial class SettingsForm : Form
    {
        private const string DEFAULT_PROGRAM_FILES = @"C:\Program Files";
        private const string DEFAULT_PROGRAM_FILES_X86 = @"C:\Program Files (x86)";
        private const string REG_PATH = @"SOFTWARE\Microsoft\Windows\CurrentVersion";
        
        public SettingsForm()
        {
            InitializeComponent();
            this.Load += SettingsForm_Load;
            ApplyLocalization();
        }

        private void ApplyLocalization()
        {
            this.Text = Localization.T("Settings.Title");
            
            // Program Files 组
            groupBoxProgramFiles.Text = Localization.T("Settings.ProgramFilesSection");
            labelProgramFilesNote.Text = Localization.T("Settings.ProgramFilesNote");
            labelAppInstall.Text = Localization.T("Settings.ProgramFilesPath");
            labelAppInstallX86.Text = Localization.T("Settings.ProgramFilesPathX86");
            checkBoxCustomX86.Text = Localization.T("Settings.CustomX86Path");
            
            // Storage Policy 组
            groupBoxStoragePolicy.Text = Localization.T("Settings.StoragePolicySection");
            labelStoragePolicyNote.Text = Localization.T("Settings.StoragePolicyNote");
            
            // 按钮
            buttonBrowseAppInstall.Text = Localization.T("Button.Browse");
            buttonBrowseAppInstallX86.Text = Localization.T("Button.Browse");
            buttonSetAppInstall.Text = Localization.T("Button.Apply");
            buttonReset.Text = Localization.T("Button.Reset");
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            var (path64, pathX86) = GetCurrentProgramFilesPaths();
            textBoxAppInstall.Text = path64;
            textBoxAppInstallX86.Text = pathX86;
            
            // 只在64位系统上显示32位选项
            bool is64Bit = Environment.Is64BitOperatingSystem;
            checkBoxCustomX86.Visible = is64Bit;
            labelAppInstallX86.Visible = is64Bit;
            textBoxAppInstallX86.Visible = is64Bit;
            buttonBrowseAppInstallX86.Visible = is64Bit;
        }

        private void textBoxAppInstall_TextChanged(object sender, EventArgs e)
        {
            // 如果未勾选自定义32位路径，自动设置
            if (!checkBoxCustomX86.Checked && Environment.Is64BitOperatingSystem)
            {
                string path64 = textBoxAppInstall.Text;
                if (!string.IsNullOrEmpty(path64))
                {
                    // 自动生成32位路径：将 "Program Files" 替换为 "Program Files (x86)"
                    string pathX86 = path64.Replace("Program Files", "Program Files (x86)");
                    textBoxAppInstallX86.Text = pathX86;
                }
            }
        }

        private void checkBoxCustomX86_CheckedChanged(object sender, EventArgs e)
        {
            bool customEnabled = checkBoxCustomX86.Checked;
            labelAppInstallX86.Enabled = customEnabled;
            textBoxAppInstallX86.Enabled = customEnabled;
            buttonBrowseAppInstallX86.Enabled = customEnabled;
            
            // 如果取消勾选，自动设置32位路径
            if (!customEnabled)
            {
                textBoxAppInstall_TextChanged(null, null);
            }
        }

        private void buttonBrowseAppInstall_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = Localization.T("Msg.SelectFolder");
                fbd.ShowNewFolderButton = true;
                fbd.SelectedPath = textBoxAppInstall.Text;
                
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    textBoxAppInstall.Text = fbd.SelectedPath;
                }
            }
        }

        private void buttonBrowseAppInstallX86_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                fbd.Description = Localization.T("Msg.SelectFolder");
                fbd.ShowNewFolderButton = true;
                fbd.SelectedPath = textBoxAppInstallX86.Text;
                
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    textBoxAppInstallX86.Text = fbd.SelectedPath;
                }
            }
        }

        private void buttonSetAppInstall_Click(object sender, EventArgs e)
        {
            string path64 = textBoxAppInstall.Text.Trim();
            string pathX86 = textBoxAppInstallX86.Text.Trim();
            
            if (string.IsNullOrEmpty(path64) || !System.IO.Directory.Exists(path64))
            {
                MessageBox.Show(
                    Localization.T("Msg.InvalidPath"), 
                    Localization.T("Title.Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }
            
            // 如果是64位系统且32位路径无效，提示错误
            if (Environment.Is64BitOperatingSystem)
            {
                if (string.IsNullOrEmpty(pathX86) || !System.IO.Directory.Exists(pathX86))
                {
                    MessageBox.Show(
                        Localization.T("Msg.InvalidPath"), 
                        Localization.T("Title.Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }
            }
            
            try
            {
                SetProgramFilesPaths(path64, pathX86);
                MessageBox.Show(
                    Localization.T("Msg.SettingsApplied"),
                    Localization.T("Title.Tip"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Localization.T("Msg.SettingsFailed"), ex.Message),
                    Localization.T("Title.Error"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                Localization.T("Msg.ResetConfirm"),
                Localization.T("Title.Tip"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);
            
            if (result == DialogResult.Yes)
            {
                try
                {
                    SetProgramFilesPaths(DEFAULT_PROGRAM_FILES, DEFAULT_PROGRAM_FILES_X86);
                    textBoxAppInstall.Text = DEFAULT_PROGRAM_FILES;
                    textBoxAppInstallX86.Text = DEFAULT_PROGRAM_FILES_X86;
                    checkBoxCustomX86.Checked = false;
                    MessageBox.Show(
                        Localization.T("Msg.SettingsApplied"),
                        Localization.T("Title.Tip"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        string.Format(Localization.T("Msg.SettingsFailed"), ex.Message),
                        Localization.T("Title.Error"),
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// 获取当前 Program Files 路径（64位和32位）
        /// </summary>
        private (string path64, string pathX86) GetCurrentProgramFilesPaths()
        {
            string path64 = DEFAULT_PROGRAM_FILES;
            string pathX86 = DEFAULT_PROGRAM_FILES_X86;
            
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(REG_PATH, false))
                {
                    if (key != null)
                    {
                        // 读取 64 位路径
                        string temp = key.GetValue("ProgramFilesDir") as string;
                        if (!string.IsNullOrEmpty(temp))
                            path64 = temp;
                        
                        // 读取 32 位路径
                        if (Environment.Is64BitOperatingSystem)
                        {
                            temp = key.GetValue("ProgramFilesDir (x86)") as string;
                            if (!string.IsNullOrEmpty(temp))
                                pathX86 = temp;
                        }
                    }
                }
            }
            catch { }
            
            return (path64, pathX86);
        }

        /// <summary>
        /// 设置 Program Files 路径（修改所有相关注册表项）
        /// </summary>
        private void SetProgramFilesPaths(string path64, string pathX86)
        {
            using (var key = Registry.LocalMachine.OpenSubKey(REG_PATH, true))
            {
                if (key == null)
                    throw new Exception("Unable to open registry key");
                
                // 设置 64 位 Program Files 目录
                key.SetValue("ProgramFilesDir", path64);
                
                // 如果是 64 位系统，设置额外的键
                if (Environment.Is64BitOperatingSystem)
                {
                    // ProgramW6432Dir 用于 64 位程序
                    key.SetValue("ProgramW6432Dir", path64);
                    
                    // ProgramFilesDir (x86) 用于 32 位程序
                    key.SetValue("ProgramFilesDir (x86)", pathX86);
                }
            }
        }
    }
}