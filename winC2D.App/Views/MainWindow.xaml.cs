using System;
using System.Windows;
using Wpf.Ui.Controls;
using winC2D.App.ViewModels;

namespace winC2D.App.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : FluentWindow
{
    private readonly IServiceProvider _serviceProvider;

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

        // 导航到默认页
        RootNavigation.Navigate(typeof(SoftwareMigrationView));
    }
}
