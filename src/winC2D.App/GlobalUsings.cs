// Global using aliases to resolve WPF + WinForms namespace conflicts.
// When UseWindowsForms=true is set alongside UseWPF=true, several types become ambiguous.
// We keep the WPF types as the defaults throughout the project.
global using Application    = System.Windows.Application;
global using UserControl    = System.Windows.Controls.UserControl;
global using MessageBox     = System.Windows.MessageBox;
global using MessageBoxButton   = System.Windows.MessageBoxButton;
global using MessageBoxImage    = System.Windows.MessageBoxImage;
global using MessageBoxResult   = System.Windows.MessageBoxResult;
global using Clipboard          = System.Windows.Clipboard;
global using Binding            = System.Windows.Data.Binding;
global using ComboBox           = System.Windows.Controls.ComboBox;
global using SelectionChangedEventArgs = System.Windows.Controls.SelectionChangedEventArgs;
