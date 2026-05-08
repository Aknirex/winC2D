using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using winC2D.App.Helpers;
using winC2D.App.ViewModels;
using winC2D.Core.Models;

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

        // Auto-fit DataGrid columns after navigation / refresh completes.
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
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

    /// <summary>
    /// When the ViewModel signals that navigation content has changed
    /// (CurrentPath is updated), auto-fit the DataGrid columns.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileSystemBrowserViewModel.CurrentPath))
        {
            // Defer to allow the DataGrid to finish binding and layout.
            Dispatcher.BeginInvoke(
                () => DataGridAutoFitHelper.AutoFitColumns(FileSystemDataGrid),
                DispatcherPriority.Loaded);
        }
    }
}
