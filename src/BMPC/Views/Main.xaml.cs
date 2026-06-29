using BMPC.ViewModels;
using System.Diagnostics;
using BMPC.Services;
using System.Windows;
using System.Windows.Controls;

namespace BMPC.Views
{
    public partial class Main : Window
    {
        private MainViewModel ViewModel { get; set; }

        public Main(MainViewModel viewModel)
        {
            ThemeService.PrepareWindow(this);
            InitializeComponent();
            this.ViewModel = viewModel;
            this.DataContext = ViewModel;

            // initial state
            BtnPackageDetails.IsEnabled = false;
            BtnPackageRemove.IsEnabled = false;
            BtnPackageEdit.IsEnabled = false;
        }

        private void LvPackages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lvPackages = (ListView)sender;
            if (lvPackages is null)
            {
                return;
            }

            if (lvPackages.SelectedItem is null)
            {
                BtnPackageDetails.IsEnabled = false;
                BtnPackageRemove.IsEnabled = false;
                BtnPackageEdit.IsEnabled = false;
            }
            else
            {
                BtnPackageDetails.IsEnabled = true;
                BtnPackageRemove.IsEnabled = true;
                BtnPackageEdit.IsEnabled = true;
            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new SettingsView();
            dlg.Owner = this;
            if (dlg.ShowDialog() == true)
            {
                this.ViewModel.ReloadSettings();
            }
        }

        // Details feature removed per request

        private void WebsiteButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://bmpc.aerond.dev/",
                UseShellExecute = true
            });
        }

        private void GitHubButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/aerond7/beemod-music-package-creator",
                UseShellExecute = true
            });
        }

        private void DiscordButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://discord.com/invite/mkqmXM5",
                UseShellExecute = true
            });
        }
    }
}
