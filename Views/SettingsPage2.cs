using System;
using System.Drawing;
using System.Windows.Forms;
using winC2D.Core;
using winC2D.UI;

namespace winC2D.Views
{
    // ══════════════════════════════════════════════════════════════════════════
    // SettingsPage2 — Settings page: theme, language, about
    // ══════════════════════════════════════════════════════════════════════════
    public class SettingsPage2 : Panel
    {
        private PageHeader _header;
        private Panel      _scrollHost;
        private CardPanel  _themeCard;
        private CardPanel  _langCard;
        private CardPanel  _aboutCard;

        // Kept as fields so we can re-style them when the theme changes
        private winC2D.UI.ModernButton _btnLight;
        private winC2D.UI.ModernButton _btnDark;

        public SettingsPage2()
        {
            BuildUI();
            ThemeManager.ThemeChanged += (s, e) => RefreshThemeButtons();
        }

        // ════════════════════════════════════════════════════════════════════
        // UI construction
        // ════════════════════════════════════════════════════════════════════
        private void BuildUI()
        {
            // Two-row root: fixed header, then scrollable card area
            var root = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 2,
                ColumnCount = 1,
                Padding     = Padding.Empty,
                Margin      = Padding.Empty
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            _header = new PageHeader
            {
                Dock     = DockStyle.Fill,
                Title    = Localization.T("Nav.Settings"),
                Subtitle = Localization.T("Desc.Settings")
            };
            root.Controls.Add(_header, 0, 0);

            // Scrollable container for all cards
            _scrollHost = new Panel
            {
                Dock       = DockStyle.Fill,
                AutoScroll = true,
                Padding    = new Padding(0, 4, 0, 8)
            };
            root.Controls.Add(_scrollHost, 0, 1);

            // Vertical card stack inside the scroll host
            var stack = new FlowLayoutPanel
            {
                Dock          = DockStyle.Top,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                AutoSizeMode  = AutoSizeMode.GrowAndShrink,
                Padding       = Padding.Empty,
                Margin        = Padding.Empty
            };
            _scrollHost.Controls.Add(stack);
            // Keep stack width in sync with the scroll host (minus scroll bar)
            _scrollHost.SizeChanged += (s, e) => stack.Width = _scrollHost.ClientSize.Width;

            // ── Theme card ────────────────────────────────────────────────
            _themeCard = MakeCard(Localization.T("Settings.Theme"));
            var themeRow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                AutoSize      = true,
                Padding       = new Padding(0, 6, 0, 0)
            };
            _btnLight = MakeThemeBtn("☀  " + Localization.T("Menu.Theme.Light"), AppTheme.Light);
            _btnDark  = MakeThemeBtn("🌙  " + Localization.T("Menu.Theme.Dark"),  AppTheme.Dark);
            themeRow.Controls.Add(_btnLight);
            themeRow.Controls.Add(_btnDark);
            _themeCard.Controls.Add(themeRow);
            stack.Controls.Add(_themeCard);

            // ── Language card ─────────────────────────────────────────────
            _langCard = MakeCard(Localization.T("Settings.Language"));
            var langRow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents  = true,
                AutoSize      = true,
                Padding       = new Padding(0, 6, 0, 0)
            };
            foreach (var (code, label) in new[]
            {
                ("en",     "English"),
                ("zh-CN",  "简体中文"),
                ("zh-Hant","繁體中文"),
                ("ja",     "日本語"),
                ("ko",     "한국어"),
                ("ru",     "Русский"),
                ("pt-BR",  "Português (BR)")
            })
            {
                var capturedCode = code;
                var btn = new winC2D.UI.ModernButton
                {
                    Text   = label,
                    Style  = winC2D.UI.ModernButton.ButtonStyle.Ghost,
                    Width  = 140,
                    Height = 34,
                    Margin = new Padding(0, 0, 8, 8)
                };
                btn.Click += (s, e) =>
                {
                    Localization.SetLanguage(capturedCode);
                    RefreshLocalization();
                };
                langRow.Controls.Add(btn);
            }
            _langCard.Controls.Add(langRow);
            stack.Controls.Add(_langCard);

            // ── About card ────────────────────────────────────────────────
            _aboutCard = MakeCard(Localization.T("Settings.About"));
            var aboutFlow = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents  = false,
                AutoSize      = true,
                Padding       = new Padding(0, 6, 0, 0)
            };
            aboutFlow.Controls.Add(new Label
            {
                AutoSize  = true,
                Text      = Localization.T("About.Description"),
                Font      = new Font("Segoe UI Variable", 9.5f),
                Margin    = new Padding(0, 0, 0, 2)
            });
            aboutFlow.Controls.Add(new Label
            {
                AutoSize  = true,
                Text      = Localization.T("About.Copyright"),
                Font      = new Font("Segoe UI Variable", 9f)
            });
            _aboutCard.Controls.Add(aboutFlow);
            stack.Controls.Add(_aboutCard);
        }

        // ════════════════════════════════════════════════════════════════════
        // Helpers
        // ════════════════════════════════════════════════════════════════════
        private winC2D.UI.ModernButton MakeThemeBtn(string text, AppTheme theme)
        {
            var btn = new winC2D.UI.ModernButton
            {
                Text   = text,
                Style  = ThemeManager.CurrentTheme == theme
                    ? winC2D.UI.ModernButton.ButtonStyle.Accent
                    : winC2D.UI.ModernButton.ButtonStyle.Default,
                Width  = 160,
                Height = 36,
                Margin = new Padding(0, 0, 10, 0)
            };
            btn.Click += (s, e) => ThemeManager.SetTheme(theme);
            return btn;
        }

        // Creates a self-sizing card with a bold title label.
        // Callers add content controls directly to card.Controls.
        private static winC2D.UI.CardPanel MakeCard(string title)
        {
            var card = new winC2D.UI.CardPanel
            {
                AutoSize     = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                CornerRadius = 8,
                Padding      = new Padding(16, 10, 16, 14),
                Margin       = new Padding(0, 0, 0, 12)
            };
            card.Controls.Add(new Label
            {
                Text      = title,
                Dock      = DockStyle.Top,
                AutoSize  = false,
                Height    = 26,
                Font      = new Font("Segoe UI Variable", 10f, FontStyle.Bold),
                ForeColor = ThemeManager.Current.Foreground
            });
            return card;
        }

        // ════════════════════════════════════════════════════════════════════
        // Theme and localization callbacks
        // ════════════════════════════════════════════════════════════════════
        private void RefreshThemeButtons()
        {
            if (InvokeRequired) { Invoke(new Action(RefreshThemeButtons)); return; }
            _btnLight.Style = ThemeManager.CurrentTheme == AppTheme.Light
                ? winC2D.UI.ModernButton.ButtonStyle.Accent
                : winC2D.UI.ModernButton.ButtonStyle.Default;
            _btnDark.Style = ThemeManager.CurrentTheme == AppTheme.Dark
                ? winC2D.UI.ModernButton.ButtonStyle.Accent
                : winC2D.UI.ModernButton.ButtonStyle.Default;
            _btnLight.Invalidate();
            _btnDark.Invalidate();
        }

        public void ApplyTheme(ThemePalette p)
        {
            BackColor = p.Background;
            ForeColor = p.Foreground;
            if (_scrollHost != null) _scrollHost.BackColor = p.Background;
            Invalidate(true);
        }

        public void RefreshLocalization()
        {
            _header.Title    = Localization.T("Nav.Settings");
            _header.Subtitle = Localization.T("Desc.Settings");
            _header.Invalidate();
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // LogPage — Migration log viewer
    // ══════════════════════════════════════════════════════════════════════════
    public class LogPage : Panel
    {
        private readonly winC2D.Core.MigrationEngineLogger _logger;
        private PageHeader _header;
        private RichTextBox _logBox;
        private winC2D.UI.ModernButton _btnClear;
        private winC2D.UI.ModernButton _btnSave;

        public LogPage(winC2D.Core.MigrationEngineLogger logger)
        {
            _logger = logger;
            BuildUI();
            _logger.EntryAdded += OnEntryAdded;
        }

        private void BuildUI()
        {
            var layout = new TableLayoutPanel
            {
                Dock        = DockStyle.Fill,
                RowCount    = 3,
                ColumnCount = 1,
                Padding     = Padding.Empty,
                Margin      = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            Controls.Add(layout);

            _header = new PageHeader
            {
                Dock     = DockStyle.Fill,
                Title    = Localization.T("Nav.Log"),
                Subtitle = ""
            };
            layout.Controls.Add(_header, 0, 0);

            _logBox = new RichTextBox
            {
                Dock        = DockStyle.Fill,
                ReadOnly    = true,
                BackColor   = ThemeManager.Current.SurfaceBackground,
                ForeColor   = ThemeManager.Current.Foreground,
                BorderStyle = BorderStyle.None,
                Font        = new Font("Cascadia Mono", 9.5f, FontStyle.Regular),
                ScrollBars  = RichTextBoxScrollBars.Vertical
            };
            layout.Controls.Add(_logBox, 0, 1);

            var btnBar = new FlowLayoutPanel
            {
                Dock          = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding       = new Padding(0, 6, 0, 0)
            };
            _btnClear = new winC2D.UI.ModernButton
            {
                Text   = Localization.T("Button.ClearLog"),
                Style  = winC2D.UI.ModernButton.ButtonStyle.Ghost,
                Width  = 120,
                Height = 34,
                Margin = new Padding(0, 0, 8, 0)
            };
            _btnSave = new winC2D.UI.ModernButton
            {
                Text   = Localization.T("Button.SaveLog"),
                Style  = winC2D.UI.ModernButton.ButtonStyle.Default,
                Width  = 120,
                Height = 34
            };
            _btnClear.Click += (s, e) => _logBox.Clear();
            _btnSave.Click  += SaveLog_Click;
            btnBar.Controls.Add(_btnClear);
            btnBar.Controls.Add(_btnSave);
            layout.Controls.Add(btnBar, 0, 2);
        }

        private void OnEntryAdded(object? sender, string entry)
        {
            if (InvokeRequired) { Invoke(new Action(() => AppendLog(entry))); return; }
            AppendLog(entry);
        }

        private void AppendLog(string entry)
        {
            var p = ThemeManager.Current;
            Color color;
            if (entry.Contains("[ERR]") || entry.Contains("[ROLLBACK]"))
                color = p.StatusError;
            else if (entry.Contains("[OK]"))
                color = p.StatusSuccess;
            else
                color = p.ForegroundMuted;

            _logBox.SelectionColor = color;
            _logBox.AppendText(entry + "\n");
            _logBox.ScrollToCaret();
        }

        private void SaveLog_Click(object? sender, EventArgs e)
        {
            using var dlg = new SaveFileDialog
            {
                Filter   = "Text Files|*.txt|All Files|*.*",
                FileName = $"winC2D_log_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
            };
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                try { _logger.SaveToFile(dlg.FileName); }
                catch (Exception ex)
                {
                    LocalizedMessageBox.Show(ex.Message, Localization.T("Title.Error"),
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public void ApplyTheme(ThemePalette p)
        {
            BackColor         = p.Background;
            ForeColor         = p.Foreground;
            _logBox.BackColor = p.SurfaceBackground;
            _logBox.ForeColor = p.Foreground;
            Invalidate(true);
        }
    }
}
