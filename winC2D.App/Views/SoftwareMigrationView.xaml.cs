using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;
using System.Windows.Threading;
using winC2D.App.Helpers;
using winC2D.App.ViewModels;
using winC2D.Core.Models;

namespace winC2D.App.Views;

/// <summary>
/// Interaction logic for SoftwareMigrationView.xaml
/// </summary>
public partial class SoftwareMigrationView : UserControl
{
    private readonly SoftwareMigrationViewModel _viewModel;

    public SoftwareMigrationView(SoftwareMigrationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // Auto-fit DataGrid columns after scan/refresh completes.
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>
    /// Detect which row was right-clicked and store it in the ViewModel so the
    /// DataGrid.ContextMenu can reach it via PlacementTarget.DataContext.RightClickedItem.
    /// </summary>
    private void SoftwareDataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not SoftwareMigrationViewModel vm) return;

        // Walk up from the clicked element until we find a DataGridRow
        var dep = e.OriginalSource as System.Windows.DependencyObject;
        while (dep != null && dep is not DataGridRow)
            dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);

        vm.RightClickedItem = dep is DataGridRow row
            ? row.Item as SoftwareInfo
            : null;
    }

    private void SoftwareDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (e.Column.SortMemberPath != nameof(SoftwareInfo.SizeBytes))
            return;

        e.Handled = true;

        var nextDirection = e.Column.SortDirection == ListSortDirection.Descending
            ? ListSortDirection.Ascending
            : ListSortDirection.Descending;

        foreach (var column in SoftwareDataGrid.Columns)
            column.SortDirection = null;

        e.Column.SortDirection = nextDirection;

        var view = CollectionViewSource.GetDefaultView(SoftwareDataGrid.ItemsSource);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(nameof(SoftwareInfo.SizeBytes), nextDirection));
        view.Refresh();
    }

    /// <summary>
    /// When the ViewModel finishes scanning (IsScanning goes false),
    /// auto-fit the DataGrid columns to their content.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SoftwareMigrationViewModel.IsScanning)
            && !_viewModel.IsScanning)
        {
            // Defer to allow the DataGrid to finish binding and layout.
            Dispatcher.BeginInvoke(
                () => DataGridAutoFitHelper.AutoFitColumns(SoftwareDataGrid),
                DispatcherPriority.Loaded);
        }
    }
}
