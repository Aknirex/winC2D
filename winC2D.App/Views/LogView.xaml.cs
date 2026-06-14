using System.Windows.Controls;
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

    private async void LogView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_viewModel.LoadLogsCommand.IsRunning)
            return;

        await _viewModel.LoadLogsCommand.ExecuteAsync(null);
    }
}
