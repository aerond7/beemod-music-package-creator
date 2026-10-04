using BMPC.Audio.Objects;
using BMPC.Core.Packaging;
using BMPC.Models;
using BMPC.Services;
using BMPC.UserControls;
using BMPC.ViewModels;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace BMPC.Views
{
    public partial class AddSongDialog : Window
    {
        public AddSongDialogViewModel ViewModel { get; private set; }

        // Package entry path -> temp file extracted from the package being edited.
        private readonly Dictionary<string, string> packagedAudioFiles = new();
        private readonly IAppPaths appPaths;

        public AddSongDialog(SongItemModel? existingModel = null, IEnumerable<string>? otherSongNames = null)
            : this(new FileDialogService(), new MessageDialogService(), new AppPaths(), existingModel, otherSongNames)
        {
        }

        public AddSongDialog(
            IFileDialogService fileDialogService,
            IMessageDialogService messageDialogService,
            IAppPaths appPaths,
            SongItemModel? existingModel = null,
            IEnumerable<string>? otherSongNames = null)
        {
            this.appPaths = appPaths;
            ThemeService.PrepareWindow(this);
            InitializeComponent();
            this.ViewModel = new AddSongDialogViewModel(fileDialogService, messageDialogService, appPaths, existingModel, otherSongNames);
            this.DataContext = ViewModel;

            this.ViewModel.RequestClose += Close;
            this.ViewModel.RequestUpdateDialogResult += (result) => this.DialogResult = result;
            this.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            BaseLoopEditor.LoopPointsChanged += loopPoints => this.ViewModel.BaseLoopPoints = loopPoints?.Clone();
            FunnelLoopEditor.LoopPointsChanged += loopPoints => this.ViewModel.FunnelLoopPoints = loopPoints?.Clone();
            Loaded += (_, _) => LoadLoopEditors();
            Closed += (_, _) => DeletePackagedAudioFiles();
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AddSongDialogViewModel.BaseMusicFilePath))
            {
                LoadBaseLoopEditor();
            }
            else if (e.PropertyName == nameof(AddSongDialogViewModel.FunnelMusicFilePath))
            {
                LoadFunnelLoopEditor();
            }
        }

        private void LoadLoopEditors()
        {
            LoadBaseLoopEditor();
            LoadFunnelLoopEditor();
        }

        private void LoadBaseLoopEditor()
            => LoadLoopEditor(
                BaseLoopEditor,
                this.ViewModel.BaseMusicFilePath,
                this.ViewModel.BaseLoopPoints,
                this.ViewModel.BaseAudioReplaced,
                (packagedAssets, path) => packagedAssets.GetBaseEntryPath(path));

        private void LoadFunnelLoopEditor()
            => LoadLoopEditor(
                FunnelLoopEditor,
                this.ViewModel.FunnelMusicFilePath,
                this.ViewModel.FunnelLoopPoints,
                this.ViewModel.TractorBeamAudioReplaced,
                (packagedAssets, path) => packagedAssets.GetTractorBeamEntryPath(path));

        // Unless the track was replaced, edit the audio stored in the package rather than the file on disk,
        // which may have changed or been removed since the package was built.
        private void LoadLoopEditor(
            LoopPointsEditor editor,
            string path,
            AudioLoopPoints? loopPoints,
            bool isReplaced,
            Func<PackagedSongAssets, string, string?> getPackagedEntryPath)
        {
            var packagedAssets = this.ViewModel.PackagedAssets;
            var packagedEntryPath = !isReplaced && packagedAssets is not null
                ? getPackagedEntryPath(packagedAssets, path)
                : null;

            if (packagedAssets is null || packagedEntryPath is null)
            {
                editor.LoadAudio(GetExistingPath(path), loopPoints);
                return;
            }

            editor.LoadAudio(GetPackagedAudioFile(packagedAssets, packagedEntryPath), loopPoints, isPackagedAudio: true);
        }

        private string? GetPackagedAudioFile(PackagedSongAssets packagedAssets, string entryPath)
        {
            if (this.packagedAudioFiles.TryGetValue(entryPath, out var filePath))
            {
                return filePath;
            }

            filePath = packagedAssets.ExtractToFile(entryPath, this.appPaths.TempDirectory);
            if (filePath != null)
            {
                this.packagedAudioFiles[entryPath] = filePath;
            }

            return filePath;
        }

        private void DeletePackagedAudioFiles()
        {
            BaseLoopEditor.StopPlayback();
            FunnelLoopEditor.StopPlayback();

            foreach (var filePath in this.packagedAudioFiles.Values)
            {
                try
                {
                    File.Delete(filePath);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            this.packagedAudioFiles.Clear();
        }

        private static string? GetExistingPath(string path)
            => string.IsNullOrWhiteSpace(path) || path == AddSongDialogViewModel.NoFileSelectedLabel || !File.Exists(path)
                ? null
                : path;

        private void TxtSelection_GotFocus(object sender, RoutedEventArgs e)
        {
            //UCGrid.Focus();
        }
    }
}
