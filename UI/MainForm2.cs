using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using winC2D.Core;
using winC2D.UI;
using winC2D.Views;

namespace winC2D
{
    // ══════════════════════════════════════════════════════════════════════════
    // MainForm2 — Main application window (v2 UI)
    //   Layout : [200 px sidebar] [fill content area]
    //   Pages  : Software Migration | AppData Migration | Settings | Log
    //   Themes : Light / Dark (ThemeManager v2)
    // ══════════════════════════════════════════════════════════════════════════
    public class MainForm2 : Form
    {
        // ── Child controls ────────────────────────────────────────────────────
        private SideNavBar      _nav;
        private Panel           _contentHost;
        private Panel           _titleBar;
        private Label           _titleLabel;
        private Label           _subtitleLabel;
        private MenuStrip       _menuStrip;
        private ModernToolStripRenderer _menuRenderer;

        // ── Pages ─────────────────────────────────────────────────────────────
        private SoftwarePage    _softwarePage;
        private AppDataPage     _appDataPage;
        private SettingsPage2   _settingsPage;
        private LogPage         _logPage;

        // ── Shared services ───────────────────────────────────────────────────
        internal readonly winC2D.Core.MigrationEngineLogger Logger = new winC2D.Core.MigrationEngineLogger();

        // ── Constructor ───────────────────────────────────────────────────────
        public MainForm2()
        {
            SuspendLayout();

            Text           = "winC2D — Windows Software Migration Tool";
            MinimumSize    = new Size(900, 600);
            Size           = new Size(1100, 700);
            StartPosition  = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            Font           = new Font("Segoe UI Variable", 9.5f, FontStyle.Regular, GraphicsUnit.Point);

            // Application icon (embedded resource, non-fatal if missing)
            try
            {
                using var stream = typeof(MainForm2).Assembly
                    .GetManifestResourceStream("winC2D.winc2d.ico");
                if (stream != null) Icon = new Icon(stream);
            }
            catch { }

            // Theme preference is loaded here so BuildLayout() can read the correct palette
            ThemeManager.LoadPreference();

            BuildLayout();

            ResumeLayout(false);
            PerformLayout();

            ThemeManager.ThemeChanged    += OnThemeChanged;
            Localization.LanguageChanged += OnLanguageChanged;
            Load        += OnLoad;
            Shown       += OnShown;
            FormClosing += OnFormClosing;
        }

        // ════════════════════════════════════════════════════════════════════
        // Layout construction
        // ════════════════════════════════════════════════════════════════════
        private void BuildLayout()
        {
            // Hidden menu strip (reserved for future keyboard shortcuts)
            _menuStrip = new MenuStrip { Visible = false };
            BuildMenuStrip();
            Controls.Add(_menuStrip);
            MainMenuStrip = _menuStrip;

            // Two-column root: fixed sidebar | fill content
            var root = new TableLayoutPanel
            {
                Dock            = DockStyle.Fill,
                ColumnCount     = 2,
                RowCount        = 1,
                Padding         = Padding.Empty,
                Margin          = Padding.Empty,
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            // Sidebar panel
            var sidebar = new Panel { Dock = DockStyle.Fill, Padding = Padding.Empty };
            root.Controls.Add(sidebar, 0, 0);

            // Logo area at the top of the sidebar
            var logoPanel = new Panel
            {
                Dock    = DockStyle.Top,
                Height  = 56,
                Padding = new Padding(14, 0, 8, 0)
            };
            logoPanel.Paint += (s, e) =>
            {
                var p    = ThemeManager.Current;
                var font = new Font("Segoe UI Variable", 12f, FontStyle.Bold);
                e.Graphics.Clear(p.SidebarBackground);
                TextRenderer.DrawText(e.Graphics, "winC2D", font,
                    new Rectangle(16, 14, 160, 28), p.Foreground, TextFormatFlags.Left);
            };

            // Navigation must be added before logoPanel so DockStyle.Fill gets the remaining area
            _nav = new SideNavBar { Dock = DockStyle.Fill, TopPadding = 56 };
            sidebar.Controls.Add(_nav);
            sidebar.Controls.Add(logoPanel);  // DockStyle.Top docked last — correct z-order

            // Content host (right column)
            _contentHost = new Panel
            {
                Dock    = DockStyle.Fill,
                Padding = new Padding(24, 20, 24, 20)
            };
            root.Controls.Add(_contentHost, 1, 0);

            // Create pages (all initially hidden)
            _softwarePage = new SoftwarePage(Logger) { Dock = DockStyle.Fill, Visible = false };
            _appDataPage  = new AppDataPage(Logger)  { Dock = DockStyle.Fill, Visible = false };
            _settingsPage = new SettingsPage2()       { Dock = DockStyle.Fill, Visible = false };
            _logPage      = new LogPage(Logger)       { Dock = DockStyle.Fill, Visible = false };

            _contentHost.Controls.Add(_softwarePage);
            _contentHost.Controls.Add(_appDataPage);
            _contentHost.Controls.Add(_settingsPage);
            _contentHost.Controls.Add(_logPage);

            // Navigation items
            _nav.AddItem(new SideNavItem { Key = "software", Label = Localization.T("Nav.Software"), Icon = "\uE74C", Page = _softwarePage });
            _nav.AddItem(new SideNavItem { Key = "appdata",  Label = Localization.T("Nav.AppData"),  Icon = "\uE838", Page = _appDataPage });
            _nav.AddItem(new SideNavItem { Key = "settings", Label = Localization.T("Nav.Settings"), Icon = "\uE713", Page = _settingsPage });
            _nav.AddItem(new SideNavItem { Key = "log",      Label = Localization.T("Nav.Log"),      Icon = "\uE9D9", Page = _logPage });

            _nav.SelectionChanged += OnNavSelectionChanged;

            ApplyTheme();
        }

        // ════════════════════════════════════════════════════════════════════
        // Menu strip
        // ════════════════════════════════════════════════════════════════════
        private void BuildMenuStrip()
        {
            var menuTheme = new ToolStripMenuItem("Theme");
            var miLight   = new ToolStripMenuItem("Light", null, (s, e) => ThemeManager.SetTheme(AppTheme.Light));
            var miDark    = new ToolStripMenuItem("Dark",  null, (s, e) => ThemeManager.SetTheme(AppTheme.Dark));
            menuTheme.DropDownItems.Add(miLight);
            menuTheme.DropDownItems.Add(miDark);
            _menuStrip.Items.Add(menuTheme);
        }

        // ════════════════════════════════════════════════════════════════════
        // Navigation
        // ════════════════════════════════════════════════════════════════════
        private void OnNavSelectionChanged(object sender, SideNavItem item)
        {
            foreach (Control c in _contentHost.Controls)
                c.Visible = false;

            if (item?.Page != null)
                item.Page.Visible = true;
        }

        // ════════════════════════════════════════════════════════════════════
        // Form lifecycle
        // ════════════════════════════════════════════════════════════════════
        private void OnLoad(object sender, EventArgs e)
        {
            ThemeManager.ApplyTitleBarToWindow(Handle, ThemeManager.CurrentTheme == AppTheme.Dark);
            _nav.Select(0);  // Show first page by default
        }

        private void OnShown(object sender, EventArgs e)
        {
            _softwarePage.LoadData();  // Trigger initial software scan
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            ThemeManager.ThemeChanged    -= OnThemeChanged;
            Localization.LanguageChanged -= OnLanguageChanged;
        }

        // ════════════════════════════════════════════════════════════════════
        // Theme
        // ════════════════════════════════════════════════════════════════════
        private void ApplyTheme()
        {
            var p = ThemeManager.Current;
            BackColor = p.Background;
            ForeColor = p.Foreground;

            ThemeManager.ApplyTitleBarToWindow(IsHandleCreated ? Handle : IntPtr.Zero,
                ThemeManager.CurrentTheme == AppTheme.Dark);

            if (_menuRenderer == null)
                _menuRenderer = new ModernToolStripRenderer(p);
            else
                _menuRenderer.UpdatePalette(p);
            _menuStrip.Renderer  = _menuRenderer;
            _menuStrip.BackColor = p.MenuBackground;
            _menuStrip.ForeColor = p.MenuForeground;

            _contentHost.BackColor = p.Background;

            _softwarePage?.ApplyTheme(p);
            _appDataPage?.ApplyTheme(p);
            _settingsPage?.ApplyTheme(p);
            _logPage?.ApplyTheme(p);

            Invalidate(true);
        }

        private void OnThemeChanged(object sender, EventArgs e)
        {
            if (InvokeRequired) Invoke(new Action(ApplyTheme));
            else ApplyTheme();
        }

        // ════════════════════════════════════════════════════════════════════
        // Localization
        // ════════════════════════════════════════════════════════════════════
        private void OnLanguageChanged(object sender, EventArgs e)
        {
            if (InvokeRequired) { Invoke(new Action(RefreshLocalization)); return; }
            RefreshLocalization();
        }

        private void RefreshLocalization()
        {
            // Rebuild nav labels with the new language
            _nav.ClearItems();
            _nav.AddItem(new SideNavItem { Key = "software", Label = Localization.T("Nav.Software"), Icon = "\uE74C", Page = _softwarePage });
            _nav.AddItem(new SideNavItem { Key = "appdata",  Label = Localization.T("Nav.AppData"),  Icon = "\uE838", Page = _appDataPage });
            _nav.AddItem(new SideNavItem { Key = "settings", Label = Localization.T("Nav.Settings"), Icon = "\uE713", Page = _settingsPage });
            _nav.AddItem(new SideNavItem { Key = "log",      Label = Localization.T("Nav.Log"),      Icon = "\uE9D9", Page = _logPage });
            _nav.Select(0);

            _softwarePage?.RefreshLocalization();
            _appDataPage?.RefreshLocalization();
            _settingsPage?.RefreshLocalization();
        }
    }
}
