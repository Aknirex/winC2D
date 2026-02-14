using System.Drawing;
using System.Windows.Forms;

namespace winC2D
{
    partial class MainForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        private winC2D.ThemedListView listViewSoftware;
        private System.Windows.Forms.ColumnHeader columnHeaderName;
        private System.Windows.Forms.ColumnHeader columnHeaderPath;
        private System.Windows.Forms.ColumnHeader columnHeaderSize;
        private System.Windows.Forms.ColumnHeader columnHeaderStatus; // 鏂板鐘舵�佸垪
        private winC2D.ModernButton buttonMigrateSoftware;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem menuLog;
        private winC2D.ModernTabControl tabControl1;
        private System.Windows.Forms.TabPage tabPageSettings;
        private System.Windows.Forms.TabPage tabPageSoftware;
        private System.Windows.Forms.TabPage tabPageAppData;
        
        // Settings tab controls
        private System.Windows.Forms.GroupBox groupBoxProgramFiles;
        private System.Windows.Forms.Label labelProgramFilesNote;
        private winC2D.ModernButton buttonResetProgramFiles;
        private winC2D.ModernButton buttonApplyProgramFiles;
        private winC2D.ModernButton buttonBrowseProgramFilesX86;
        private System.Windows.Forms.TextBox textBoxProgramFilesX86;
        private System.Windows.Forms.Label labelProgramFilesX86;
        private System.Windows.Forms.CheckBox checkBoxCustomX86;
        private winC2D.ModernButton buttonBrowseProgramFiles;
        private System.Windows.Forms.TextBox textBoxProgramFiles;
        private System.Windows.Forms.Label labelProgramFiles;
        private System.Windows.Forms.GroupBox groupBoxStoragePolicy;
        private System.Windows.Forms.Label labelStoragePolicyNote;
        private winC2D.ModernButton buttonOpenWindowsStorage;
        
        private winC2D.ThemedListView listViewAppData;
        private System.Windows.Forms.ColumnHeader columnHeaderAppName;
        private System.Windows.Forms.ColumnHeader columnHeaderAppPath;
        private System.Windows.Forms.ColumnHeader columnHeaderAppSize;
        private System.Windows.Forms.ColumnHeader columnHeaderAppStatus; // 灏嗙被鍨嬪垪鏀逛负鐘舵�佸垪
        private winC2D.ModernButton buttonMigrateAppData;
        private winC2D.ModernButton buttonRefreshAppData;
        private winC2D.ModernButton buttonCheckSuspiciousAppData;
        private System.Windows.Forms.ToolStripMenuItem menuLanguage;
        private System.Windows.Forms.ToolStripMenuItem menuLanguageEnglish;
        private System.Windows.Forms.ToolStripMenuItem menuLanguageChinese;
        private System.Windows.Forms.ToolStripMenuItem menuLanguageJapanese;
        private System.Windows.Forms.ToolStripMenuItem menuLanguageKorean;
        private System.Windows.Forms.ToolStripMenuItem menuLanguageRussian;
        private System.Windows.Forms.ToolStripMenuItem menuLanguageChineseTraditional;
        private System.Windows.Forms.ToolStripMenuItem menuLanguagePortuguese;
        private winC2D.ModernButton buttonRefreshSoftware;
        private winC2D.ModernButton buttonCheckSuspicious;
        private winC2D.ModernButton buttonManageScanPaths;
        private System.Windows.Forms.ToolStripMenuItem menuTheme;
        private System.Windows.Forms.ToolStripMenuItem menuThemeLight;
        private System.Windows.Forms.ToolStripMenuItem menuThemeDark;

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
            menuStrip1 = new MenuStrip();
            menuLog = new ToolStripMenuItem();
            menuLanguage = new ToolStripMenuItem();
            menuLanguageEnglish = new ToolStripMenuItem();
            menuLanguageChinese = new ToolStripMenuItem();
            menuLanguageChineseTraditional = new ToolStripMenuItem();
            menuLanguageJapanese = new ToolStripMenuItem();
            menuLanguageKorean = new ToolStripMenuItem();
            menuLanguageRussian = new ToolStripMenuItem();
            menuLanguagePortuguese = new ToolStripMenuItem();
            menuTheme = new ToolStripMenuItem();
            menuThemeLight = new ToolStripMenuItem();
            menuThemeDark = new ToolStripMenuItem();
            tabControl1 = new winC2D.ModernTabControl();
            tabPageSoftware = new TabPage();
            listViewSoftware = new winC2D.ThemedListView();
            columnHeaderName = new ColumnHeader();
            columnHeaderPath = new ColumnHeader();
            columnHeaderSize = new ColumnHeader();
            columnHeaderStatus = new ColumnHeader();
            buttonMigrateSoftware = new winC2D.ModernButton();
            buttonRefreshSoftware = new winC2D.ModernButton();
            buttonCheckSuspicious = new winC2D.ModernButton();
            buttonManageScanPaths = new winC2D.ModernButton();
            tabPageAppData = new TabPage();
            listViewAppData = new winC2D.ThemedListView();
            columnHeaderAppName = new ColumnHeader();
            columnHeaderAppPath = new ColumnHeader();
            columnHeaderAppSize = new ColumnHeader();
            columnHeaderAppStatus = new ColumnHeader();
            buttonMigrateAppData = new winC2D.ModernButton();
            buttonRefreshAppData = new winC2D.ModernButton();
            buttonCheckSuspiciousAppData = new winC2D.ModernButton();
            tabPageSettings = new TabPage();
            groupBoxStoragePolicy = new GroupBox();
            buttonOpenWindowsStorage = new winC2D.ModernButton();
            labelStoragePolicyNote = new Label();
            groupBoxProgramFiles = new GroupBox();
            labelProgramFilesNote = new Label();
            buttonResetProgramFiles = new winC2D.ModernButton();
            buttonApplyProgramFiles = new winC2D.ModernButton();
            buttonBrowseProgramFilesX86 = new winC2D.ModernButton();
            textBoxProgramFilesX86 = new TextBox();
            labelProgramFilesX86 = new Label();
            checkBoxCustomX86 = new CheckBox();
            buttonBrowseProgramFiles = new winC2D.ModernButton();
            textBoxProgramFiles = new TextBox();
            labelProgramFiles = new Label();
            menuStrip1.SuspendLayout();
            tabControl1.SuspendLayout();
            tabPageSoftware.SuspendLayout();
            tabPageAppData.SuspendLayout();
            tabPageSettings.SuspendLayout();
            groupBoxStoragePolicy.SuspendLayout();
            groupBoxProgramFiles.SuspendLayout();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            menuStrip1.Items.AddRange(new ToolStripItem[] { menuLog, menuLanguage, menuTheme });
            menuStrip1.Location = new System.Drawing.Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Padding = new Padding(10, 4, 10, 4);
            menuStrip1.RenderMode = ToolStripRenderMode.Professional;
            menuStrip1.Size = new System.Drawing.Size(1000, 30);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // menuLog
            // 
            menuLog.Name = "menuLog";
            menuLog.Size = new System.Drawing.Size(68, 21);
            menuLog.Text = "迁移日志";
            menuLog.Click += menuLog_Click;
            // 
            // menuLanguage
            // 
            menuLanguage.DropDownItems.AddRange(new ToolStripItem[] { menuLanguageEnglish, menuLanguageChinese, menuLanguageChineseTraditional, menuLanguageJapanese, menuLanguageKorean, menuLanguageRussian, menuLanguagePortuguese });
            menuLanguage.Name = "menuLanguage";
            menuLanguage.Size = new System.Drawing.Size(44, 21);
            menuLanguage.Text = "语言";
            // 
            // menuLanguageEnglish
            // 
            menuLanguageEnglish.Name = "menuLanguageEnglish";
            menuLanguageEnglish.Size = new System.Drawing.Size(179, 22);
            menuLanguageEnglish.Text = "English";
            menuLanguageEnglish.Click += menuLanguageEnglish_Click;
            // 
            // menuLanguageChinese
            // 
            menuLanguageChinese.Name = "menuLanguageChinese";
            menuLanguageChinese.Size = new System.Drawing.Size(179, 22);
            menuLanguageChinese.Text = "简体中文";
            menuLanguageChinese.Click += menuLanguageChinese_Click;
            // 
            // menuLanguageChineseTraditional
            // 
            menuLanguageChineseTraditional.Name = "menuLanguageChineseTraditional";
            menuLanguageChineseTraditional.Size = new System.Drawing.Size(179, 22);
            menuLanguageChineseTraditional.Text = "繁體中文";
            menuLanguageChineseTraditional.Click += menuLanguageChineseTraditional_Click;
            // 
            // menuLanguageJapanese
            // 
            menuLanguageJapanese.Name = "menuLanguageJapanese";
            menuLanguageJapanese.Size = new System.Drawing.Size(179, 22);
            menuLanguageJapanese.Text = "日本語";
            menuLanguageJapanese.Click += menuLanguageJapanese_Click;
            // 
            // menuLanguageKorean
            // 
            menuLanguageKorean.Name = "menuLanguageKorean";
            menuLanguageKorean.Size = new System.Drawing.Size(179, 22);
            menuLanguageKorean.Text = "???";
            menuLanguageKorean.Click += menuLanguageKorean_Click;
            // 
            // menuLanguageRussian
            // 
            menuLanguageRussian.Name = "menuLanguageRussian";
            menuLanguageRussian.Size = new System.Drawing.Size(179, 22);
            menuLanguageRussian.Text = "Русский";
            menuLanguageRussian.Click += menuLanguageRussian_Click;
            // 
            // menuLanguagePortuguese
            // 
            menuLanguagePortuguese.Name = "menuLanguagePortuguese";
            menuLanguagePortuguese.Size = new System.Drawing.Size(179, 22);
            menuLanguagePortuguese.Text = "Português (Brasil)";
            menuLanguagePortuguese.Click += menuLanguagePortuguese_Click;
            // 
            // menuTheme
            // 
            menuTheme.DropDownItems.AddRange(new ToolStripItem[] { menuThemeLight, menuThemeDark });
            menuTheme.Name = "menuTheme";
            menuTheme.Size = new System.Drawing.Size(48, 21);
            menuTheme.Text = "Theme";
            // 
            // menuThemeLight
            // 
            menuThemeLight.Name = "menuThemeLight";
            menuThemeLight.Size = new System.Drawing.Size(179, 22);
            menuThemeLight.Text = "Light";
            menuThemeLight.Click += menuThemeLight_Click;
            // 
            // menuThemeDark
            // 
            menuThemeDark.Name = "menuThemeDark";
            menuThemeDark.Size = new System.Drawing.Size(179, 22);
            menuThemeDark.Text = "Dark";
            menuThemeDark.Click += menuThemeDark_Click;
            // 
            // tabControl1
            // 
            tabControl1.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabControl1.Controls.Add(tabPageSoftware);
            tabControl1.Controls.Add(tabPageAppData);
            tabControl1.Controls.Add(tabPageSettings);
            tabControl1.Location = new System.Drawing.Point(10, 30);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new System.Drawing.Size(979, 560);
            tabControl1.TabIndex = 1;
            tabControl1.DrawMode = TabDrawMode.OwnerDrawFixed;
            tabControl1.ItemSize = new System.Drawing.Size(150, 36);
            tabControl1.Padding = new Point(12, 6);
            tabControl1.SizeMode = TabSizeMode.Fixed;
            // 
            // tabPageSoftware
            // 
            tabPageSoftware.Controls.Add(listViewSoftware);
            tabPageSoftware.Controls.Add(buttonMigrateSoftware);
            tabPageSoftware.Controls.Add(buttonRefreshSoftware);
            tabPageSoftware.Controls.Add(buttonCheckSuspicious);
            tabPageSoftware.Controls.Add(buttonManageScanPaths);
            tabPageSoftware.Location = new System.Drawing.Point(4, 26);
            tabPageSoftware.Name = "tabPageSoftware";
            tabPageSoftware.Padding = new Padding(3);
            tabPageSoftware.Size = new System.Drawing.Size(971, 530);
            tabPageSoftware.TabIndex = 1;
            tabPageSoftware.Text = "软件迁移";
            tabPageSoftware.UseVisualStyleBackColor = false;
            // 
            // listViewSoftware
            // 
            listViewSoftware.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listViewSoftware.CheckBoxes = true;
            listViewSoftware.Columns.AddRange(new ColumnHeader[] { columnHeaderName, columnHeaderPath, columnHeaderSize, columnHeaderStatus });
            listViewSoftware.FullRowSelect = true;
            listViewSoftware.Location = new System.Drawing.Point(5, 6);
            listViewSoftware.Name = "listViewSoftware";
            listViewSoftware.Size = new System.Drawing.Size(960, 485);
            listViewSoftware.TabIndex = 0;
            listViewSoftware.UseCompatibleStateImageBehavior = false;
            listViewSoftware.View = View.Details;
            listViewSoftware.BorderStyle = BorderStyle.None;
            // 
            // columnHeaderName
            // 
            columnHeaderName.Text = "软件名称";
            columnHeaderName.Width = 220;
            // 
            // columnHeaderPath
            // 
            columnHeaderPath.Text = "安装路径";
            columnHeaderPath.Width = 480;
            // 
            // columnHeaderSize
            // 
            columnHeaderSize.Text = "大小";
            columnHeaderSize.Width = 120;
            // 
            // columnHeaderStatus
            // 
            columnHeaderStatus.Text = "状态";
            columnHeaderStatus.Width = 120;
            // 
            // buttonMigrateSoftware
            // 
            buttonMigrateSoftware.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonMigrateSoftware.Location = new System.Drawing.Point(867, 497);
            buttonMigrateSoftware.Name = "buttonMigrateSoftware";
            buttonMigrateSoftware.Size = new System.Drawing.Size(98, 27);
            buttonMigrateSoftware.TabIndex = 1;
            buttonMigrateSoftware.Text = "迁移所选";
            buttonMigrateSoftware.UseVisualStyleBackColor = true;
            buttonMigrateSoftware.Click += buttonMigrateSoftware_Click;
            // 
            // buttonRefreshSoftware
            // 
            buttonRefreshSoftware.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonRefreshSoftware.Location = new System.Drawing.Point(763, 497);
            buttonRefreshSoftware.Name = "buttonRefreshSoftware";
            buttonRefreshSoftware.Size = new System.Drawing.Size(98, 27);
            buttonRefreshSoftware.TabIndex = 2;
            buttonRefreshSoftware.Text = "刷新列表";
            buttonRefreshSoftware.UseVisualStyleBackColor = true;
            buttonRefreshSoftware.Click += buttonRefreshSoftware_Click;
            // 
            // buttonCheckSuspicious
            // 
            buttonCheckSuspicious.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCheckSuspicious.Location = new System.Drawing.Point(659, 497);
            buttonCheckSuspicious.Name = "buttonCheckSuspicious";
            buttonCheckSuspicious.Size = new System.Drawing.Size(98, 27);
            buttonCheckSuspicious.TabIndex = 3;
            buttonCheckSuspicious.Text = "检查可疑";
            buttonCheckSuspicious.UseVisualStyleBackColor = true;
            buttonCheckSuspicious.Click += buttonCheckSuspiciousSoftware_Click;
            // 
            // buttonManageScanPaths
            // 
            buttonManageScanPaths.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonManageScanPaths.Location = new System.Drawing.Point(5, 497);
            buttonManageScanPaths.Name = "buttonManageScanPaths";
            buttonManageScanPaths.Size = new System.Drawing.Size(150, 27);
            buttonManageScanPaths.TabIndex = 4;
            buttonManageScanPaths.Text = "扫描路径";
            buttonManageScanPaths.UseVisualStyleBackColor = true;
            buttonManageScanPaths.Click += buttonManageScanPaths_Click;
            // 
            // tabPageAppData
            // 
            tabPageAppData.Controls.Add(listViewAppData);
            tabPageAppData.Controls.Add(buttonMigrateAppData);
            tabPageAppData.Controls.Add(buttonRefreshAppData);
            tabPageAppData.Controls.Add(buttonCheckSuspiciousAppData);
            tabPageAppData.Location = new System.Drawing.Point(4, 26);
            tabPageAppData.Name = "tabPageAppData";
            tabPageAppData.Padding = new Padding(3);
            tabPageAppData.Size = new System.Drawing.Size(971, 530);
            tabPageAppData.TabIndex = 3;
            tabPageAppData.Text = "AppData (mklink)";
            tabPageAppData.UseVisualStyleBackColor = false;
            // 
            // listViewAppData
            // 
            listViewAppData.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listViewAppData.CheckBoxes = true;
            listViewAppData.Columns.AddRange(new ColumnHeader[] { columnHeaderAppName, columnHeaderAppPath, columnHeaderAppSize, columnHeaderAppStatus });
            listViewAppData.FullRowSelect = true;
            listViewAppData.Location = new System.Drawing.Point(5, 6);
            listViewAppData.Name = "listViewAppData";
            listViewAppData.Size = new System.Drawing.Size(960, 485);
            listViewAppData.TabIndex = 0;
            listViewAppData.UseCompatibleStateImageBehavior = false;
            listViewAppData.View = View.Details;
            listViewAppData.BorderStyle = BorderStyle.None;
            // 
            // columnHeaderAppName
            // 
            columnHeaderAppName.Text = "应用名称";
            columnHeaderAppName.Width = 260;
            // 
            // columnHeaderAppPath
            // 
            columnHeaderAppPath.Text = "路径";
            columnHeaderAppPath.Width = 500;
            // 
            // columnHeaderAppSize
            // 
            columnHeaderAppSize.Text = "大小";
            columnHeaderAppSize.Width = 120;
            // 
            // columnHeaderAppStatus
            // 
            columnHeaderAppStatus.Text = "状态";
            columnHeaderAppStatus.Width = 80;
            // 
            // buttonMigrateAppData
            // 
            buttonMigrateAppData.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonMigrateAppData.Location = new System.Drawing.Point(867, 497);
            buttonMigrateAppData.Name = "buttonMigrateAppData";
            buttonMigrateAppData.Size = new System.Drawing.Size(98, 27);
            buttonMigrateAppData.TabIndex = 1;
            buttonMigrateAppData.Text = "迁移所选";
            buttonMigrateAppData.UseVisualStyleBackColor = true;
            buttonMigrateAppData.Click += buttonMigrateAppData_Click;
            // 
            // buttonRefreshAppData
            // 
            buttonRefreshAppData.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonRefreshAppData.Location = new System.Drawing.Point(763, 497);
            buttonRefreshAppData.Name = "buttonRefreshAppData";
            buttonRefreshAppData.Size = new System.Drawing.Size(98, 27);
            buttonRefreshAppData.TabIndex = 2;
            buttonRefreshAppData.Text = "刷新列表";
            buttonRefreshAppData.UseVisualStyleBackColor = true;
            buttonRefreshAppData.Click += buttonRefreshAppData_Click;
            // 
            // buttonCheckSuspiciousAppData
            // 
            buttonCheckSuspiciousAppData.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCheckSuspiciousAppData.Location = new System.Drawing.Point(659, 497);
            buttonCheckSuspiciousAppData.Name = "buttonCheckSuspiciousAppData";
            buttonCheckSuspiciousAppData.Size = new System.Drawing.Size(98, 27);
            buttonCheckSuspiciousAppData.TabIndex = 3;
            buttonCheckSuspiciousAppData.Text = "检查可疑";
            buttonCheckSuspiciousAppData.UseVisualStyleBackColor = true;
            buttonCheckSuspiciousAppData.Click += buttonCheckSuspiciousAppData_Click;
            // 
            // tabPageSettings
            // 
            tabPageSettings.Controls.Add(groupBoxStoragePolicy);
            tabPageSettings.Controls.Add(groupBoxProgramFiles);
            tabPageSettings.Location = new System.Drawing.Point(4, 26);
            tabPageSettings.Name = "tabPageSettings";
            tabPageSettings.Padding = new Padding(3);
            tabPageSettings.Size = new System.Drawing.Size(971, 530);
            tabPageSettings.TabIndex = 0;
            tabPageSettings.Text = "系统设置";
            tabPageSettings.UseVisualStyleBackColor = false;
            // 
            // groupBoxStoragePolicy
            // 
            groupBoxStoragePolicy.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxStoragePolicy.Controls.Add(buttonOpenWindowsStorage);
            groupBoxStoragePolicy.Controls.Add(labelStoragePolicyNote);
            groupBoxStoragePolicy.Location = new System.Drawing.Point(13, 238);
            groupBoxStoragePolicy.Name = "groupBoxStoragePolicy";
            groupBoxStoragePolicy.Size = new System.Drawing.Size(943, 85);
            groupBoxStoragePolicy.TabIndex = 1;
            groupBoxStoragePolicy.TabStop = false;
            groupBoxStoragePolicy.Text = "鏂板唴瀹逛繚瀛樹綅缃紙Microsoft Store 搴旂敤鍜岀敤鎴峰唴瀹癸級";
            // 
            // buttonOpenWindowsStorage
            // 
            buttonOpenWindowsStorage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonOpenWindowsStorage.Location = new System.Drawing.Point(781, 30);
            buttonOpenWindowsStorage.Name = "buttonOpenWindowsStorage";
            buttonOpenWindowsStorage.Size = new System.Drawing.Size(149, 42);
            buttonOpenWindowsStorage.TabIndex = 1;
            buttonOpenWindowsStorage.Text = "鎵撳紑 Windows 璁剧疆";
            buttonOpenWindowsStorage.UseVisualStyleBackColor = true;
            buttonOpenWindowsStorage.Click += buttonOpenWindowsStorage_Click;
            // 
            // labelStoragePolicyNote
            // 
            labelStoragePolicyNote.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            labelStoragePolicyNote.ForeColor = System.Drawing.Color.DarkBlue;
            labelStoragePolicyNote.Location = new System.Drawing.Point(13, 26);
            labelStoragePolicyNote.Name = "labelStoragePolicyNote";
            labelStoragePolicyNote.Size = new System.Drawing.Size(751, 51);
            labelStoragePolicyNote.TabIndex = 0;
            labelStoragePolicyNote.Text = "?? 此设置影响 Microsoft Store 应用和新用户内容。需要 Windows 10 或更高版本。";
            // 
            // groupBoxProgramFiles
            // 
            groupBoxProgramFiles.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBoxProgramFiles.Controls.Add(labelProgramFilesNote);
            groupBoxProgramFiles.Controls.Add(buttonResetProgramFiles);
            groupBoxProgramFiles.Controls.Add(buttonApplyProgramFiles);
            groupBoxProgramFiles.Controls.Add(buttonBrowseProgramFilesX86);
            groupBoxProgramFiles.Controls.Add(textBoxProgramFilesX86);
            groupBoxProgramFiles.Controls.Add(labelProgramFilesX86);
            groupBoxProgramFiles.Controls.Add(checkBoxCustomX86);
            groupBoxProgramFiles.Controls.Add(buttonBrowseProgramFiles);
            groupBoxProgramFiles.Controls.Add(textBoxProgramFiles);
            groupBoxProgramFiles.Controls.Add(labelProgramFiles);
            groupBoxProgramFiles.Location = new System.Drawing.Point(13, 13);
            groupBoxProgramFiles.Name = "groupBoxProgramFiles";
            groupBoxProgramFiles.Size = new System.Drawing.Size(943, 212);
            groupBoxProgramFiles.TabIndex = 0;
            groupBoxProgramFiles.TabStop = false;
            groupBoxProgramFiles.Text = "Program Files 浣嶇疆锛堜紶缁熸闈㈢▼搴忥級";
            // 
            // labelProgramFilesNote
            // 
            labelProgramFilesNote.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            labelProgramFilesNote.ForeColor = System.Drawing.Color.DarkBlue;
            labelProgramFilesNote.Location = new System.Drawing.Point(13, 157);
            labelProgramFilesNote.Name = "labelProgramFilesNote";
            labelProgramFilesNote.Size = new System.Drawing.Size(917, 42);
            labelProgramFilesNote.TabIndex = 9;
            labelProgramFilesNote.Text = "?? 此设置影响传统桌面程序。";
            // 
            // buttonResetProgramFiles
            // 
            buttonResetProgramFiles.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonResetProgramFiles.Location = new System.Drawing.Point(860, 46);
            buttonResetProgramFiles.Name = "buttonResetProgramFiles";
            buttonResetProgramFiles.Size = new System.Drawing.Size(70, 25);
            buttonResetProgramFiles.TabIndex = 8;
            buttonResetProgramFiles.Text = "鎭㈠榛樿";
            buttonResetProgramFiles.UseVisualStyleBackColor = true;
            buttonResetProgramFiles.Click += buttonResetProgramFiles_Click;
            // 
            // buttonApplyProgramFiles
            // 
            buttonApplyProgramFiles.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonApplyProgramFiles.Location = new System.Drawing.Point(781, 46);
            buttonApplyProgramFiles.Name = "buttonApplyProgramFiles";
            buttonApplyProgramFiles.Size = new System.Drawing.Size(70, 25);
            buttonApplyProgramFiles.TabIndex = 7;
            buttonApplyProgramFiles.Text = "搴旂敤";
            buttonApplyProgramFiles.UseVisualStyleBackColor = true;
            buttonApplyProgramFiles.Click += buttonApplyProgramFiles_Click;
            // 
            // buttonBrowseProgramFilesX86
            // 
            buttonBrowseProgramFilesX86.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonBrowseProgramFilesX86.Enabled = false;
            buttonBrowseProgramFilesX86.Location = new System.Drawing.Point(702, 127);
            buttonBrowseProgramFilesX86.Name = "buttonBrowseProgramFilesX86";
            buttonBrowseProgramFilesX86.Size = new System.Drawing.Size(70, 25);
            buttonBrowseProgramFilesX86.TabIndex = 6;
            buttonBrowseProgramFilesX86.Text = "娴忚";
            buttonBrowseProgramFilesX86.UseVisualStyleBackColor = true;
            buttonBrowseProgramFilesX86.Click += buttonBrowseProgramFilesX86_Click;
            // 
            // textBoxProgramFilesX86
            // 
            textBoxProgramFilesX86.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxProgramFilesX86.Enabled = false;
            textBoxProgramFilesX86.Location = new System.Drawing.Point(13, 128);
            textBoxProgramFilesX86.Name = "textBoxProgramFilesX86";
            textBoxProgramFilesX86.Size = new System.Drawing.Size(681, 23);
            textBoxProgramFilesX86.TabIndex = 5;
            // 
            // labelProgramFilesX86
            // 
            labelProgramFilesX86.AutoSize = true;
            labelProgramFilesX86.Enabled = false;
            labelProgramFilesX86.Location = new System.Drawing.Point(13, 106);
            labelProgramFilesX86.Name = "labelProgramFilesX86";
            labelProgramFilesX86.Size = new System.Drawing.Size(202, 17);
            labelProgramFilesX86.TabIndex = 4;
            labelProgramFilesX86.Text = "Program Files 默认位置(32位):";
            // 
            // checkBoxCustomX86
            // 
            checkBoxCustomX86.AutoSize = true;
            checkBoxCustomX86.Location = new System.Drawing.Point(13, 81);
            checkBoxCustomX86.Name = "checkBoxCustomX86";
            checkBoxCustomX86.Size = new System.Drawing.Size(145, 21);
            checkBoxCustomX86.TabIndex = 3;
            checkBoxCustomX86.Text = "鑷畾涔?32浣?绋嬪簭璺緞";
            checkBoxCustomX86.UseVisualStyleBackColor = true;
            checkBoxCustomX86.CheckedChanged += checkBoxCustomX86_CheckedChanged;
            // 
            // buttonBrowseProgramFiles
            // 
            buttonBrowseProgramFiles.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonBrowseProgramFiles.Location = new System.Drawing.Point(702, 46);
            buttonBrowseProgramFiles.Name = "buttonBrowseProgramFiles";
            buttonBrowseProgramFiles.Size = new System.Drawing.Size(70, 25);
            buttonBrowseProgramFiles.TabIndex = 2;
            buttonBrowseProgramFiles.Text = "娴忚";
            buttonBrowseProgramFiles.UseVisualStyleBackColor = true;
            buttonBrowseProgramFiles.Click += buttonBrowseProgramFiles_Click;
            // 
            // textBoxProgramFiles
            // 
            textBoxProgramFiles.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            textBoxProgramFiles.Location = new System.Drawing.Point(13, 47);
            textBoxProgramFiles.Name = "textBoxProgramFiles";
            textBoxProgramFiles.Size = new System.Drawing.Size(681, 23);
            textBoxProgramFiles.TabIndex = 1;
            textBoxProgramFiles.TextChanged += textBoxProgramFiles_TextChanged;
            // 
            // labelProgramFiles
            // 
            labelProgramFiles.AutoSize = true;
            labelProgramFiles.Location = new System.Drawing.Point(13, 26);
            labelProgramFiles.Name = "labelProgramFiles";
            labelProgramFiles.Size = new System.Drawing.Size(202, 17);
            labelProgramFiles.TabIndex = 0;
            labelProgramFiles.Text = "Program Files 默认位置(64位):";
            // 
            // MainForm
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1000, 600);
            Controls.Add(tabControl1);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            MinimumSize = new System.Drawing.Size(700, 460);
            Name = "MainForm";
            Text = "Windows瀛樺偍杩佺Щ鍔╂墜";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            tabControl1.ResumeLayout(false);
            tabPageSoftware.ResumeLayout(false);
            tabPageSoftware.PerformLayout();
            tabPageAppData.ResumeLayout(false);
            tabPageSettings.ResumeLayout(false);
            groupBoxStoragePolicy.ResumeLayout(false);
            groupBoxProgramFiles.ResumeLayout(false);
            groupBoxProgramFiles.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion
    }
}
