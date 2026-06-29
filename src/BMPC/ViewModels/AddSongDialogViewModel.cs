using BMPC.Commands;
using BMPC.Core;
using BMPC.Models;
using BMPC.Mvvm;
using BMPC.Properties;
using BMPC.Services;
using System.IO;
using System.Windows.Input;

namespace BMPC.ViewModels
{
    public class AddSongDialogViewModel : ViewModelBase
    {
        private string _musicName = string.Empty;
        public string MusicName
        {
            get => _musicName;
            set
            {
                _musicName = value;
                NotifyPropertyChanged(nameof(MusicName));
            }
        }

        private string _musicDescription = string.Empty;
        public string MusicDescription
        {
            get => _musicDescription;
            set
            {
                _musicDescription = value;
                NotifyPropertyChanged(nameof(MusicDescription));
            }
        }

        private string _musicAuthors = string.Empty;
        public string MusicAuthors
        {
            get => _musicAuthors;
            set
            {
                _musicAuthors = value;
                NotifyPropertyChanged(nameof(MusicAuthors));
            }
        }

        private string _previewImage = string.Empty;
        public string PreviewImage
        {
            get => _previewImage;
            private set
            {
                _previewImage = value;
                NotifyPropertyChanged(nameof(PreviewImage));
            }
        }

        /// <summary>Sentinel value displayed when no audio file has been selected.</summary>
        public const string NoFileSelectedLabel = "No file selected.";

        private string _baseMusicFilePath = NoFileSelectedLabel;
        public string BaseMusicFilePath
        {
            get => _baseMusicFilePath;
            private set
            {
                _baseMusicFilePath = value;
                NotifyPropertyChanged(nameof(BaseMusicFilePath));
            }
        }

        private string _funnelMusicFilePath = NoFileSelectedLabel;
        public string FunnelMusicFilePath
        {
            get => _funnelMusicFilePath;
            private set
            {
                _funnelMusicFilePath = value;
                NotifyPropertyChanged(nameof(FunnelMusicFilePath));

                if (File.Exists(_funnelMusicFilePath))
                {
                    CanApplyDefaultFunnelMusic = false;
                    ApplyDefaultFunnelMusic = false;
                }
            }
        }

        private string _speedGelSelections = "No files selected.";
        public string SpeedGelSelections
        {
            get => _speedGelSelections;
            private set
            {
                _speedGelSelections = value;
                NotifyPropertyChanged(nameof(SpeedGelSelections));
            }
        }

        private string _bounceGelSelections = "No files selected.";
        public string BounceGelSelections
        {
            get => _bounceGelSelections;
            private set
            {
                _bounceGelSelections = value;
                NotifyPropertyChanged(nameof(BounceGelSelections));
            }
        }

        private bool _applyDefaultFunnelMusic = false;
        public bool ApplyDefaultFunnelMusic
        {
            get => _applyDefaultFunnelMusic;
            set
            {
                _applyDefaultFunnelMusic = value;
                NotifyPropertyChanged(nameof(ApplyDefaultFunnelMusic));

                if (_applyDefaultFunnelMusic)
                {
                    CanSelectFunnelMusic = false;
                    FunnelMusicFilePath = NoFileSelectedLabel;
                }
                else
                {
                    CanSelectFunnelMusic = true;
                }
            }
        }

        private bool _canApplyDefaultFunnelMusic = true;
        public bool CanApplyDefaultFunnelMusic
        {
            get => _canApplyDefaultFunnelMusic;
            private set
            {
                _canApplyDefaultFunnelMusic = value;
                NotifyPropertyChanged(nameof(CanApplyDefaultFunnelMusic));
            }
        }

        private bool _canSelectFunnelMusic = true;
        public bool CanSelectFunnelMusic
        {
            get => _canSelectFunnelMusic;
            private set
            {
                _canSelectFunnelMusic = value;
                NotifyPropertyChanged(nameof(CanSelectFunnelMusic));
            }
        }

        private bool _canSelectFunnelSync = true;
        public bool CanSelectFunnelSync
        {
            get => _canSelectFunnelSync;
            private set
            {
                _canSelectFunnelSync = value;
                NotifyPropertyChanged(nameof(CanSelectFunnelSync));
            }
        }

        private bool _syncFunnelMusic = false;
        public bool SyncFunnelMusic
        {
            get => _syncFunnelMusic;
            set
            {
                _syncFunnelMusic = value;
                NotifyPropertyChanged(nameof(SyncFunnelMusic));
            }
        }

        private bool _hasSelectedSpeedGel = false;
        public bool HasSelectedSpeedGel
        {
            get => _hasSelectedSpeedGel;
            private set
            {
                _hasSelectedSpeedGel = value;
                NotifyPropertyChanged(nameof(HasSelectedSpeedGel));
            }
        }

        private bool _hasSelectedBounceGel = false;
        public bool HasSelectedBounceGel
        {
            get => _hasSelectedBounceGel;
            private set
            {
                _hasSelectedBounceGel = value;
                NotifyPropertyChanged(nameof(HasSelectedBounceGel));
            }
        }

        public List<string> SelectedSpeedGelSfxFullPaths { get; private set; } = new List<string>();
        public List<string> SelectedBounceGelSfxFullPaths { get; private set; } = new List<string>();

        public event Action? RequestClose;
        public event Action<bool?>? RequestUpdateDialogResult;

        public ICommand SelectIconCommand { get; private set; }
        public ICommand SelectSoundFileCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand AddCommand { get; private set; }
        public ICommand SelectSfxCommand { get; private set; }
        public ICommand ClearSfxCommand { get; private set; }

        private readonly bool _isEditMode;
        private readonly IFileDialogService fileDialogService;
        private readonly IMessageDialogService messageDialogService;
        private readonly IAppPaths appPaths;

        public AddSongDialogViewModel(SongItemModel? existingModel = null)
            : this(new FileDialogService(), new MessageDialogService(), new AppPaths(), existingModel)
        {
        }

        public AddSongDialogViewModel(
            IFileDialogService fileDialogService,
            IMessageDialogService messageDialogService,
            IAppPaths appPaths,
            SongItemModel? existingModel = null)
        {
            this.fileDialogService = fileDialogService;
            this.messageDialogService = messageDialogService;
            this.appPaths = appPaths;
            this.SelectIconCommand = new RelayCommand(IconSelectCommand);
            this.SelectSoundFileCommand = new RelayCommand(SelectSoundFile);
            this.CancelCommand = new RelayCommand(CancelAddingCommand);
            this.AddCommand = new RelayCommand(AddSongCommand);
            this.SelectSfxCommand = new RelayCommand(SelectSfxSoundFiles);
            this.ClearSfxCommand = new RelayCommand(ClearSfx);

            _isEditMode = existingModel != null;

            LoadDefaultImage();

            if (existingModel is not null)
            {
                this.MusicName = existingModel.Name;
                this.MusicDescription = existingModel.Description;
                this.MusicAuthors = existingModel.Authors;
                this.PreviewImage = existingModel.Icon;
                this.BaseMusicFilePath = existingModel.BaseMusicPath;
                this.FunnelMusicFilePath = existingModel.TractorBeamPath ?? "No file selected.";
                this.ApplyDefaultFunnelMusic = existingModel.UseDefaultTractorBeamMusic;
                this.SyncFunnelMusic = existingModel.SyncTractorBeamMusic;
                SetSpeedGelSfxPaths(existingModel.SpeedGelSfxFullPaths);
                SetBounceGelSfxPaths(existingModel.BounceGelSfxFullPaths);
            }
        }

        private void LoadDefaultImage()
        {
            var path = Path.Combine(this.appPaths.ResourcesDirectory, "bmpc.png");
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(this.appPaths.ResourcesDirectory);
                File.WriteAllBytes(path, Resources.BMPCLogo);
            }

            var fileInfo = new FileInfo(path);
            PreviewImage = fileInfo.FullName;
        }

        private void AddSongCommand(object? obj)
        {
            if (string.IsNullOrWhiteSpace(MusicName) ||
                MusicName.Length < 3)
            {
                this.messageDialogService.ShowWarning("Music name must be at least 3 characters");
                return;
            }

            if (string.IsNullOrWhiteSpace(MusicDescription))
            {
                this.messageDialogService.ShowWarning("Enter a description");
                return;
            }

            if (string.IsNullOrWhiteSpace(MusicAuthors))
            {
                this.messageDialogService.ShowWarning("Enter music authors");
                return;
            }

            if (string.IsNullOrWhiteSpace(BaseMusicFilePath) ||
                (!_isEditMode && !File.Exists(BaseMusicFilePath)))
            {
                this.messageDialogService.ShowWarning("Select base music");
                return;
            }

            RequestUpdateDialogResult?.Invoke(true);
            RequestClose?.Invoke();
        }

        private void CancelAddingCommand(object? obj)
        {
            if (this.messageDialogService.Confirm("Are you sure you want to cancel adding a song?", "Cancel"))
            {
                RequestClose?.Invoke();
            }
        }

        private void IconSelectCommand(object? obj)
        {
            var fileName = this.fileDialogService.OpenFile("Select music icon", "Image Files (*.png, *.jpg, *.jpeg)|*.png;*.jpg;*.jpeg");
            if (fileName != null)
            {
                PreviewImage = fileName;
            }
        }

        private void SelectSoundFile(object? obj)
        {
            if (obj is null)
            {
                return;
            }

            var type = obj as string;
            if (type is null)
            {
                return;
            }

            var fileName = this.fileDialogService.OpenFile($"Select {type} music", "Sound Files (*.wav, *.mp3)|*.wav;*.mp3");
            if (fileName != null)
            {
                switch (type)
                {
                    case "base":
                        {
                            BaseMusicFilePath = fileName;
                            break;
                        }

                    case "funnel":
                        {
                            FunnelMusicFilePath = fileName;
                            break;
                        }
                }
            }
        }

        private void SelectSfxSoundFiles(object? obj)
        {
            if (obj is null)
            {
                return;
            }

            var type = obj as string;
            if (type is null)
            {
                return;
            }

            var fileNames = this.fileDialogService.OpenFiles($"Select {type} SFX", "Sound Files (*.wav, *.mp3)|*.wav;*.mp3");
            if (fileNames.Count > 0)
            {
                switch (type)
                {
                    case "speed":
                        {
                            SetSpeedGelSfxPaths(fileNames.ToList());
                            break;
                        }

                    case "bounce":
                        {
                            SetBounceGelSfxPaths(fileNames.ToList());
                            break;
                        }
                }
            }
        }

        private void ClearSfx(object? obj)
        {
            if (obj is null)
            {
                return;
            }

            var type = obj as string;
            if (type is null)
            {
                return;
            }

            switch (type)
            {
                case "speed":
                    {
                        SetSpeedGelSfxPaths(new List<string>());
                        break;
                    }

                case "bounce":
                    {
                        SetBounceGelSfxPaths(new List<string>());
                        break;
                    }
            }
        }

        private void SetSpeedGelSfxPaths(List<string> paths)
        {
            SelectedSpeedGelSfxFullPaths = paths;
            HasSelectedSpeedGel = SelectedSpeedGelSfxFullPaths.Count > 0;
            SpeedGelSelections = HasSelectedSpeedGel ? $"{SelectedSpeedGelSfxFullPaths.Count} file(s)" : "No files selected.";
        }

        private void SetBounceGelSfxPaths(List<string> paths)
        {
            SelectedBounceGelSfxFullPaths = paths;
            HasSelectedBounceGel = SelectedBounceGelSfxFullPaths.Count > 0;
            BounceGelSelections = HasSelectedBounceGel ? $"{SelectedBounceGelSfxFullPaths.Count} file(s)" : "No files selected.";
        }
    }
}
