using BMPC.Models;
using BMPC.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using BMPC.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace BMPC.Views
{
    public partial class Main : Window
    {
        private MainViewModel ViewModel { get; set; }

        private bool sortDescending;

        public Main(MainViewModel viewModel)
        {
            ThemeService.PrepareWindow(this);
            InitializeComponent();
            this.ViewModel = viewModel;
            this.DataContext = ViewModel;
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            var view = CollectionViewSource.GetDefaultView(LvPackages.ItemsSource);
            if (view is null)
            {
                return;
            }

            var term = TxtSearch.Text;
            view.Filter = string.IsNullOrWhiteSpace(term)
                ? null
                : o => o is MusicPackageItem item
                       && item.Name.Contains(term, StringComparison.OrdinalIgnoreCase);
        }

        private void LvPackages_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // Only react to double-click on an actual row, not the header / empty area.
            var container = ItemsControl.ContainerFromElement((ListView)sender, (System.Windows.DependencyObject)e.OriginalSource);
            if (container is not ListViewItem)
            {
                return;
            }

            if (LvPackages.SelectedItem is MusicPackageItem item
                && this.ViewModel.EditPackageCommand.CanExecute(item))
            {
                this.ViewModel.EditPackageCommand.Execute(item);
            }
        }

        private void Sort_Changed(object sender, RoutedEventArgs e) => ApplySort();

        private void SortDir_Click(object sender, RoutedEventArgs e)
        {
            this.sortDescending = !this.sortDescending;
            IconSortDir.Kind = this.sortDescending
                ? MaterialDesignThemes.Wpf.PackIconKind.SortDescending
                : MaterialDesignThemes.Wpf.PackIconKind.SortAscending;
            ApplySort();
        }

        private void ApplySort()
        {
            // Controls are created during InitializeComponent; guard against early events.
            if (CmbSort is null || LvPackages?.ItemsSource is null)
            {
                return;
            }

            var view = CollectionViewSource.GetDefaultView(LvPackages.ItemsSource);
            if (view is null)
            {
                return;
            }

            var property = CmbSort.SelectedIndex switch
            {
                1 => nameof(MusicPackageItem.SongCountValue),
                2 => nameof(MusicPackageItem.AddedValue),
                _ => nameof(MusicPackageItem.Name)
            };

            var direction = this.sortDescending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;

            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(property, direction));
            view.Refresh();
        }

        private void LvPackages_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var lvPackages = (ListView)sender;
            var container = ItemsControl.ContainerFromElement(lvPackages, (System.Windows.DependencyObject)e.OriginalSource);
            if (container is ListViewItem item)
            {
                item.IsSelected = true;
            }
            else
            {
                // Clicked empty area / header / scrollbar -> clear selection.
                lvPackages.SelectedItem = null;
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
