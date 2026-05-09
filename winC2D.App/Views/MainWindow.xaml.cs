using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Wpf.Ui.Controls;
using winC2D.App.ViewModels;

namespace winC2D.App.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly IServiceProvider _serviceProvider;
    private Thumb? _paneResizeThumb;
    private Grid? _paneRoot;

    private const double MinPaneWidth = 180;
    private const double MaxPaneWidth = 500;

    public MainWindow(MainViewModel viewModel, IServiceProvider serviceProvider)
    {
        InitializeComponent();
        DataContext = viewModel;

        _serviceProvider = serviceProvider;

        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        // 将 DI 容器挂到 NavigationView，使其能解析页面实例
        RootNavigation.SetServiceProvider(_serviceProvider);

        // 导航到默认页 — Explorer 视图
        RootNavigation.Navigate(typeof(FileSystemBrowserView));

        // 为导航栏添加可拖拽调整宽度的 Thumb
        AddPaneResizeThumb();
    }

    /// <summary>
    /// 在 NavigationView 的内部 PaneRoot 右侧添加可拖拽调整宽度的 Thumb。
    /// </summary>
    private void AddPaneResizeThumb()
    {
        // 确保模板已应用
        RootNavigation.ApplyTemplate();

        _paneRoot = RootNavigation.Template.FindName("PART_PaneRoot", RootNavigation) as Grid;
        if (_paneRoot is null) return;

        _paneResizeThumb = new Thumb
        {
            Width = 6,
            Cursor = Cursors.SizeWE,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Stretch,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(0, 0, -3, 0),
        };

        // 给 Thumb 添加一条细竖线作为视觉提示（悬停时高亮）
        _paneResizeThumb.Template = CreateResizeThumbTemplate();

        _paneResizeThumb.DragDelta += OnPaneResizeDragDelta;

        // 确保 Thumb 跨越 Grid 所有行，且在最顶层
        Grid.SetRowSpan(_paneResizeThumb, 999);
        Panel.SetZIndex(_paneResizeThumb, 1000);

        _paneRoot.Children.Add(_paneResizeThumb);
    }

    /// <summary>
    /// 创建 Thumb 模板：中间一条细竖线作为拖拽手柄的视觉提示。
    /// </summary>
    private static ControlTemplate CreateResizeThumbTemplate()
    {
        var template = new ControlTemplate(typeof(Thumb));
        var fef = new FrameworkElementFactory(typeof(Border));
        fef.SetValue(Border.BackgroundProperty, Brushes.Transparent);

        var gridFef = new FrameworkElementFactory(typeof(Grid));
        gridFef.SetValue(Grid.HorizontalAlignmentProperty, HorizontalAlignment.Center);

        var lineFef = new FrameworkElementFactory(typeof(Border));
        lineFef.SetValue(Border.WidthProperty, 1.0);
        lineFef.SetValue(Border.VerticalAlignmentProperty, VerticalAlignment.Stretch);
        lineFef.SetValue(Border.BackgroundProperty, new SolidColorBrush(Color.FromArgb(0x40, 0x80, 0x80, 0x80)));
        lineFef.SetValue(Border.MarginProperty, new Thickness(0, 8, 0, 8));
        lineFef.SetValue(Border.CornerRadiusProperty, new CornerRadius(0.5));

        gridFef.AppendChild(lineFef);
        fef.AppendChild(gridFef);
        template.VisualTree = fef;
        return template;
    }

    /// <summary>
    /// 拖拽 Thumb 时动态调整导航栏宽度。
    /// </summary>
    private void OnPaneResizeDragDelta(object sender, DragDeltaEventArgs e)
    {
        var newWidth = RootNavigation.OpenPaneLength + e.HorizontalChange;
        if (newWidth < MinPaneWidth) newWidth = MinPaneWidth;
        if (newWidth > MaxPaneWidth) newWidth = MaxPaneWidth;
        RootNavigation.OpenPaneLength = newWidth;
    }
}
