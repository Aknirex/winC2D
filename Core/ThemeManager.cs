using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

namespace winC2D.Core
{
    // ──────────────────────────────────────────────
    // 主题枚举
    // ──────────────────────────────────────────────
    public enum AppTheme { Light, Dark, Custom }

    // ──────────────────────────────────────────────
    // 完整调色板（涵盖所有控件场景）
    // ──────────────────────────────────────────────
    public sealed class ThemePalette
    {
        // ── 窗体 / 背景 ──────────────────────────
        public Color Background        { get; set; }   // 主背景
        public Color SurfaceBackground { get; set; }   // 卡片/面板背景
        public Color SidebarBackground { get; set; }   // 侧边栏背景

        // ── 前景 / 文字 ──────────────────────────
        public Color Foreground        { get; set; }   // 主文字
        public Color ForegroundMuted   { get; set; }   // 次要文字/标签
        public Color ForegroundDisabled{ get; set; }   // 禁用状态文字

        // ── 强调色 ───────────────────────────────
        public Color Accent            { get; set; }   // 主强调色
        public Color AccentHover       { get; set; }   // 强调悬浮色
        public Color AccentPressed     { get; set; }   // 强调按下色
        public Color AccentForeground  { get; set; }   // 强调色上的文字颜色

        // ── 侧边栏导航 ───────────────────────────
        public Color NavItemHover      { get; set; }
        public Color NavItemSelected   { get; set; }
        public Color NavItemSelectedFg { get; set; }
        public Color NavItemFg         { get; set; }

        // ── 按钮 ─────────────────────────────────
        public Color ButtonBackground  { get; set; }
        public Color ButtonForeground  { get; set; }
        public Color ButtonBorder      { get; set; }
        public Color ButtonHover       { get; set; }
        public Color ButtonPressed     { get; set; }

        // ── 输入框 ───────────────────────────────
        public Color InputBackground   { get; set; }
        public Color InputForeground   { get; set; }
        public Color InputBorder       { get; set; }
        public Color InputBorderFocus  { get; set; }

        // ── 分隔线 / 边框 ─────────────────────────
        public Color Separator         { get; set; }
        public Color CardBorder        { get; set; }

        // ── 菜单 ─────────────────────────────────
        public Color MenuBackground    { get; set; }
        public Color MenuForeground    { get; set; }
        public Color MenuItemHover     { get; set; }
        public Color MenuItemSelected  { get; set; }
        public Color MenuBorder        { get; set; }

        // ── ListView（完全自绘，彻底解决暗色适配）──
        public Color ListBackground    { get; set; }
        public Color ListForeground    { get; set; }
        public Color ListHeaderBg      { get; set; }
        public Color ListHeaderFg      { get; set; }
        public Color ListRowOdd        { get; set; }
        public Color ListRowEven       { get; set; }
        public Color ListRowHover      { get; set; }
        public Color ListRowSelected   { get; set; }
        public Color ListRowSelectedFg { get; set; }
        public Color ListGridLine      { get; set; }

        // ── 状态色 ───────────────────────────────
        public Color StatusNormal      { get; set; }
        public Color StatusSymlink     { get; set; }
        public Color StatusSuspicious  { get; set; }
        public Color StatusEmpty       { get; set; }
        public Color StatusResidual    { get; set; }
        public Color StatusSuccess     { get; set; }
        public Color StatusError       { get; set; }
        public Color StatusWarning     { get; set; }

        // ── 进度条 ───────────────────────────────
        public Color ProgressBackground{ get; set; }
        public Color ProgressFill      { get; set; }

        // ── 滚动条（依赖 SetWindowTheme，此处存主题名）──
        public string ScrollbarThemeName { get; set; }

        // ── 旧版别名（向后兼容 MainForm / LogForm / ScanPathsForm）──────────
        public Color FormBackground              => Background;
        public Color TabControlBackground        => Background;
        public Color TabPageBackground           => SurfaceBackground;
        public Color ControlBackground           => SurfaceBackground;
        public Color ListViewBackground          => ListBackground;
        public Color ListViewForeground          => ListForeground;
        public Color ListViewHeaderBackground    => ListHeaderBg;
        public Color ListViewHeaderForeground    => ListHeaderFg;
        public Color ListViewRowBackground       => ListRowOdd;
        public Color ListViewRowAlternateBackground => ListRowEven;
        public Color ListViewRowHoverBackground  => ListRowHover;
        public Color ListViewRowSelectedBackground => ListRowSelected;
        public Color ListViewGridColor           => ListGridLine;
    }

    // ──────────────────────────────────────────────
    // ThemeManager v2
    // ──────────────────────────────────────────────
    public static class ThemeManager
    {
        private const string ConfigFileName = "theme2.json";
        private static AppTheme _currentTheme = AppTheme.Light;
        private static ThemePalette _customPalette;

        public static event EventHandler ThemeChanged;
        public static AppTheme CurrentTheme => _currentTheme;

        // ── 内置调色板 ─────────────────────────────────────────────────────
        public static ThemePalette LightPalette { get; } = BuildLight();
        public static ThemePalette DarkPalette  { get; } = BuildDark();

        private static ThemePalette BuildLight() => new ThemePalette
        {
            // ── Win11 Fluent 2 Light ──────────────────────────────────────────
            // Background layers: Mica-style page bg → card surface → sidebar
            Background         = Color.FromArgb(243, 243, 243),   // #F3F3F3  WinUI LayerOnMicaBase
            SurfaceBackground  = Color.FromArgb(255, 255, 255),   // #FFFFFF  CardBackgroundFillColorDefault
            SidebarBackground  = Color.FromArgb(238, 238, 238),   // #EEEEEE  slightly darker layer

            // Text
            Foreground         = Color.FromArgb(28,  28,  28),    // #1C1C1C  TextFillColorPrimary
            ForegroundMuted    = Color.FromArgb(95,  95,  95),    // #5F5F5F  TextFillColorSecondary
            ForegroundDisabled = Color.FromArgb(160, 160, 160),   // #A0A0A0  TextFillColorDisabled

            // System blue accent  (matches Windows default #0067C0)
            Accent             = Color.FromArgb(0,   103, 192),
            AccentHover        = Color.FromArgb(0,    88, 168),
            AccentPressed      = Color.FromArgb(0,    70, 140),
            AccentForeground   = Color.White,

            // Sidebar nav items
            NavItemHover       = Color.FromArgb(20,  0, 0, 0),    // ~8% black overlay
            NavItemSelected    = Color.FromArgb(28,  0, 103, 192),// 11% accent overlay
            NavItemSelectedFg  = Color.FromArgb(0,   103, 192),
            NavItemFg          = Color.FromArgb(28,  28,  28),

            // Buttons — WinUI ControlFillColorDefault = #FDFDFD / stroke #0000000E+#00000029
            ButtonBackground   = Color.FromArgb(253, 253, 253),
            ButtonForeground   = Color.FromArgb(28,  28,  28),
            ButtonBorder       = Color.FromArgb(41,  0,   0, 0),  // ControlStrokeColorDefault ~16% black
            ButtonHover        = Color.FromArgb(246, 246, 246),   // ControlFillColorSecondary
            ButtonPressed      = Color.FromArgb(240, 240, 240),   // ControlFillColorTertiary

            // Text input
            InputBackground    = Color.FromArgb(255, 255, 255),
            InputForeground    = Color.FromArgb(28,  28,  28),
            InputBorder        = Color.FromArgb(138, 138, 138),   // TextControlElevationBorderBrush ~54%
            InputBorderFocus   = Color.FromArgb(0,   103, 192),

            // Dividers / card stroke
            Separator          = Color.FromArgb(229, 229, 229),   // DividerStrokeColorDefault
            CardBorder         = Color.FromArgb(20,  0,   0, 0),  // CardStrokeColorDefault  ~8% black

            // Menus / flyouts
            MenuBackground     = Color.FromArgb(253, 253, 253),
            MenuForeground     = Color.FromArgb(28,  28,  28),
            MenuItemHover      = Color.FromArgb(242, 242, 242),
            MenuItemSelected   = Color.FromArgb(224, 238, 253),
            MenuBorder         = Color.FromArgb(200, 200, 200),

            // ListView
            ListBackground     = Color.FromArgb(255, 255, 255),
            ListForeground     = Color.FromArgb(28,  28,  28),
            ListHeaderBg       = Color.FromArgb(246, 246, 246),
            ListHeaderFg       = Color.FromArgb(80,  80,  80),
            ListRowOdd         = Color.FromArgb(255, 255, 255),
            ListRowEven        = Color.FromArgb(250, 250, 250),
            ListRowHover       = Color.FromArgb(235, 243, 255),
            ListRowSelected    = Color.FromArgb(210, 232, 255),
            ListRowSelectedFg  = Color.FromArgb(0,   70,  140),
            ListGridLine       = Color.FromArgb(229, 229, 229),

            // Status colours (clear, readable on white)
            StatusNormal       = Color.FromArgb(16,  124, 16),
            StatusSymlink      = Color.FromArgb(0,   103, 192),
            StatusSuspicious   = Color.FromArgb(157, 93,  0),
            StatusEmpty        = Color.FromArgb(140, 140, 140),
            StatusResidual     = Color.FromArgb(196, 43,  28),
            StatusSuccess      = Color.FromArgb(16,  124, 16),
            StatusError        = Color.FromArgb(196, 43,  28),
            StatusWarning      = Color.FromArgb(157, 93,  0),

            ProgressBackground = Color.FromArgb(214, 214, 214),
            ProgressFill       = Color.FromArgb(0,   103, 192),

            ScrollbarThemeName = "Explorer"
        };

        private static ThemePalette BuildDark() => new ThemePalette
        {
            // ── Win11 Fluent 2 Dark ───────────────────────────────────────────
            // Background layers: #202020 → #2C2C2C → #1C1C1C  (3-level depth)
            Background         = Color.FromArgb(32,  32,  32),    // #202020  SolidBackgroundFillColorBase
            SurfaceBackground  = Color.FromArgb(44,  44,  44),    // #2C2C2C  LayerFillColorDefault
            SidebarBackground  = Color.FromArgb(28,  28,  28),    // #1C1C1C  darker than base

            // Text
            Foreground         = Color.FromArgb(255, 255, 255),   // #FFFFFF  TextFillColorPrimary (dark)
            ForegroundMuted    = Color.FromArgb(162, 162, 162),   // #A2A2A2  TextFillColorSecondary
            ForegroundDisabled = Color.FromArgb(90,  90,  90),    // #5A5A5A  TextFillColorDisabled

            // System accent: #60CDFF (Win11 dark-mode accent default)
            Accent             = Color.FromArgb(96,  205, 255),   // #60CDFF
            AccentHover        = Color.FromArgb(122, 217, 255),   // slightly brighter
            AccentPressed      = Color.FromArgb(72,  180, 232),   // slightly darker
            AccentForeground   = Color.FromArgb(0,   25,  40),    // dark text on light accent

            // Sidebar nav items — white overlays
            NavItemHover       = Color.FromArgb(18,  255, 255, 255), // ~7% white overlay
            NavItemSelected    = Color.FromArgb(28,  96,  205, 255), // 11% accent overlay
            NavItemSelectedFg  = Color.FromArgb(96,  205, 255),
            NavItemFg          = Color.FromArgb(242, 242, 242),

            // Buttons — WinUI dark ControlFillColorDefault = #FFFFFF0F / stroke = #FFFFFF12+#00000029
            ButtonBackground   = Color.FromArgb(56,  56,  56),    // SubtleFillColorSecondary approx
            ButtonForeground   = Color.FromArgb(255, 255, 255),
            ButtonBorder       = Color.FromArgb(55,  255, 255, 255), // ~22% white top stroke (averaged)
            ButtonHover        = Color.FromArgb(66,  66,  66),    // ControlFillColorSecondary
            ButtonPressed      = Color.FromArgb(46,  46,  46),    // ControlFillColorTertiary

            // Text input
            InputBackground    = Color.FromArgb(48,  48,  48),
            InputForeground    = Color.FromArgb(255, 255, 255),
            InputBorder        = Color.FromArgb(100, 100, 100),
            InputBorderFocus   = Color.FromArgb(96,  205, 255),

            // Dividers / card stroke — subtle white overlay
            Separator          = Color.FromArgb(55,  55,  55),
            CardBorder         = Color.FromArgb(30,  255, 255, 255), // ~12% white overlay

            // Menus / flyouts
            MenuBackground     = Color.FromArgb(44,  44,  44),
            MenuForeground     = Color.FromArgb(255, 255, 255),
            MenuItemHover      = Color.FromArgb(60,  60,  60),
            MenuItemSelected   = Color.FromArgb(72,  72,  72),
            MenuBorder         = Color.FromArgb(68,  68,  68),

            // ListView
            ListBackground     = Color.FromArgb(40,  40,  40),
            ListForeground     = Color.FromArgb(242, 242, 242),
            ListHeaderBg       = Color.FromArgb(34,  34,  34),
            ListHeaderFg       = Color.FromArgb(162, 162, 162),
            ListRowOdd         = Color.FromArgb(42,  42,  42),
            ListRowEven        = Color.FromArgb(46,  46,  46),
            ListRowHover       = Color.FromArgb(56,  70,  86),
            ListRowSelected    = Color.FromArgb(40,  82, 120),
            ListRowSelectedFg  = Color.FromArgb(210, 235, 255),
            ListGridLine       = Color.FromArgb(52,  52,  52),

            // Status colours (vivid enough on dark bg)
            StatusNormal       = Color.FromArgb(108, 203, 95),    // green
            StatusSymlink      = Color.FromArgb(96,  205, 255),   // accent blue
            StatusSuspicious   = Color.FromArgb(252, 183, 36),    // amber — NOT yellow-green
            StatusEmpty        = Color.FromArgb(130, 130, 130),
            StatusResidual     = Color.FromArgb(255, 99,  99),
            StatusSuccess      = Color.FromArgb(108, 203, 95),
            StatusError        = Color.FromArgb(255, 99,  99),
            StatusWarning      = Color.FromArgb(252, 183, 36),

            ProgressBackground = Color.FromArgb(62,  62,  62),
            ProgressFill       = Color.FromArgb(96,  205, 255),

            ScrollbarThemeName = "DarkMode_Explorer"
        };

        // ── 获取当前调色板 ─────────────────────────────────────────────────
        public static ThemePalette Current => GetPalette(_currentTheme);

        public static ThemePalette GetPalette(AppTheme theme) => theme switch
        {
            AppTheme.Dark   => DarkPalette,
            AppTheme.Custom => _customPalette ?? LightPalette,
            _               => LightPalette
        };

        // ── 切换主题 ───────────────────────────────────────────────────────
        public static void SetTheme(AppTheme theme, ThemePalette customPalette = null)
        {
            _currentTheme  = theme;
            _customPalette = theme == AppTheme.Custom ? customPalette : null;
            ApplyNativeScrollbar(theme == AppTheme.Dark);
            ApplyTitleBarDarkMode(theme == AppTheme.Dark);
            SavePreference();
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }

        // ── 加载偏好 ───────────────────────────────────────────────────────
        public static void LoadPreference()
        {
            try
            {
                string path = GetConfigPath();
                if (!File.Exists(path)) return;
                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.TryGetProperty("theme", out var t) &&
                    Enum.TryParse<AppTheme>(t.GetString(), true, out var parsed))
                {
                    _currentTheme = parsed;
                }
            }
            catch { }
            ApplyNativeScrollbar(_currentTheme == AppTheme.Dark);
        }

        // ── Win11 原生滚动条黑暗模式 ───────────────────────────────────────
        [DllImport("uxtheme.dll", EntryPoint = "#135", CharSet = CharSet.Unicode, PreserveSig = false)]
        private static extern void SetPreferredAppMode(int appMode);

        public static void ApplyNativeScrollbar(bool dark)
        {
            try { SetPreferredAppMode(dark ? 2 : 0); } catch { }
        }

        // ── DWM 标题栏暗色 ─────────────────────────────────────────────────
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int val, int size);
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        public static void ApplyTitleBarDarkMode(bool dark)
        {
            // 将在窗体 Handle 创建后调用
        }

        public static void ApplyTitleBarToWindow(IntPtr hwnd, bool dark)
        {
            if (Environment.OSVersion.Version >= new Version(10, 0, 18985))
            {
                int val = dark ? 1 : 0;
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref val, sizeof(int));
            }
        }

        // ── SetWindowTheme（ListView 等原生控件暗色支持）────────────────────
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hwnd, string sub, string idList);

        // ── 保存偏好 ───────────────────────────────────────────────────────
        private static void SavePreference()
        {
            try
            {
                var obj = new { theme = _currentTheme.ToString() };
                File.WriteAllText(GetConfigPath(),
                    JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch { }
        }

        private static string GetConfigPath() =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigFileName);
    }
}
