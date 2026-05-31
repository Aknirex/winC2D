using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using winC2D.App.Helpers;
using winC2D.App.ViewModels;

namespace winC2D.App.Views;

/// <summary>
/// Interaction logic for AppDataMigrationView.xaml
/// </summary>
public partial class AppDataMigrationView : UserControl
{
    private readonly AppDataMigrationViewModel _viewModel;

    public AppDataMigrationView(AppDataMigrationViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        // Auto-fit DataGrid columns after scan completes.
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void CheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount > 1)
            e.Handled = true;
    }

    private void AppDataDataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (e.Column.SortMemberPath != nameof(AppDataInfo.SizeBytes))
            return;

        e.Handled = true;

        var nextDirection = e.Column.SortDirection == ListSortDirection.Descending
            ? ListSortDirection.Ascending
            : ListSortDirection.Descending;

        foreach (var column in AppDataDataGrid.Columns)
            column.SortDirection = null;

        e.Column.SortDirection = nextDirection;

        var view = CollectionViewSource.GetDefaultView(AppDataDataGrid.ItemsSource);
        view.SortDescriptions.Clear();
        view.SortDescriptions.Add(new SortDescription(nameof(AppDataInfo.SizeBytes), nextDirection));
        view.Refresh();
    }

    /// <summary>
    /// When the ViewModel finishes scanning (IsScanning goes false),
    /// auto-fit the DataGrid columns to their content.
    /// </summary>
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppDataMigrationViewModel.IsScanning)
            && !_viewModel.IsScanning)
        {
            // Defer to allow the DataGrid to finish binding and layout.
            Dispatcher.BeginInvoke(
                () => DataGridAutoFitHelper.AutoFitColumns(AppDataDataGrid),
                DispatcherPriority.Loaded);
        }
    }
}
