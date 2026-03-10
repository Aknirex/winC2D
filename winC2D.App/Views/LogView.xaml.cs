using System.Windows.Controls;
using winC2D.App.ViewModels;

namespace winC2D.App.Views;

/// <summary>
/// Interaction logic for LogView.xaml
/// </summary>
public partial class LogView : UserControl
{
    public LogView(LogViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}