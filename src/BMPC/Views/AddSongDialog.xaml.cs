using BMPC.Models;
using BMPC.Services;
using BMPC.ViewModels;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace BMPC.Views
{
    public partial class AddSongDialog : Window
    {
        public AddSongDialogViewModel ViewModel { get; private set; }

        public AddSongDialog(SongItemModel? existingModel = null)
            : this(new FileDialogService(), new MessageDialogService(), new AppPaths(), existingModel)
        {
        }

        public AddSongDialog(
            IFileDialogService fileDialogService,
            IMessageDialogService messageDialogService,
            IAppPaths appPaths,
            SongItemModel? existingModel = null)
        {
            ThemeService.PrepareWindow(this);
            InitializeComponent();
            this.ViewModel = new AddSongDialogViewModel(fileDialogService, messageDialogService, appPaths, existingModel);
            this.DataContext = ViewModel;

            this.ViewModel.RequestClose += Close;
            this.ViewModel.RequestUpdateDialogResult += (result) => this.DialogResult = result;
            this.ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            BaseLoopEditor.LoopPointsChanged += loopPoints => this.ViewModel.BaseLoopPoints = loopPoints?.Clone();
            FunnelLoopEditor.LoopPointsChanged += loopPoints => this.ViewModel.FunnelLoopPoints = loopPoints?.Clone();
            Loaded += (_, _) => LoadLoopEditors();
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
            => BaseLoopEditor.LoadAudio(GetExistingPath(this.ViewModel.BaseMusicFilePath), this.ViewModel.BaseLoopPoints);

        private void LoadFunnelLoopEditor()
            => FunnelLoopEditor.LoadAudio(GetExistingPath(this.ViewModel.FunnelMusicFilePath), this.ViewModel.FunnelLoopPoints);

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
