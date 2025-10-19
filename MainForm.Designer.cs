using System.Windows.Forms;

namespace winC2D
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.ListView listViewFolders;
        private System.Windows.Forms.ColumnHeader columnHeaderFolderName;
        private System.Windows.Forms.ColumnHeader columnHeaderFolderPath;
        private System.Windows.Forms.Button buttonMigrateFolders;
        private System.Windows.Forms.ListView listViewSoftware;
        private System.Windows.Forms.ColumnHeader columnHeaderName;
        private System.Windows.Forms.ColumnHeader columnHeaderPath;
        private System.Windows.Forms.ColumnHeader columnHeaderSize;
        private System.Windows.Forms.Button buttonMigrateSoftware;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuSettings;
        private System.Windows.Forms.ToolStripMenuItem menuLog;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageSoftware;
        private System.Windows.Forms.TabPage tabPageFolders;

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

        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.menuSettings = new System.Windows.Forms.ToolStripMenuItem();
            this.menuLog = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageSoftware = new System.Windows.Forms.TabPage();
            this.listViewSoftware = new System.Windows.Forms.ListView();
            this.columnHeaderName = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderPath = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderSize = new System.Windows.Forms.ColumnHeader();
            this.buttonMigrateSoftware = new System.Windows.Forms.Button();
            this.tabPageFolders = new System.Windows.Forms.TabPage();
            this.listViewFolders = new System.Windows.Forms.ListView();
            this.columnHeaderFolderName = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderFolderPath = new System.Windows.Forms.ColumnHeader();
            this.buttonMigrateFolders = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageSoftware.SuspendLayout();
            this.tabPageFolders.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuSettings, this.menuLog});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 28);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // menuSettings
            // 
            this.menuSettings.Name = "menuSettings";
            this.menuSettings.Size = new System.Drawing.Size(83, 24);
            this.menuSettings.Text = "系统设置";
            this.menuSettings.Click += new System.EventHandler(this.menuSettings_Click);
            // 
            // menuLog
            // 
            this.menuLog.Name = "menuLog";
            this.menuLog.Size = new System.Drawing.Size(83, 24);
            this.menuLog.Text = "迁移日志";
            this.menuLog.Click += new System.EventHandler(this.menuLog_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPageSoftware);
            this.tabControl1.Controls.Add(this.tabPageFolders);
            this.tabControl1.Location = new System.Drawing.Point(12, 40);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(776, 398);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPageSoftware
            // 
            this.tabPageSoftware.Controls.Add(this.listViewSoftware);
            this.tabPageSoftware.Controls.Add(this.buttonMigrateSoftware);
            this.tabPageSoftware.Location = new System.Drawing.Point(4, 29);
            this.tabPageSoftware.Name = "tabPageSoftware";
            this.tabPageSoftware.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSoftware.Size = new System.Drawing.Size(768, 365);
            this.tabPageSoftware.TabIndex = 0;
            this.tabPageSoftware.Text = "软件迁移";
            this.tabPageSoftware.UseVisualStyleBackColor = true;
            // 
            // listViewSoftware
            // 
            this.listViewSoftware.CheckBoxes = true;
            this.listViewSoftware.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderName,
            this.columnHeaderPath,
            this.columnHeaderSize});
            this.listViewSoftware.FullRowSelect = true;
            this.listViewSoftware.Location = new System.Drawing.Point(6, 6);
            this.listViewSoftware.Name = "listViewSoftware";
            this.listViewSoftware.Size = new System.Drawing.Size(756, 300);
            this.listViewSoftware.TabIndex = 0;
            this.listViewSoftware.UseCompatibleStateImageBehavior = false;
            this.listViewSoftware.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderName
            // 
            this.columnHeaderName.Text = "软件名称";
            this.columnHeaderName.Width = 180;
            // 
            // columnHeaderPath
            // 
            this.columnHeaderPath.Text = "安装路径";
            this.columnHeaderPath.Width = 400;
            // 
            // columnHeaderSize
            // 
            this.columnHeaderSize.Text = "大小";
            this.columnHeaderSize.Width = 120;
            // 
            // buttonMigrateSoftware
            // 
            this.buttonMigrateSoftware.Location = new System.Drawing.Point(650, 320);
            this.buttonMigrateSoftware.Name = "buttonMigrateSoftware";
            this.buttonMigrateSoftware.Size = new System.Drawing.Size(112, 32);
            this.buttonMigrateSoftware.TabIndex = 1;
            this.buttonMigrateSoftware.Text = "迁移所选";
            this.buttonMigrateSoftware.UseVisualStyleBackColor = true;
            this.buttonMigrateSoftware.Click += new System.EventHandler(this.buttonMigrateSoftware_Click);
            // 
            // tabPageFolders
            // 
            this.tabPageFolders.Controls.Add(this.listViewFolders);
            this.tabPageFolders.Controls.Add(this.buttonMigrateFolders);
            this.tabPageFolders.Location = new System.Drawing.Point(4, 29);
            this.tabPageFolders.Name = "tabPageFolders";
            this.tabPageFolders.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageFolders.Size = new System.Drawing.Size(768, 365);
            this.tabPageFolders.TabIndex = 1;
            this.tabPageFolders.Text = "用户文件夹";
            this.tabPageFolders.UseVisualStyleBackColor = true;
            // 
            // listViewFolders
            // 
            this.listViewFolders.CheckBoxes = true;
            this.listViewFolders.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderFolderName,
            this.columnHeaderFolderPath});
            this.listViewFolders.FullRowSelect = true;
            this.listViewFolders.Location = new System.Drawing.Point(6, 6);
            this.listViewFolders.Name = "listViewFolders";
            this.listViewFolders.Size = new System.Drawing.Size(756, 300);
            this.listViewFolders.TabIndex = 0;
            this.listViewFolders.UseCompatibleStateImageBehavior = false;
            this.listViewFolders.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderFolderName
            // 
            this.columnHeaderFolderName.Text = "文件夹名称";
            this.columnHeaderFolderName.Width = 180;
            // 
            // columnHeaderFolderPath
            // 
            this.columnHeaderFolderPath.Text = "当前路径";
            this.columnHeaderFolderPath.Width = 520;
            // 
            // buttonMigrateFolders
            // 
            this.buttonMigrateFolders.Location = new System.Drawing.Point(650, 320);
            this.buttonMigrateFolders.Name = "buttonMigrateFolders";
            this.buttonMigrateFolders.Size = new System.Drawing.Size(112, 32);
            this.buttonMigrateFolders.TabIndex = 1;
            this.buttonMigrateFolders.Text = "迁移所选";
            this.buttonMigrateFolders.UseVisualStyleBackColor = true;
            this.buttonMigrateFolders.Click += new System.EventHandler(this.buttonMigrateFolders_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.Text = "Windows存储迁移助手";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPageSoftware.ResumeLayout(false);
            this.tabPageFolders.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
