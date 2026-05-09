using System;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Controls;
using winC2D.App.ViewModels;

namespace winC2D.App.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly IServiceProvider _serviceProvider;

    private const double MinPaneWidth = 180;
    private const double MaxPaneWidth = 500;
    private const double ResizeGripWidth = 6;

    private bool _isResizing;
    private System.Windows.Point _resizeStartPoint;
    private double _resizeStartWidth;

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

        // 为导航栏添加鼠标拖拽调整宽度支持（使用 Preview 隧道事件避免被子元素拦截）
        RootNavigation.PreviewMouseMove += RootNavigation_PreviewMouseMove;
        RootNavigation.PreviewMouseLeftButtonDown += RootNavigation_PreviewMouseLeftButtonDown;
        RootNavigation.PreviewMouseLeftButtonUp += RootNavigation_PreviewMouseLeftButtonUp;
        RootNavigation.MouseLeave += RootNavigation_MouseLeave;
    }

    /// <summary>
    /// 判断鼠标是否处于可拖拽区域（导航栏右边缘 ±ResizeGripWidth）。
    /// </summary>
    private bool IsInResizeZone(System.Windows.Point mousePos)
    {
        double paneEdge = RootNavigation.OpenPaneLength;
        return mousePos.X >= paneEdge - ResizeGripWidth
            && mousePos.X <= paneEdge + ResizeGripWidth
            && RootNavigation.IsPaneOpen;
    }

    private void RootNavigation_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        var pos = e.GetPosition(RootNavigation);

        if (_isResizing)
        {
            var delta = pos.X - _resizeStartPoint.X;
            var newWidth = _resizeStartWidth + delta;
            if (newWidth < MinPaneWidth) newWidth = MinPaneWidth;
            if (newWidth > MaxPaneWidth) newWidth = MaxPaneWidth;
            RootNavigation.OpenPaneLength = newWidth;
            return;
        }

        // 在拖拽区域内显示水平调整光标
        RootNavigation.Cursor = IsInResizeZone(pos)
            ? System.Windows.Input.Cursors.SizeWE
            : null;
    }

    private void RootNavigation_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(RootNavigation);
        if (IsInResizeZone(pos))
        {
            _isResizing = true;
            _resizeStartPoint = pos;
            _resizeStartWidth = RootNavigation.OpenPaneLength;
            RootNavigation.CaptureMouse();
            e.Handled = true;
        }
    }

    private void RootNavigation_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_isResizing)
        {
            _isResizing = false;
            RootNavigation.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void RootNavigation_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (!_isResizing)
        {
            RootNavigation.Cursor = null;
        }
    }
}
