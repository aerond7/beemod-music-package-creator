using BMPC.Commands;
using BMPC.Interfaces;
using BMPC.Models;
using BMPC.Services;
using BMPC.ViewModels;
using BMPC.Views;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BMPC.UserControls
{
    /// <summary>
    /// Interaction logic for PackageFilesUC.xaml
    /// </summary>
    public partial class PackageFilesUC : UserControl, ICreatePackageSetupStage
    {
        public ObservableCollection<SongItemModel> SongItems { get; set; }

        public string MusicName { get; private set; } = "";
        public string MusicDescription { get; private set; } = "";
        public string MusicAuthors { get; private set; } = "";

        public ICommand EditCommand { get; private set; }
        public ICommand RemoveCommand { get; private set; }
        private readonly IFileDialogService fileDialogService;
        private readonly IMessageDialogService messageDialogService;
        private readonly IAppPaths appPaths;

        public PackageFilesUC(IEnumerable<SongItemModel>? initialSongs = null)
            : this(new FileDialogService(), new MessageDialogService(), new AppPaths(), initialSongs)
        {
        }

        public PackageFilesUC(
            IFileDialogService fileDialogService,
            IMessageDialogService messageDialogService,
            IAppPaths appPaths,
            IEnumerable<SongItemModel>? initialSongs = null)
        {
            this.fileDialogService = fileDialogService;
            this.messageDialogService = messageDialogService;
            this.appPaths = appPaths;
            InitializeComponent();
            this.DataContext = this;

            this.SongItems = new ObservableCollection<SongItemModel>(initialSongs ?? Enumerable.Empty<SongItemModel>());
            this.EditCommand = new RelayCommand(EditItem);
            this.RemoveCommand = new RelayCommand(RemoveItem);
        }

        public string GetStageName()
        {
            return "Add music";
        }

        public string GetStageDescription()
        {
            return "Add music to your new package.";
        }

        public PackageSetupStageValidationResult Validate()
        {
            if (SongItems.Count <= 0)
            {
                return new PackageSetupStageValidationResult(false, "You must add at least 1 song to your package");
            }

            return new PackageSetupStageValidationResult
            {
                IsValid = true
            };
        }

        private void AddSongButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddSongDialog(this.fileDialogService, this.messageDialogService, this.appPaths);

            if (dialog.ShowDialog() == true)
            {
                SongItems.Add(new SongItemModel
                {
                    Name = dialog.ViewModel.MusicName,
                    Description = dialog.ViewModel.MusicDescription,
                    Authors = dialog.ViewModel.MusicAuthors,
                    Group = null,
                    Icon = dialog.ViewModel.PreviewImage,
                    BaseMusicPath = dialog.ViewModel.BaseMusicFilePath,
                    TractorBeamPath = File.Exists(dialog.ViewModel.FunnelMusicFilePath) ? dialog.ViewModel.FunnelMusicFilePath : null,
                    UseDefaultTractorBeamMusic = dialog.ViewModel.ApplyDefaultFunnelMusic,
                    SyncTractorBeamMusic = dialog.ViewModel.SyncFunnelMusic,
                    SpeedGelSfxFullPaths = dialog.ViewModel.SelectedSpeedGelSfxFullPaths,
                    BounceGelSfxFullPaths = dialog.ViewModel.SelectedBounceGelSfxFullPaths
                });
            }
        }

        private void EditItem(object? arg)
        {
            if (arg is not SongItemModel item)
            {
                return;
            }

            var dialog = new AddSongDialog(this.fileDialogService, this.messageDialogService, this.appPaths, item);
            if (dialog.ShowDialog() == true)
            {
                var index = SongItems.IndexOf(item);
                var funnelPath = dialog.ViewModel.FunnelMusicFilePath;
                var newItem = new SongItemModel
                {
                    Guid = item.Guid,
                    Name = dialog.ViewModel.MusicName,
                    Description = dialog.ViewModel.MusicDescription,
                    Authors = dialog.ViewModel.MusicAuthors,
                    Group = null,
                    Icon = dialog.ViewModel.PreviewImage,
                    BaseMusicPath = dialog.ViewModel.BaseMusicFilePath,
                    TractorBeamPath = string.IsNullOrWhiteSpace(funnelPath) || funnelPath == AddSongDialogViewModel.NoFileSelectedLabel ? null : funnelPath,
                    UseDefaultTractorBeamMusic = dialog.ViewModel.ApplyDefaultFunnelMusic,
                    SyncTractorBeamMusic = dialog.ViewModel.SyncFunnelMusic,
                    SpeedGelSfxFullPaths = dialog.ViewModel.SelectedSpeedGelSfxFullPaths,
                    BounceGelSfxFullPaths = dialog.ViewModel.SelectedBounceGelSfxFullPaths
                };

                SongItems.Remove(item);
                SongItems.Insert(index, newItem);
            }
        }

        private void RemoveItem(object? arg)
        {
            if (arg is not SongItemModel item)
            {
                return;
            }

            if (this.messageDialogService.Confirm("Are you sure you want to remove this song?", "Remove"))
            {
                SongItems.Remove(item);
            }
        }
    }
}
