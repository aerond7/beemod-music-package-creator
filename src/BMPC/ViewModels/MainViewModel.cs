using BMPC.Commands;
using BMPC.Core;
using BMPC.Core.Models;
using BMPC.Core.Services;
using BMPC.Helpers;
using BMPC.Models;
using BMPC.Mvvm;
using BMPC.Services;
using BMPC.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;

namespace BMPC.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IAbstractFactory<CreatePackageView> packageViewFactory;
        private readonly IMessageDialogService messageDialogService;
        private readonly IFileDialogService fileDialogService;
        private readonly IProcessLauncher processLauncher;
        private readonly IAppPaths appPaths;

        public string AppVersion { get; private set; }
        public ObservableCollection<MusicPackageItem> Packages { get; private set; } = new ObservableCollection<MusicPackageItem>();
        public int TotalPackages => Packages.Count;

        public ICommand CreateNewPackageCommand { get; set; }
        public ICommand EditPackageCommand { get; set; }
        public ICommand DeletePackageCommand { get; set; }
        public ICommand OpenPackageLocationCommand { get; set; }
        public ICommand OpenBeemodCommand { get; set; }
        public ICommand RefreshCommand { get; set; }
        public ICommand OpenPackagesFolderCommand { get; set; }

        public MainViewModel(
            IAbstractFactory<CreatePackageView> packageViewFactory,
            IMessageDialogService messageDialogService,
            IFileDialogService fileDialogService,
            IProcessLauncher processLauncher,
            IAppPaths appPaths)
        {
            this.packageViewFactory = packageViewFactory;
            this.messageDialogService = messageDialogService;
            this.fileDialogService = fileDialogService;
            this.processLauncher = processLauncher;
            this.appPaths = appPaths;
            this.CreateNewPackageCommand = new RelayCommand(CreateNewPackage);
            this.EditPackageCommand = new RelayCommand(EditPackage, IsPackageSelected);
            this.DeletePackageCommand = new RelayCommand(DeletePackage, IsPackageSelected);
            this.OpenPackageLocationCommand = new RelayCommand(OpenPackageLocation, IsPackageSelected);
            this.OpenBeemodCommand = new RelayCommand(_ => LaunchBee());
            this.RefreshCommand = new RelayCommand(_ => ReloadPackages());
            this.OpenPackagesFolderCommand = new RelayCommand(_ => OpenPackagesFolder());

            Packages.CollectionChanged += Packages_CollectionChanged;
            ReloadPackages();

            this.AppVersion = Utils.GetAppVersion();
            ReloadSettings();

#if DEBUG
            this.AppVersion += " (debug build)";
#endif
        }

        private static bool IsPackageSelected(object? obj) => obj is MusicPackageItem;

        private void OpenPackagesFolder()
        {
            try
            {
                var path = Path.GetFullPath(this.appPaths.BeePackagesDirectory);
                if (!this.processLauncher.OpenFolder(path))
                {
                    this.messageDialogService.ShowWarning("The packages folder could not be found.");
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void OpenPackageLocation(object? obj)
        {
            try
            {
                if (obj is not MusicPackageItem item)
                {
                    return;
                }

                var path = Path.GetFullPath(Path.Combine(this.appPaths.BeePackagesDirectory, item.Package.Id + Constants.BeePackageFileExtension));
                if (!this.processLauncher.RevealInFileExplorer(path))
                {
                    this.messageDialogService.ShowWarning("The package file could not be found.");
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void DeletePackage(object? obj)
        {
            try
            {
                if (this.processLauncher.IsProcessRunning("BEE2"))
                {
                    this.messageDialogService.ShowWarning("Exit BEEmod before deleting a package");
                    return;
                }

                if (obj is not MusicPackageItem item)
                {
                    return;
                }

                var settings = new SettingsService().Load();
                if (settings.ConfirmBeforeDelete &&
                    !this.messageDialogService.Confirm($"Are you sure you want to remove '{item.Package.Name}' package?", "Remove package"))
                {
                    return;
                }

                var packageService = new PackageService(this.appPaths.TempDirectory);
                packageService.DeletePackage(item.Package.Id);
                ReloadPackages();
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void EditPackage(object? obj)
        {
            try
            {
                if (this.processLauncher.IsProcessRunning("BEE2"))
                {
                    this.messageDialogService.ShowWarning("Exit BEEmod before editing a package");
                    return;
                }

                if (obj is not MusicPackageItem item)
                {
                    return;
                }

                if (item.Package.EditData == null)
                {
                    this.messageDialogService.ShowWarning("This package was created with an older version of BMPC and cannot be edited. Please delete and recreate it.");
                    return;
                }

                var view = new CreatePackageView(this.messageDialogService, this.fileDialogService, this.appPaths, item.Package);
                if (view.ShowDialog() == true)
                {
                    ReloadPackages();
                    HandlePostImport("The package was successfully updated and re-imported into BEEmod!\n\nLaunch BEE2 now?", "Package updated");
                }
                else
                {
                    ReloadPackages();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void ReloadPackages()
        {
            try
            {
                Packages.Clear();

                var loader = new PackageLoader(this.appPaths.PackagesDirectory);
                foreach (var p in loader.LoadPackages())
                {
                    Packages.Add(new MusicPackageItem
                    {
                        Package = p
                    });
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        public void ReloadSettings()
        {
            // Currently no view-model state driven by settings besides delete confirmation.
        }

        private void Packages_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            NotifyPropertyChanged(nameof(TotalPackages));
        }

        private void CreateNewPackage(object? obj)
        {
            try
            {
                if (this.processLauncher.IsProcessRunning("BEE2"))
                {
                    this.messageDialogService.ShowWarning("Exit BEEmod before creating a new package");
                    return;
                }

                if (this.packageViewFactory.Create().ShowDialog() == true)
                {
                    ReloadPackages();
                    HandlePostImport("The package was successfully created and imported into BEEmod!\n\nLaunch BEE2 now?", "Package created");
                }
                else
                {
                    ReloadPackages();
                }
            }
            catch (Exception ex)
            {
                ShowError(ex);
            }
        }

        private void HandlePostImport(string prompt, string title)
        {
            var settings = new SettingsService().Load();
            switch (settings.PostImportAction)
            {
                case PostImportAction.OpenBeemod:
                    LaunchBee();
                    break;
                case PostImportAction.Ask:
                default:
                    if (this.messageDialogService.Confirm(prompt, title, MessageDialogIcon.Information))
                    {
                        LaunchBee();
                    }
                    break;
                case PostImportAction.DoNothing:
                    break;
            }
        }

        private void LaunchBee()
        {
            if (!this.processLauncher.TryLaunch(this.appPaths.BeeExecutableName))
            {
                this.messageDialogService.ShowWarning($"{this.appPaths.BeeExecutableName} was not found.");
            }
        }

        private void ShowError(Exception ex)
            => this.messageDialogService.ShowError(ex.ToString(), "Error!");
    }
}
