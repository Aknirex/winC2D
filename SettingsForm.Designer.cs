namespace winC2D
{
    partial class SettingsForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.groupBoxProgramFiles = new System.Windows.Forms.GroupBox();
            this.labelProgramFilesNote = new System.Windows.Forms.Label();
            this.buttonReset = new System.Windows.Forms.Button();
            this.buttonSetAppInstall = new System.Windows.Forms.Button();
            this.buttonBrowseAppInstallX86 = new System.Windows.Forms.Button();
            this.textBoxAppInstallX86 = new System.Windows.Forms.TextBox();
            this.labelAppInstallX86 = new System.Windows.Forms.Label();
            this.checkBoxCustomX86 = new System.Windows.Forms.CheckBox();
            this.buttonBrowseAppInstall = new System.Windows.Forms.Button();
            this.textBoxAppInstall = new System.Windows.Forms.TextBox();
            this.labelAppInstall = new System.Windows.Forms.Label();
            this.groupBoxStoragePolicy = new System.Windows.Forms.GroupBox();
            this.labelStoragePolicyNote = new System.Windows.Forms.Label();
            this.groupBoxProgramFiles.SuspendLayout();
            this.groupBoxStoragePolicy.SuspendLayout();
            this.SuspendLayout();
            // 
            // groupBoxProgramFiles
            // 
            this.groupBoxProgramFiles.Controls.Add(this.labelProgramFilesNote);
            this.groupBoxProgramFiles.Controls.Add(this.buttonReset);
            this.groupBoxProgramFiles.Controls.Add(this.buttonSetAppInstall);
            this.groupBoxProgramFiles.Controls.Add(this.buttonBrowseAppInstallX86);
            this.groupBoxProgramFiles.Controls.Add(this.textBoxAppInstallX86);
            this.groupBoxProgramFiles.Controls.Add(this.labelAppInstallX86);
            this.groupBoxProgramFiles.Controls.Add(this.checkBoxCustomX86);
            this.groupBoxProgramFiles.Controls.Add(this.buttonBrowseAppInstall);
            this.groupBoxProgramFiles.Controls.Add(this.textBoxAppInstall);
            this.groupBoxProgramFiles.Controls.Add(this.labelAppInstall);
            this.groupBoxProgramFiles.Location = new System.Drawing.Point(15, 15);
            this.groupBoxProgramFiles.Name = "groupBoxProgramFiles";
            this.groupBoxProgramFiles.Size = new System.Drawing.Size(820, 250);
            this.groupBoxProgramFiles.TabIndex = 0;
            this.groupBoxProgramFiles.TabStop = false;
            this.groupBoxProgramFiles.Text = "Program Files 位置（传统桌面程序）";
            // 
            // labelProgramFilesNote
            // 
            this.labelProgramFilesNote.AutoSize = false;
            this.labelProgramFilesNote.ForeColor = System.Drawing.Color.DarkBlue;
            this.labelProgramFilesNote.Location = new System.Drawing.Point(15, 185);
            this.labelProgramFilesNote.Name = "labelProgramFilesNote";
            this.labelProgramFilesNote.Size = new System.Drawing.Size(790, 50);
            this.labelProgramFilesNote.TabIndex = 9;
            this.labelProgramFilesNote.Text = "ℹ 此设置影响通过安装程序（.exe、.msi）安装的传统桌面程序。大多数第三方软件使用此位置。";
            // 
            // labelAppInstall
            // 
            this.labelAppInstall.AutoSize = true;
            this.labelAppInstall.Location = new System.Drawing.Point(15, 30);
            this.labelAppInstall.Name = "labelAppInstall";
            this.labelAppInstall.Size = new System.Drawing.Size(250, 20);
            this.labelAppInstall.TabIndex = 0;
            this.labelAppInstall.Text = "Program Files 默认位置（64位）：";
            // 
            // textBoxAppInstall
            // 
            this.textBoxAppInstall.Location = new System.Drawing.Point(15, 55);
            this.textBoxAppInstall.Name = "textBoxAppInstall";
            this.textBoxAppInstall.Size = new System.Drawing.Size(520, 27);
            this.textBoxAppInstall.TabIndex = 1;
            this.textBoxAppInstall.TextChanged += new System.EventHandler(this.textBoxAppInstall_TextChanged);
            // 
            // buttonBrowseAppInstall
            // 
            this.buttonBrowseAppInstall.Location = new System.Drawing.Point(545, 54);
            this.buttonBrowseAppInstall.Name = "buttonBrowseAppInstall";
            this.buttonBrowseAppInstall.Size = new System.Drawing.Size(80, 29);
            this.buttonBrowseAppInstall.TabIndex = 2;
            this.buttonBrowseAppInstall.Text = "浏览";
            this.buttonBrowseAppInstall.UseVisualStyleBackColor = true;
            this.buttonBrowseAppInstall.Click += new System.EventHandler(this.buttonBrowseAppInstall_Click);
            // 
            // checkBoxCustomX86
            // 
            this.checkBoxCustomX86.AutoSize = true;
            this.checkBoxCustomX86.Location = new System.Drawing.Point(15, 95);
            this.checkBoxCustomX86.Name = "checkBoxCustomX86";
            this.checkBoxCustomX86.Size = new System.Drawing.Size(260, 24);
            this.checkBoxCustomX86.TabIndex = 3;
            this.checkBoxCustomX86.Text = "自定义 32位 程序路径";
            this.checkBoxCustomX86.UseVisualStyleBackColor = true;
            this.checkBoxCustomX86.CheckedChanged += new System.EventHandler(this.checkBoxCustomX86_CheckedChanged);
            // 
            // labelAppInstallX86
            // 
            this.labelAppInstallX86.AutoSize = true;
            this.labelAppInstallX86.Enabled = false;
            this.labelAppInstallX86.Location = new System.Drawing.Point(15, 125);
            this.labelAppInstallX86.Name = "labelAppInstallX86";
            this.labelAppInstallX86.Size = new System.Drawing.Size(250, 20);
            this.labelAppInstallX86.TabIndex = 4;
            this.labelAppInstallX86.Text = "Program Files 默认位置（32位）：";
            // 
            // textBoxAppInstallX86
            // 
            this.textBoxAppInstallX86.Enabled = false;
            this.textBoxAppInstallX86.Location = new System.Drawing.Point(15, 150);
            this.textBoxAppInstallX86.Name = "textBoxAppInstallX86";
            this.textBoxAppInstallX86.Size = new System.Drawing.Size(520, 27);
            this.textBoxAppInstallX86.TabIndex = 5;
            // 
            // buttonBrowseAppInstallX86
            // 
            this.buttonBrowseAppInstallX86.Enabled = false;
            this.buttonBrowseAppInstallX86.Location = new System.Drawing.Point(545, 149);
            this.buttonBrowseAppInstallX86.Name = "buttonBrowseAppInstallX86";
            this.buttonBrowseAppInstallX86.Size = new System.Drawing.Size(80, 29);
            this.buttonBrowseAppInstallX86.TabIndex = 6;
            this.buttonBrowseAppInstallX86.Text = "浏览";
            this.buttonBrowseAppInstallX86.UseVisualStyleBackColor = true;
            this.buttonBrowseAppInstallX86.Click += new System.EventHandler(this.buttonBrowseAppInstallX86_Click);
            // 
            // buttonSetAppInstall
            // 
            this.buttonSetAppInstall.Location = new System.Drawing.Point(635, 54);
            this.buttonSetAppInstall.Name = "buttonSetAppInstall";
            this.buttonSetAppInstall.Size = new System.Drawing.Size(80, 29);
            this.buttonSetAppInstall.TabIndex = 7;
            this.buttonSetAppInstall.Text = "应用";
            this.buttonSetAppInstall.UseVisualStyleBackColor = true;
            this.buttonSetAppInstall.Click += new System.EventHandler(this.buttonSetAppInstall_Click);
            // 
            // buttonReset
            // 
            this.buttonReset.Location = new System.Drawing.Point(725, 54);
            this.buttonReset.Name = "buttonReset";
            this.buttonReset.Size = new System.Drawing.Size(80, 29);
            this.buttonReset.TabIndex = 8;
            this.buttonReset.Text = "恢复默认";
            this.buttonReset.UseVisualStyleBackColor = true;
            this.buttonReset.Click += new System.EventHandler(this.buttonReset_Click);
            // 
            // groupBoxStoragePolicy
            // 
            this.groupBoxStoragePolicy.Controls.Add(this.labelStoragePolicyNote);
            this.groupBoxStoragePolicy.Location = new System.Drawing.Point(15, 280);
            this.groupBoxStoragePolicy.Name = "groupBoxStoragePolicy";
            this.groupBoxStoragePolicy.Size = new System.Drawing.Size(820, 100);
            this.groupBoxStoragePolicy.TabIndex = 1;
            this.groupBoxStoragePolicy.TabStop = false;
            this.groupBoxStoragePolicy.Text = "新内容保存位置（Microsoft Store 应用和用户内容）";
            // 
            // labelStoragePolicyNote
            // 
            this.labelStoragePolicyNote.AutoSize = false;
            this.labelStoragePolicyNote.ForeColor = System.Drawing.Color.DarkBlue;
            this.labelStoragePolicyNote.Location = new System.Drawing.Point(15, 30);
            this.labelStoragePolicyNote.Name = "labelStoragePolicyNote";
            this.labelStoragePolicyNote.Size = new System.Drawing.Size(790, 60);
            this.labelStoragePolicyNote.TabIndex = 0;
            this.labelStoragePolicyNote.Text = "ℹ 此设置影响 Microsoft Store 应用和新用户内容（文档、照片等）。需要 Windows 10 或更高版本。\r\n该功能即将推出...";
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(850, 400);
            this.Controls.Add(this.groupBoxStoragePolicy);
            this.Controls.Add(this.groupBoxProgramFiles);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "设置";
            this.groupBoxProgramFiles.ResumeLayout(false);
            this.groupBoxProgramFiles.PerformLayout();
            this.groupBoxStoragePolicy.ResumeLayout(false);
            this.ResumeLayout(false);
        }

    #endregion

        private System.Windows.Forms.Label labelAppInstall;
        private System.Windows.Forms.TextBox textBoxAppInstall;
        private System.Windows.Forms.Button buttonBrowseAppInstall;
        private System.Windows.Forms.Button buttonSetAppInstall;
        private System.Windows.Forms.Button buttonReset;
        private System.Windows.Forms.Label labelDescription;
        private System.Windows.Forms.Label labelWarning;
        private System.Windows.Forms.Label labelAppInstallX86;
        private System.Windows.Forms.TextBox textBoxAppInstallX86;
        private System.Windows.Forms.Button buttonBrowseAppInstallX86;
        private System.Windows.Forms.CheckBox checkBoxCustomX86;
        private System.Windows.Forms.GroupBox groupBoxProgramFiles;
        private System.Windows.Forms.Label labelProgramFilesNote;
        private System.Windows.Forms.GroupBox groupBoxStoragePolicy;
        private System.Windows.Forms.Label labelStoragePolicyNote;
    }
}