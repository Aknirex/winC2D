using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using winC2D_WinUI.Models;
using winC2D_WinUI.Services;
using System.Linq;

namespace winC2D_WinUI.Views
{
    public sealed partial class SoftwarePage : Page
    {
        private ObservableCollection<InstalledSoftware> SoftwareList { get; } = new();

        public SoftwarePage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshData();
        }

        private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            await RefreshData();
        }

        private async Task RefreshData()
        {
            StatusText.Text = "Scanning...";
            BtnRefresh.IsEnabled = false;
            SoftwareList.Clear();

            await Task.Run(() =>
            {
                var data = SoftwareScanner.GetInstalledSoftwareOnC();
                DispatcherQueue.TryEnqueue(() =>
                {
                    ListSoftware.ItemsSource = data;
                    StatusText.Text = $"Found {data.Count} items.";
                    BtnRefresh.IsEnabled = true;
                });
            });
        }

        private async void BtnMigrate_Click(object sender, RoutedEventArgs e)
        {
            var selected = ListSoftware.SelectedItems.Cast<InstalledSoftware>().ToList();
            if (selected.Count == 0)
            {
                var dialog = new ContentDialog
                {
                    Title = "No Selection",
                    Content = "Please select software to migrate.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }

            // Migration logic usually involves picking a target folder
            // For MVP, we'll just show a placeholder dialog or implement basic logic
            // In a real app, this would open a folder picker.
            
            var folderPicker = new Windows.Storage.Pickers.FolderPicker();
            folderPicker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.ComputerFolder;
            folderPicker.FileTypeFilter.Add("*");

            // Getting window handle for picker is tricky in WinUI 3 without helper
            var window = ((App)Application.Current).MainWindow;
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                // Perform migration... 
                // Since actual migration logic is complex and involves admin rights/IO, 
                // we'll just show a message for now that it would migrate to selected folder.
                 var confirm = new ContentDialog
                {
                    Title = "Migrate Software",
                    Content = $"Migrating {selected.Count} items to {folder.Path}?\n(This is a demo, check log for details)",
                    PrimaryButtonText = "Start",
                    CloseButtonText = "Cancel",
                    XamlRoot = this.XamlRoot
                };
                
                if (await confirm.ShowAsync() == ContentDialogResult.Primary)
                {
                    // Call migration service here
                }
            }
        }
    }
}
