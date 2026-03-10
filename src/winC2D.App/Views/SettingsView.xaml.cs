using System.Windows.Controls;
using winC2D.App.ViewModels;

namespace winC2D.App.Views;

/// <summary>
/// Interaction logic for SettingsView.xaml
/// </summary>
public partial class SettingsView : UserControl
{
    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}