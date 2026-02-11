using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using winC2D_WinUI.Views;

namespace winC2D_WinUI
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Title = "winC2D (WinUI 3)";
            // Set Mica backdrop
            this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
        }

        private void NavView_Loaded(object sender, RoutedEventArgs e)
        {
            // Set initial page
            NavView.SelectedItem = NavView.MenuItems[0];
        }

        private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected)
            {
                ContentFrame.Navigate(typeof(SettingsPage));
            }
            else
            {
                var tag = args.SelectedItemContainer.Tag.ToString();
                switch (tag)
                {
                    case "SoftwarePage":
                        // Ensure SoftwarePage type exists or create it
                        ContentFrame.Navigate(typeof(SoftwarePage));
                        break;
                    case "AppDataPage":
                         ContentFrame.Navigate(typeof(AppDataPage));
                        break;
                }
            }
        }
    }
}
