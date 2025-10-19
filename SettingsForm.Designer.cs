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
            this.SuspendLayout();
            // 
            // labelAppInstall
            // 
            this.labelAppInstall.AutoSize = true;
            this.labelAppInstall.Location = new System.Drawing.Point(30, 30);
            this.labelAppInstall.Name = "labelAppInstall";
            this.labelAppInstall.Size = new System.Drawing.Size(180, 20);
            this.labelAppInstall.TabIndex = 0;
            this.labelAppInstall.Text = "新应用默认安装位置：";
            // 
            // textBoxAppInstall
            // 
            this.textBoxAppInstall.Location = new System.Drawing.Point(220, 27);
            this.textBoxAppInstall.Name = "textBoxAppInstall";
            this.textBoxAppInstall.Size = new System.Drawing.Size(250, 27);
            this.textBoxAppInstall.TabIndex = 1;
            // 
            // buttonBrowseAppInstall
            // 
            this.buttonBrowseAppInstall.Location = new System.Drawing.Point(480, 26);
            this.buttonBrowseAppInstall.Name = "buttonBrowseAppInstall";
            this.buttonBrowseAppInstall.Size = new System.Drawing.Size(60, 29);
            this.buttonBrowseAppInstall.TabIndex = 2;
            this.buttonBrowseAppInstall.Text = "浏览";
            this.buttonBrowseAppInstall.UseVisualStyleBackColor = true;
            this.buttonBrowseAppInstall.Click += new System.EventHandler(this.buttonBrowseAppInstall_Click);
            // 
            // buttonSetAppInstall
            // 
            this.buttonSetAppInstall.Location = new System.Drawing.Point(550, 26);
            this.buttonSetAppInstall.Name = "buttonSetAppInstall";
            this.buttonSetAppInstall.Size = new System.Drawing.Size(80, 29);
            this.buttonSetAppInstall.TabIndex = 3;
            this.buttonSetAppInstall.Text = "应用";
            this.buttonSetAppInstall.UseVisualStyleBackColor = true;
            this.buttonSetAppInstall.Click += new System.EventHandler(this.buttonSetAppInstall_Click);
            // 
            // SettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(660, 120);
            this.Controls.Add(this.labelAppInstall);
            this.Controls.Add(this.textBoxAppInstall);
            this.Controls.Add(this.buttonBrowseAppInstall);
            this.Controls.Add(this.buttonSetAppInstall);
            this.Name = "SettingsForm";
            this.Text = "系统设置";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

    #endregion

        private System.Windows.Forms.Label labelAppInstall;
        private System.Windows.Forms.TextBox textBoxAppInstall;
        private System.Windows.Forms.Button buttonBrowseAppInstall;
        private System.Windows.Forms.Button buttonSetAppInstall;
    }
}