using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using winC2D.App.ViewModels;

namespace winC2D.App.Views;

/// <summary>
/// Interaction logic for LogView.xaml
/// </summary>
public partial class LogView : UserControl
{
    private readonly LogViewModel _viewModel;

    public LogView(LogViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        Loaded += LogView_Loaded;
    }

    private async void LogView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_viewModel.LoadLogsCommand.IsRunning)
            return;

        await _viewModel.LoadLogsCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// When the user clicks a row (including the checkbox), select that entry.
    /// </summary>
    private void LogDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not DataGrid grid) return;

        var dep = e.OriginalSource as DependencyObject;
        while (dep != null && dep is not DataGridRow)
            dep = System.Windows.Media.VisualTreeHelper.GetParent(dep);

        if (dep is DataGridRow row && row.Item is MigrationLogEntry entry
            && entry.CanRollback)
        {
            _viewModel.SelectedEntry = entry;
        }
    }
}
