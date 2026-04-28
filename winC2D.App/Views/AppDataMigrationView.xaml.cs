using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using winC2D.App.ViewModels;

namespace winC2D.App.Views;

/// <summary>
/// Interaction logic for AppDataMigrationView.xaml
/// </summary>
public partial class AppDataMigrationView : UserControl
{
    public AppDataMigrationView(AppDataMigrationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
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
}
