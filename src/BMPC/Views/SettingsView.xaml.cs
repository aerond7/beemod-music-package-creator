using BMPC.Core.Models;
using BMPC.Core.Services;
using BMPC.Services;
using BMPC.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace BMPC.Views
{
    public partial class SettingsView : Window
    {
        private readonly SettingsViewModel viewModel;

        public SettingsView()
        {
            ThemeService.PrepareWindow(this);
            InitializeComponent();
            viewModel = new SettingsViewModel(new SettingsService());
            DataContext = viewModel;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            viewModel.Save();
            ThemeService.Apply(viewModel.SelectedThemeMode);
            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}


