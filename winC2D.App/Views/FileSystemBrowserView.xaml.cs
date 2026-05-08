using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using winC2D.Core.Models;
using winC2D.App.ViewModels;

namespace winC2D.App.Views;

/// <summary>
/// Code-behind for the FileSystemBrowserView.
/// Handles double-click on DataGrid rows for directory navigation
/// and triggers ViewModel initialisation on load.
/// </summary>
public partial class FileSystemBrowserView : UserControl
{
    private readonly FileSystemBrowserViewModel _viewModel;
    private bool _isLoaded;

    public FileSystemBrowserView(FileSystemBrowserViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoaded) return;
        _isLoaded = true;

        await _viewModel.LoadAsync();
    }

    /// <summary>
    /// Navigate into a directory when the user double-clicks a directory row.
    /// </summary>
    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid) return;
        if (grid.SelectedItem is not FileSystemItem item) return;
        if (!item.IsDirectory) return;

        _viewModel.NavigateItemCommand.Execute(item);
    }
}
