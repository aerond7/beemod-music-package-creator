using BMPC.Models;
using BMPC.ViewModels;
using System;
using System.ComponentModel;
using System.Diagnostics;
using BMPC.Services;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

namespace BMPC.Views
{
    public partial class Main : Window
    {
        private MainViewModel ViewModel { get; set; }

        private string? sortProperty;
        private ListSortDirection sortDirection;

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

        private void LvPackages_ColumnHeaderClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is not GridViewColumnHeader header || header.Column is null)
            {
                return;
            }

            string? property = null;
            if (ReferenceEquals(header.Column, ColName))
            {
                property = nameof(MusicPackageItem.Name);
            }
            else if (ReferenceEquals(header.Column, ColSongs))
            {
                property = nameof(MusicPackageItem.SongCountValue);
            }
            else if (ReferenceEquals(header.Column, ColAdded))
            {
                property = nameof(MusicPackageItem.AddedValue);
            }

            if (property is null)
            {
                return;
            }

            this.sortDirection = this.sortProperty == property && this.sortDirection == ListSortDirection.Ascending
                ? ListSortDirection.Descending
                : ListSortDirection.Ascending;
            this.sortProperty = property;

            var view = CollectionViewSource.GetDefaultView(LvPackages.ItemsSource);
            if (view is null)
            {
                return;
            }

            view.SortDescriptions.Clear();
            view.SortDescriptions.Add(new SortDescription(property, this.sortDirection));
            view.Refresh();

            UpdateSortIndicators(header.Column);
        }

        private void UpdateSortIndicators(GridViewColumn sortedColumn)
        {
            var arrow = this.sortDirection == ListSortDirection.Ascending ? "  ▲" : "  ▼";
            ColName.Header = "Name" + (ReferenceEquals(sortedColumn, ColName) ? arrow : string.Empty);
            ColSongs.Header = "Songs in package" + (ReferenceEquals(sortedColumn, ColSongs) ? arrow : string.Empty);
            ColAdded.Header = "Added" + (ReferenceEquals(sortedColumn, ColAdded) ? arrow : string.Empty);
        }

        private void Window_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var src = e.OriginalSource as System.Windows.DependencyObject;

            // ListView handles its own selection; buttons act on the current selection.
            if (FindAncestor<ListView>(src) != null || FindAncestor<Button>(src) != null)
            {
                return;
            }

            LvPackages.SelectedItem = null;
        }

        private static T? FindAncestor<T>(System.Windows.DependencyObject? current) where T : System.Windows.DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                {
                    return match;
                }

                current = System.Windows.Media.VisualTreeHelper.GetParent(current)
                          ?? System.Windows.LogicalTreeHelper.GetParent(current);
            }

            return null;
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
