using System.Windows.Controls;
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
}