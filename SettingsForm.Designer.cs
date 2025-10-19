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
            this.labelAppInstall = new System.Windows.Forms.Label();
            this.textBoxAppInstall = new System.Windows.Forms.TextBox();
            this.buttonBrowseAppInstall = new System.Windows.Forms.Button();
            this.buttonSetAppInstall = new System.Windows.Forms.Button();
            this.buttonReset = new System.Windows.Forms.Button();
            this.labelDescription = new System.Windows.Forms.Label();
            this.labelWarning = new System.Windows.Forms.Label();
            this.labelAppInstallX86 = new System.Windows.Forms.Label();
            this.textBoxAppInstallX86 = new System.Windows.Forms.TextBox();
            this.buttonBrowseAppInstallX86 = new System.Windows.Forms.Button();
            this.checkBoxCustomX86 = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // labelAppInstall
            // 
            this.labelAppInstall.AutoSize = true;
            this.labelAppInstall.Location = new System.Drawing.Point(20, 20);
            this.labelAppInstall.Name = "labelAppInstall";
            this.labelAppInstall.Size = new System.Drawing.Size(250, 20);
            this.labelAppInstall.TabIndex = 0;
            this.labelAppInstall.Text = "Program Files 默认位置（64位）：";
            // 
            // textBoxAppInstall
            // 
            this.textBoxAppInstall.Location = new System.Drawing.Point(20, 45);
            this.textBoxAppInstall.Name = "textBoxAppInstall";
            this.textBoxAppInstall.Size = new System.Drawing.Size(520, 27);
            this.textBoxAppInstall.TabIndex = 1;
            this.textBoxAppInstall.TextChanged += new System.EventHandler(this.textBoxAppInstall_TextChanged);
            // 
            // buttonBrowseAppInstall
            // 
            this.buttonBrowseAppInstall.Location = new System.Drawing.Point(550, 44);
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
            this.checkBoxCustomX86.Location = new System.Drawing.Point(20, 85);
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
            this.labelAppInstallX86.Location = new System.Drawing.Point(20, 115);
            this.labelAppInstallX86.Name = "labelAppInstallX86";
            this.labelAppInstallX86.Size = new System.Drawing.Size(250, 20);
            this.labelAppInstallX86.TabIndex = 4;
            this.labelAppInstallX86.Text = "Program Files 默认位置（32位）：";
            // 
            // textBoxAppInstallX86
            // 
            this.textBoxAppInstallX86.Enabled = false;
            this.textBoxAppInstallX86.Location = new System.Drawing.Point(20, 140);
            this.textBoxAppInstallX86.Name = "textBoxAppInstallX86";
            this.textBoxAppInstallX86.Size = new System.Drawing.Size(520, 27);
            this.textBoxAppInstallX86.TabIndex = 5;
            // 
            // buttonBrowseAppInstallX86
            // 
            this.buttonBrowseAppInstallX86.Enabled = false;
            this.buttonBrowseAppInstallX86.Location = new System.Drawing.Point(550, 139);
            this.buttonBrowseAppInstallX86.Name = "buttonBrowseAppInstallX86";
            this.buttonBrowseAppInstallX86.Size = new System.Drawing.Size(80, 29);
            this.buttonBrowseAppInstallX86.TabIndex = 6;
            this.buttonBrowseAppInstallX86.Text = "浏览";
            this.buttonBrowseAppInstallX86.UseVisualStyleBackColor = true;
            this.buttonBrowseAppInstallX86.Click += new System.EventHandler(this.buttonBrowseAppInstallX86_Click);
            // 
            // buttonSetAppInstall
            // 
            this.buttonSetAppInstall.Location = new System.Drawing.Point(640, 44);
            this.buttonSetAppInstall.Name = "buttonSetAppInstall";
            this.buttonSetAppInstall.Size = new System.Drawing.Size(80, 29);
            this.buttonSetAppInstall.TabIndex = 7;
            this.buttonSetAppInstall.Text = "应用";
            this.buttonSetAppInstall.UseVisualStyleBackColor = true;
            this.buttonSetAppInstall.Click += new System.EventHandler(this.buttonSetAppInstall_Click);
            // 
            // buttonReset
            // 
            this.buttonReset.Location = new System.Drawing.Point(730, 44);
            this.buttonReset.Name = "buttonReset";
            this.buttonReset.Size = new System.Drawing.Size(100, 29);
            this.buttonReset.TabIndex = 8;
            this.buttonReset.Text = "恢复默认";
            this.buttonReset.UseVisualStyleBackColor = true;
            this.buttonReset.Click += new System.EventHandler(this.buttonReset_Click);
            // 
            // labelDescription
            // 
            this.labelDescription.AutoSize = false;
            this.labelDescription.Location = new System.Drawing.Point(20, 185);
            this.labelDescription.Name = "labelDescription";
            this.labelDescription.Size = new System.Drawing.Size(810, 40);
            this.labelDescription.TabIndex = 9;
            this.labelDescription.Text = "此设置会修改 Windows 系统的 Program Files 默认路径，影响新安装软件的默认安装位置。";
            // 
            // labelWarning
            // 
            this.labelWarning.AutoSize = false;
            this.labelWarning.ForeColor = System.Drawing.Color.DarkBlue;
            this.labelWarning.Location = new System.Drawing.Point(20, 225);
            this.labelWarning.Name = "labelWarning";
            this.labelWarning.Size = new System.Drawing.Size(810, 60);
            this.labelWarning.TabIndex = 10;
            this.labelWarning.Text = "ℹ 注意：此更改仅影响新安装的软件，已安装的软件不受影响。某些安装程序可能需要重启系统才能识别此更改。";
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(850, 300);
            this.Controls.Add(this.labelWarning);
            this.Controls.Add(this.labelDescription);
            this.Controls.Add(this.buttonReset);
            this.Controls.Add(this.buttonSetAppInstall);
            this.Controls.Add(this.buttonBrowseAppInstallX86);
            this.Controls.Add(this.textBoxAppInstallX86);
            this.Controls.Add(this.labelAppInstallX86);
            this.Controls.Add(this.checkBoxCustomX86);
            this.Controls.Add(this.buttonBrowseAppInstall);
            this.Controls.Add(this.textBoxAppInstall);
            this.Controls.Add(this.labelAppInstall);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "设置";
            this.ResumeLayout(false);
            this.PerformLayout();
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
    }
}