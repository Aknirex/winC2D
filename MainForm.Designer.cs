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
        private System.Windows.Forms.ToolStripMenuItem menuLog;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageSettings;
        private System.Windows.Forms.TabPage tabPageSoftware;
        private System.Windows.Forms.TabPage tabPageFolders;
        private System.Windows.Forms.TabPage tabPageAppData;
        
        // Settings tab controls
        private System.Windows.Forms.GroupBox groupBoxProgramFiles;
        private System.Windows.Forms.Label labelProgramFilesNote;
        private System.Windows.Forms.Button buttonResetProgramFiles;
        private System.Windows.Forms.Button buttonApplyProgramFiles;
        private System.Windows.Forms.Button buttonBrowseProgramFilesX86;
        private System.Windows.Forms.TextBox textBoxProgramFilesX86;
        private System.Windows.Forms.Label labelProgramFilesX86;
        private System.Windows.Forms.CheckBox checkBoxCustomX86;
        private System.Windows.Forms.Button buttonBrowseProgramFiles;
        private System.Windows.Forms.TextBox textBoxProgramFiles;
        private System.Windows.Forms.Label labelProgramFiles;
        private System.Windows.Forms.GroupBox groupBoxStoragePolicy;
        private System.Windows.Forms.Label labelStoragePolicyNote;
        private System.Windows.Forms.Button buttonOpenWindowsStorage;
        
        private System.Windows.Forms.ListView listViewAppData;
        private System.Windows.Forms.ColumnHeader columnHeaderAppName;
        private System.Windows.Forms.ColumnHeader columnHeaderAppPath;
        private System.Windows.Forms.ColumnHeader columnHeaderAppSize;
        private System.Windows.Forms.ColumnHeader columnHeaderAppType;
        private System.Windows.Forms.Button buttonMigrateAppData;
        private System.Windows.Forms.Button buttonRefreshAppData;
        private System.Windows.Forms.Label labelMklinkNote;
        private System.Windows.Forms.ToolStripMenuItem menuLanguage;
        private System.Windows.Forms.ToolStripMenuItem menuLanguageEnglish;
        private System.Windows.Forms.ToolStripMenuItem menuLanguageChinese;
        private System.Windows.Forms.ToolStripMenuItem menuLanguageJapanese;
        private System.Windows.Forms.ToolStripMenuItem menuLanguageKorean;
        private System.Windows.Forms.ToolStripMenuItem menuLanguageFrench;
        private System.Windows.Forms.ToolStripMenuItem menuLanguageGerman;
        private System.Windows.Forms.ToolStripMenuItem menuLanguageSpanish;
        private System.Windows.Forms.ToolStripMenuItem menuLanguageRussian;

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
            this.menuLog = new System.Windows.Forms.ToolStripMenuItem();
            this.menuLanguage = new System.Windows.Forms.ToolStripMenuItem();
            this.menuLanguageEnglish = new System.Windows.Forms.ToolStripMenuItem();
            this.menuLanguageChinese = new System.Windows.Forms.ToolStripMenuItem();
            this.menuLanguageJapanese = new System.Windows.Forms.ToolStripMenuItem();
            this.menuLanguageKorean = new System.Windows.Forms.ToolStripMenuItem();
            this.menuLanguageFrench = new System.Windows.Forms.ToolStripMenuItem();
            this.menuLanguageGerman = new System.Windows.Forms.ToolStripMenuItem();
            this.menuLanguageSpanish = new System.Windows.Forms.ToolStripMenuItem();
            this.menuLanguageRussian = new System.Windows.Forms.ToolStripMenuItem();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPageSettings = new System.Windows.Forms.TabPage();
            this.groupBoxProgramFiles = new System.Windows.Forms.GroupBox();
            this.labelProgramFilesNote = new System.Windows.Forms.Label();
            this.buttonResetProgramFiles = new System.Windows.Forms.Button();
            this.buttonApplyProgramFiles = new System.Windows.Forms.Button();
            this.buttonBrowseProgramFilesX86 = new System.Windows.Forms.Button();
            this.textBoxProgramFilesX86 = new System.Windows.Forms.TextBox();
            this.labelProgramFilesX86 = new System.Windows.Forms.Label();
            this.checkBoxCustomX86 = new System.Windows.Forms.CheckBox();
            this.buttonBrowseProgramFiles = new System.Windows.Forms.Button();
            this.textBoxProgramFiles = new System.Windows.Forms.TextBox();
            this.labelProgramFiles = new System.Windows.Forms.Label();
            this.groupBoxStoragePolicy = new System.Windows.Forms.GroupBox();
            this.labelStoragePolicyNote = new System.Windows.Forms.Label();
            this.buttonOpenWindowsStorage = new System.Windows.Forms.Button();
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
            this.tabPageAppData = new System.Windows.Forms.TabPage();
            this.listViewAppData = new System.Windows.Forms.ListView();
            this.columnHeaderAppName = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderAppPath = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderAppSize = new System.Windows.Forms.ColumnHeader();
            this.columnHeaderAppType = new System.Windows.Forms.ColumnHeader();
            this.buttonMigrateAppData = new System.Windows.Forms.Button();
            this.buttonRefreshAppData = new System.Windows.Forms.Button();
            this.labelMklinkNote = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPageSettings.SuspendLayout();
            this.groupBoxProgramFiles.SuspendLayout();
            this.groupBoxStoragePolicy.SuspendLayout();
            this.tabPageSoftware.SuspendLayout();
            this.tabPageFolders.SuspendLayout();
            this.tabPageAppData.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuLog, this.menuLanguage});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 28);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // menuLog
            // 
            this.menuLog.Name = "menuLog";
            this.menuLog.Size = new System.Drawing.Size(83, 24);
            this.menuLog.Text = "迁移日志";
            this.menuLog.Click += new System.EventHandler(this.menuLog_Click);
            // 
            // menuLanguage
            // 
            this.menuLanguage.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuLanguageEnglish,
            this.menuLanguageChinese,
            this.menuLanguageJapanese,
            this.menuLanguageKorean,
            this.menuLanguageFrench,
            this.menuLanguageGerman,
            this.menuLanguageSpanish,
            this.menuLanguageRussian});
            this.menuLanguage.Name = "menuLanguage";
            this.menuLanguage.Size = new System.Drawing.Size(83, 24);
            this.menuLanguage.Text = "语言";
            // 
            // menuLanguageEnglish
            // 
            this.menuLanguageEnglish.Name = "menuLanguageEnglish";
            this.menuLanguageEnglish.Size = new System.Drawing.Size(180, 26);
            this.menuLanguageEnglish.Text = "English";
            this.menuLanguageEnglish.Click += new System.EventHandler(this.menuLanguageEnglish_Click);
            // 
            // menuLanguageChinese
            // 
            this.menuLanguageChinese.Name = "menuLanguageChinese";
            this.menuLanguageChinese.Size = new System.Drawing.Size(180, 26);
            this.menuLanguageChinese.Text = "中文";
            this.menuLanguageChinese.Click += new System.EventHandler(this.menuLanguageChinese_Click);
            // 
            // menuLanguageJapanese
            // 
            this.menuLanguageJapanese.Name = "menuLanguageJapanese";
            this.menuLanguageJapanese.Size = new System.Drawing.Size(180, 26);
            this.menuLanguageJapanese.Text = "日本語";
            this.menuLanguageJapanese.Click += new System.EventHandler(this.menuLanguageJapanese_Click);
            // 
            // menuLanguageKorean
            // 
            this.menuLanguageKorean.Name = "menuLanguageKorean";
            this.menuLanguageKorean.Size = new System.Drawing.Size(180, 26);
            this.menuLanguageKorean.Text = "한국어";
            this.menuLanguageKorean.Click += new System.EventHandler(this.menuLanguageKorean_Click);
            // 
            // menuLanguageFrench
            // 
            this.menuLanguageFrench.Name = "menuLanguageFrench";
            this.menuLanguageFrench.Size = new System.Drawing.Size(180, 26);
            this.menuLanguageFrench.Text = "Français";
            this.menuLanguageFrench.Click += new System.EventHandler(this.menuLanguageFrench_Click);
            // 
            // menuLanguageGerman
            // 
            this.menuLanguageGerman.Name = "menuLanguageGerman";
            this.menuLanguageGerman.Size = new System.Drawing.Size(180, 26);
            this.menuLanguageGerman.Text = "Deutsch";
            this.menuLanguageGerman.Click += new System.EventHandler(this.menuLanguageGerman_Click);
            // 
            // menuLanguageSpanish
            // 
            this.menuLanguageSpanish.Name = "menuLanguageSpanish";
            this.menuLanguageSpanish.Size = new System.Drawing.Size(180, 26);
            this.menuLanguageSpanish.Text = "Español";
            this.menuLanguageSpanish.Click += new System.EventHandler(this.menuLanguageSpanish_Click);
            // 
            // menuLanguageRussian
            // 
            this.menuLanguageRussian.Name = "menuLanguageRussian";
            this.menuLanguageRussian.Size = new System.Drawing.Size(180, 26);
            this.menuLanguageRussian.Text = "Русский";
            this.menuLanguageRussian.Click += new System.EventHandler(this.menuLanguageRussian_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabControl1.Controls.Add(this.tabPageSettings);
            this.tabControl1.Controls.Add(this.tabPageSoftware);
            this.tabControl1.Controls.Add(this.tabPageFolders);
            this.tabControl1.Controls.Add(this.tabPageAppData);
            this.tabControl1.Location = new System.Drawing.Point(12, 35);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(776, 494);
            this.tabControl1.TabIndex = 1;
            // 
            // tabPageSettings
            // 
            this.tabPageSettings.Controls.Add(this.groupBoxStoragePolicy);
            this.tabPageSettings.Controls.Add(this.groupBoxProgramFiles);
            this.tabPageSettings.Location = new System.Drawing.Point(4, 29);
            this.tabPageSettings.Name = "tabPageSettings";
            this.tabPageSettings.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSettings.Size = new System.Drawing.Size(768, 461);
            this.tabPageSettings.TabIndex = 0;
            this.tabPageSettings.Text = "系统设置";
            this.tabPageSettings.UseVisualStyleBackColor = true;
            // 
            // groupBoxProgramFiles
            // 
            this.groupBoxProgramFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxProgramFiles.Controls.Add(this.labelProgramFilesNote);
            this.groupBoxProgramFiles.Controls.Add(this.buttonResetProgramFiles);
            this.groupBoxProgramFiles.Controls.Add(this.buttonApplyProgramFiles);
            this.groupBoxProgramFiles.Controls.Add(this.buttonBrowseProgramFilesX86);
            this.groupBoxProgramFiles.Controls.Add(this.textBoxProgramFilesX86);
            this.groupBoxProgramFiles.Controls.Add(this.labelProgramFilesX86);
            this.groupBoxProgramFiles.Controls.Add(this.checkBoxCustomX86);
            this.groupBoxProgramFiles.Controls.Add(this.buttonBrowseProgramFiles);
            this.groupBoxProgramFiles.Controls.Add(this.textBoxProgramFiles);
            this.groupBoxProgramFiles.Controls.Add(this.labelProgramFiles);
            this.groupBoxProgramFiles.Location = new System.Drawing.Point(15, 15);
            this.groupBoxProgramFiles.Name = "groupBoxProgramFiles";
            this.groupBoxProgramFiles.Size = new System.Drawing.Size(735, 250);
            this.groupBoxProgramFiles.TabIndex = 0;
            this.groupBoxProgramFiles.TabStop = false;
            this.groupBoxProgramFiles.Text = "Program Files 位置（传统桌面程序）";
            // 
            // labelProgramFiles
            // 
            this.labelProgramFiles.AutoSize = true;
            this.labelProgramFiles.Location = new System.Drawing.Point(15, 30);
            this.labelProgramFiles.Name = "labelProgramFiles";
            this.labelProgramFiles.Size = new System.Drawing.Size(250, 20);
            this.labelProgramFiles.TabIndex = 0;
            this.labelProgramFiles.Text = "Program Files 默认位置（64位）：";
            // 
            // textBoxProgramFiles
            // 
            this.textBoxProgramFiles.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxProgramFiles.Location = new System.Drawing.Point(15, 55);
            this.textBoxProgramFiles.Name = "textBoxProgramFiles";
            this.textBoxProgramFiles.Size = new System.Drawing.Size(435, 27);
            this.textBoxProgramFiles.TabIndex = 1;
            this.textBoxProgramFiles.TextChanged += new System.EventHandler(this.textBoxProgramFiles_TextChanged);
            // 
            // buttonBrowseProgramFiles
            // 
            this.buttonBrowseProgramFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowseProgramFiles.Location = new System.Drawing.Point(460, 54);
            this.buttonBrowseProgramFiles.Name = "buttonBrowseProgramFiles";
            this.buttonBrowseProgramFiles.Size = new System.Drawing.Size(80, 29);
            this.buttonBrowseProgramFiles.TabIndex = 2;
            this.buttonBrowseProgramFiles.Text = "浏览";
            this.buttonBrowseProgramFiles.UseVisualStyleBackColor = true;
            this.buttonBrowseProgramFiles.Click += new System.EventHandler(this.buttonBrowseProgramFiles_Click);
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
            // labelProgramFilesX86
            // 
            this.labelProgramFilesX86.AutoSize = true;
            this.labelProgramFilesX86.Enabled = false;
            this.labelProgramFilesX86.Location = new System.Drawing.Point(15, 125);
            this.labelProgramFilesX86.Name = "labelProgramFilesX86";
            this.labelProgramFilesX86.Size = new System.Drawing.Size(250, 20);
            this.labelProgramFilesX86.TabIndex = 4;
            this.labelProgramFilesX86.Text = "Program Files 默认位置（32位）：";
            // 
            // textBoxProgramFilesX86
            // 
            this.textBoxProgramFilesX86.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxProgramFilesX86.Enabled = false;
            this.textBoxProgramFilesX86.Location = new System.Drawing.Point(15, 150);
            this.textBoxProgramFilesX86.Name = "textBoxProgramFilesX86";
            this.textBoxProgramFilesX86.Size = new System.Drawing.Size(435, 27);
            this.textBoxProgramFilesX86.TabIndex = 5;
            // 
            // buttonBrowseProgramFilesX86
            // 
            this.buttonBrowseProgramFilesX86.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonBrowseProgramFilesX86.Enabled = false;
            this.buttonBrowseProgramFilesX86.Location = new System.Drawing.Point(460, 149);
            this.buttonBrowseProgramFilesX86.Name = "buttonBrowseProgramFilesX86";
            this.buttonBrowseProgramFilesX86.Size = new System.Drawing.Size(80, 29);
            this.buttonBrowseProgramFilesX86.TabIndex = 6;
            this.buttonBrowseProgramFilesX86.Text = "浏览";
            this.buttonBrowseProgramFilesX86.UseVisualStyleBackColor = true;
            this.buttonBrowseProgramFilesX86.Click += new System.EventHandler(this.buttonBrowseProgramFilesX86_Click);
            // 
            // buttonApplyProgramFiles
            // 
            this.buttonApplyProgramFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonApplyProgramFiles.Location = new System.Drawing.Point(550, 54);
            this.buttonApplyProgramFiles.Name = "buttonApplyProgramFiles";
            this.buttonApplyProgramFiles.Size = new System.Drawing.Size(80, 29);
            this.buttonApplyProgramFiles.TabIndex = 7;
            this.buttonApplyProgramFiles.Text = "应用";
            this.buttonApplyProgramFiles.UseVisualStyleBackColor = true;
            this.buttonApplyProgramFiles.Click += new System.EventHandler(this.buttonApplyProgramFiles_Click);
            // 
            // buttonResetProgramFiles
            // 
            this.buttonResetProgramFiles.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonResetProgramFiles.Location = new System.Drawing.Point(640, 54);
            this.buttonResetProgramFiles.Name = "buttonResetProgramFiles";
            this.buttonResetProgramFiles.Size = new System.Drawing.Size(80, 29);
            this.buttonResetProgramFiles.TabIndex = 8;
            this.buttonResetProgramFiles.Text = "恢复默认";
            this.buttonResetProgramFiles.UseVisualStyleBackColor = true;
            this.buttonResetProgramFiles.Click += new System.EventHandler(this.buttonResetProgramFiles_Click);
            // 
            // labelProgramFilesNote
            // 
            this.labelProgramFilesNote.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelProgramFilesNote.AutoSize = false;
            this.labelProgramFilesNote.ForeColor = System.Drawing.Color.DarkBlue;
            this.labelProgramFilesNote.Location = new System.Drawing.Point(15, 185);
            this.labelProgramFilesNote.Name = "labelProgramFilesNote";
            this.labelProgramFilesNote.Size = new System.Drawing.Size(705, 50);
            this.labelProgramFilesNote.TabIndex = 9;
            this.labelProgramFilesNote.Text = "ℹ 此设置影响通过安装程序（.exe、.msi）安装的传统桌面程序。大多数第三方软件使用此位置。";
            // 
            // groupBoxStoragePolicy
            // 
            this.groupBoxStoragePolicy.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxStoragePolicy.Controls.Add(this.buttonOpenWindowsStorage);
            this.groupBoxStoragePolicy.Controls.Add(this.labelStoragePolicyNote);
            this.groupBoxStoragePolicy.Location = new System.Drawing.Point(15, 280);
            this.groupBoxStoragePolicy.Name = "groupBoxStoragePolicy";
            this.groupBoxStoragePolicy.Size = new System.Drawing.Size(735, 100);
            this.groupBoxStoragePolicy.TabIndex = 1;
            this.groupBoxStoragePolicy.TabStop = false;
            this.groupBoxStoragePolicy.Text = "新内容保存位置（Microsoft Store 应用和用户内容）";
            // 
            // labelStoragePolicyNote
            // 
            this.labelStoragePolicyNote.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelStoragePolicyNote.AutoSize = false;
            this.labelStoragePolicyNote.ForeColor = System.Drawing.Color.DarkBlue;
            this.labelStoragePolicyNote.Location = new System.Drawing.Point(15, 30);
            this.labelStoragePolicyNote.Name = "labelStoragePolicyNote";
            this.labelStoragePolicyNote.Size = new System.Drawing.Size(515, 60);
            this.labelStoragePolicyNote.TabIndex = 0;
            this.labelStoragePolicyNote.Text = "ℹ 此设置影响 Microsoft Store 应用和新用户内容（文档、照片等）。需要 Windows 10 或更高版本。";
            // 
            // buttonOpenWindowsStorage
            // 
            this.buttonOpenWindowsStorage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOpenWindowsStorage.Location = new System.Drawing.Point(550, 35);
            this.buttonOpenWindowsStorage.Name = "buttonOpenWindowsStorage";
            this.buttonOpenWindowsStorage.Size = new System.Drawing.Size(170, 50);
            this.buttonOpenWindowsStorage.TabIndex = 1;
            this.buttonOpenWindowsStorage.Text = "打开 Windows 设置";
            this.buttonOpenWindowsStorage.UseVisualStyleBackColor = true;
            this.buttonOpenWindowsStorage.Click += new System.EventHandler(this.buttonOpenWindowsStorage_Click);
            // 
            // tabPageSoftware
            // 
            this.tabPageSoftware.Controls.Add(this.listViewSoftware);
            this.tabPageSoftware.Controls.Add(this.buttonMigrateSoftware);
            this.tabPageSoftware.Location = new System.Drawing.Point(4, 29);
            this.tabPageSoftware.Name = "tabPageSoftware";
            this.tabPageSoftware.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageSoftware.Size = new System.Drawing.Size(768, 461);
            this.tabPageSoftware.TabIndex = 1;
            this.tabPageSoftware.Text = "软件迁移";
            this.tabPageSoftware.UseVisualStyleBackColor = true;
            // 
            // listViewSoftware
            // 
            this.listViewSoftware.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
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
            this.buttonMigrateSoftware.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
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
            this.tabPageFolders.Size = new System.Drawing.Size(768, 461);
            this.tabPageFolders.TabIndex = 2;
            this.tabPageFolders.Text = "用户文件夹";
            this.tabPageFolders.UseVisualStyleBackColor = true;
            // 
            // listViewFolders
            // 
            this.listViewFolders.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
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
            this.buttonMigrateFolders.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonMigrateFolders.Location = new System.Drawing.Point(650, 320);
            this.buttonMigrateFolders.Name = "buttonMigrateFolders";
            this.buttonMigrateFolders.Size = new System.Drawing.Size(112, 32);
            this.buttonMigrateFolders.TabIndex = 1;
            this.buttonMigrateFolders.Text = "迁移所选";
            this.buttonMigrateFolders.UseVisualStyleBackColor = true;
            this.buttonMigrateFolders.Click += new System.EventHandler(this.buttonMigrateFolders_Click);
            // 
            // tabPageAppData
            // 
            this.tabPageAppData.Controls.Add(this.labelMklinkNote);
            this.tabPageAppData.Controls.Add(this.listViewAppData);
            this.tabPageAppData.Controls.Add(this.buttonMigrateAppData);
            this.tabPageAppData.Controls.Add(this.buttonRefreshAppData);
            this.tabPageAppData.Location = new System.Drawing.Point(4, 29);
            this.tabPageAppData.Name = "tabPageAppData";
            this.tabPageAppData.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageAppData.Size = new System.Drawing.Size(768, 461);
            this.tabPageAppData.TabIndex = 3;
            this.tabPageAppData.Text = "AppData (mklink)";
            this.tabPageAppData.UseVisualStyleBackColor = true;
            // 
            // listViewAppData
            // 
            this.listViewAppData.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewAppData.CheckBoxes = true;
            this.listViewAppData.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeaderAppName,
            this.columnHeaderAppPath,
            this.columnHeaderAppSize,
            this.columnHeaderAppType});
            this.listViewAppData.FullRowSelect = true;
            this.listViewAppData.Location = new System.Drawing.Point(6, 35);
            this.listViewAppData.Name = "listViewAppData";
            this.listViewAppData.Size = new System.Drawing.Size(756, 270);
            this.listViewAppData.TabIndex = 0;
            this.listViewAppData.UseCompatibleStateImageBehavior = false;
            this.listViewAppData.View = System.Windows.Forms.View.Details;
            // 
            // columnHeaderAppName
            // 
            this.columnHeaderAppName.Text = "应用名称";
            this.columnHeaderAppName.Width = 200;
            // 
            // columnHeaderAppPath
            // 
            this.columnHeaderAppPath.Text = "路径";
            this.columnHeaderAppPath.Width = 350;
            // 
            // columnHeaderAppSize
            // 
            this.columnHeaderAppSize.Text = "大小";
            this.columnHeaderAppSize.Width = 100;
            // 
            // columnHeaderAppType
            // 
            this.columnHeaderAppType.Text = "类型";
            this.columnHeaderAppType.Width = 80;
            // 
            // buttonMigrateAppData
            // 
            this.buttonMigrateAppData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonMigrateAppData.Location = new System.Drawing.Point(650, 320);
            this.buttonMigrateAppData.Name = "buttonMigrateAppData";
            this.buttonMigrateAppData.Size = new System.Drawing.Size(112, 32);
            this.buttonMigrateAppData.TabIndex = 1;
            this.buttonMigrateAppData.Text = "迁移所选";
            this.buttonMigrateAppData.UseVisualStyleBackColor = true;
            this.buttonMigrateAppData.Click += new System.EventHandler(this.buttonMigrateAppData_Click);
            // 
            // buttonRefreshAppData
            // 
            this.buttonRefreshAppData.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRefreshAppData.Location = new System.Drawing.Point(532, 320);
            this.buttonRefreshAppData.Name = "buttonRefreshAppData";
            this.buttonRefreshAppData.Size = new System.Drawing.Size(112, 32);
            this.buttonRefreshAppData.TabIndex = 2;
            this.buttonRefreshAppData.Text = "刷新列表";
            this.buttonRefreshAppData.UseVisualStyleBackColor = true;
            this.buttonRefreshAppData.Click += new System.EventHandler(this.buttonRefreshAppData_Click);
            // 
            // labelMklinkNote
            // 
            this.labelMklinkNote.AutoSize = true;
            this.labelMklinkNote.ForeColor = System.Drawing.Color.DarkBlue;
            this.labelMklinkNote.Location = new System.Drawing.Point(6, 10);
            this.labelMklinkNote.Name = "labelMklinkNote";
            this.labelMklinkNote.Size = new System.Drawing.Size(500, 20);
            this.labelMklinkNote.TabIndex = 3;
            this.labelMklinkNote.Text = "注意：此操作使用 mklink 创建符号链接，需要管理员权限。";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 541);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.MinimumSize = new System.Drawing.Size(600, 450);
            this.Name = "MainForm";
            this.Text = "Windows存储迁移助手";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPageSettings.ResumeLayout(false);
            this.groupBoxProgramFiles.ResumeLayout(false);
            this.groupBoxProgramFiles.PerformLayout();
            this.groupBoxStoragePolicy.ResumeLayout(false);
            this.tabPageSoftware.ResumeLayout(false);
            this.tabPageFolders.ResumeLayout(false);
            this.tabPageAppData.ResumeLayout(false);
            this.tabPageAppData.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion
    }
}
