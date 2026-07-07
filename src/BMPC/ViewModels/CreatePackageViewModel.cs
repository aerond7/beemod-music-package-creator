using BMPC.Commands;
using BMPC.Core;
using BMPC.Core.Models;
using BMPC.Core.Services;
using BMPC.Interfaces;
using BMPC.Models;
using BMPC.Mvvm;
using BMPC.Services;
using BMPC.UserControls;
using System.Windows.Controls;
using System.Windows.Input;

namespace BMPC.ViewModels
{
    public class CreatePackageViewModel : ViewModelBase
    {
        private UserControl? _currentSetupControl;
        public UserControl? CurrentSetupControl
        {
            get => _currentSetupControl;
            private set
            {
                _currentSetupControl = value;
                NotifyPropertyChanged(nameof(CurrentSetupControl));

                if (_currentSetupControl is ICreatePackageSetupStage)
                {
                    CurrentStageName = ((ICreatePackageSetupStage)_currentSetupControl!).GetStageName();
                    CurrentStageDescription = ((ICreatePackageSetupStage)_currentSetupControl!).GetStageDescription();
                    StageSummary = $"({currentStageIndex + 1}/{setupStages.Length}) {CurrentStageName}";
                }
            }
        }

        private string _currentStageName = string.Empty;
        public string CurrentStageName
        {
            get => _currentStageName;
            private set
            {
                _currentStageName = value;
                NotifyPropertyChanged(nameof(CurrentStageName));
            }
        }

        private string _currentStageDescription = string.Empty;
        public string CurrentStageDescription
        {
            get => _currentStageDescription;
            private set
            {
                _currentStageDescription = value;
                NotifyPropertyChanged(nameof(CurrentStageDescription));
            }
        }

        private string _stageSummary = string.Empty;
        public string StageSummary
        {
            get => _stageSummary;
            private set
            {
                _stageSummary = value;
                NotifyPropertyChanged(nameof(StageSummary));
            }
        }

        private bool _isImportVisible = false;
        public bool IsImportVisible
        {
            get => _isImportVisible;
            private set
            {
                _isImportVisible = value;
                NotifyPropertyChanged(nameof(IsImportVisible));
            }
        }

        private bool _canBacktrack = false;
        public bool CanBacktrack
        {
            get => _canBacktrack;
            private set
            {
                _canBacktrack = value;
                NotifyPropertyChanged(nameof(CanBacktrack));
            }
        }

        private bool _buttonsVisible = true;
        public bool ButtonsVisible
        {
            get => _buttonsVisible;
            private set
            {
                _buttonsVisible = value;
                NotifyPropertyChanged(nameof(ButtonsVisible));
            }
        }

        private bool _spinnerVisible = false;
        public bool SpinnerVisible
        {
            get => _spinnerVisible;
            private set
            {
                _spinnerVisible = value;
                NotifyPropertyChanged(nameof(SpinnerVisible));
            }
        }

        private bool _isFinished = false;
        public bool IsFinished
        {
            get => _isFinished;
            private set
            {
                _isFinished = value;
                NotifyPropertyChanged(nameof(IsFinished));
            }
        }

        private bool _isImporting = false;
        public bool IsImporting
        {
            get => _isImporting;
            private set
            {
                _isImporting = value;
                NotifyPropertyChanged(nameof(IsImporting));
            }
        }

        public ICommand NextCommand { get; private set; }
        public ICommand BackCommand { get; private set; }
        public ICommand CancelCommand { get; private set; }
        public ICommand ImportPackageCommand { get; private set; }

        public event Action? RequestClose;
        public event Action<bool?>? RequestUpdateDialogResult;

        private int currentStageIndex = 0;
        private ICreatePackageSetupStage[] setupStages;

        private PackageData PackData { get; set; }

        private readonly bool _isEditMode;
        private readonly string? _existingPackageId;
        private readonly IMessageDialogService messageDialogService;
        private readonly IFileDialogService fileDialogService;
        private readonly IAppPaths appPaths;

        public string WindowTitle => _isEditMode ? "Edit package" : "Create new package";
        public string ImportButtonLabel => _isEditMode ? "Update" : "Create";

        public string SummaryPackName => PackData.Name;
        public string SummaryPackDescription => PackData.Description;
        public string SummaryPackGroupName => PackData.DefaultGroup;
        public string SummaryPackSongs => string.Join("\n", PackData.Songs.Select(s => $"{s.Name} (by {s.Authors})"));

        public CreatePackageViewModel(
            IMessageDialogService messageDialogService,
            IFileDialogService fileDialogService,
            IAppPaths appPaths,
            BmpcPackage? existingPackage = null)
        {
            this.messageDialogService = messageDialogService;
            this.fileDialogService = fileDialogService;
            this.appPaths = appPaths;
            this.NextCommand = new RelayCommand(ContinueSetupCommand);
            this.BackCommand = new RelayCommand(BackSetupCommand);
            this.CancelCommand = new RelayCommand(CancelSetupCommand);
            this.ImportPackageCommand = new AsyncRelayCommand(ImportPackage);

            this.PackData = new PackageData();

            if (existingPackage?.EditData != null)
            {
                _isEditMode = true;
                _existingPackageId = existingPackage.Id;
                var editData = existingPackage.EditData;

                var initialSongs = editData.Songs.Select(s => new SongItemModel
                {
                    Guid = s.SongId ?? Guid.NewGuid(),
                    Name = s.Name,
                    Description = s.Description,
                    Authors = s.Authors,
                    Group = s.Group,
                    Icon = s.IconFullPath,
                    BaseMusicPath = s.BaseFullPath,
                    BaseLoopPoints = s.BaseLoopPoints?.Clone(),
                    TractorBeamPath = s.TractorBeamFullPath,
                    TractorBeamLoopPoints = s.TractorBeamLoopPoints?.Clone(),
                    UseDefaultTractorBeamMusic = s.UseDefaultTractorBeamMusic,
                    SyncTractorBeamMusic = s.SyncTractorBeamMusic,
                    SpeedGelSfxFullPaths = s.SpeedGelSfxFullPaths,
                    BounceGelSfxFullPaths = s.BounceGelSfxFullPaths
                });

                // When DefaultGroup matches Name it means no explicit group was set (it defaults to the name),
                // so pass null to leave the group field empty in edit mode.
                var explicitGroup = editData.DefaultGroup != editData.Name ? editData.DefaultGroup : null;

                setupStages = new ICreatePackageSetupStage[]
                {
                    new PackageDetailsUC(editData.Name, editData.Description, explicitGroup),
                    new PackageFilesUC(this.fileDialogService, this.messageDialogService, this.appPaths, initialSongs),
                    new PackageSummaryUC(isEditMode: true)
                };
            }
            else
            {
                setupStages = new ICreatePackageSetupStage[]
                {
                    new PackageDetailsUC(),
                    new PackageFilesUC(this.fileDialogService, this.messageDialogService, this.appPaths),
                    new PackageSummaryUC()
                };
            }

            CurrentSetupControl = setupStages[currentStageIndex] as UserControl;
        }

        private async Task ImportPackage(object? obj)
        {
            PackageService? packageService = null;
            var statusSubscribed = false;

            try
            {
                if (!TryProcessCurrentStage())
                {
                    return;
                }

                ButtonsVisible = false;
                IsImportVisible = false;
                CanBacktrack = false;

                packageService = new PackageService(this.appPaths.TempDirectory);
                CurrentSetupControl = new PackageImportingUC();
                CurrentStageName = _isEditMode ? "Update" : "Import";
                CurrentStageDescription = _isEditMode
                    ? "Please wait while BMPC updates the package and re-imports it to BEEmod."
                    : "Please wait while BMPC creates the package and imports it to BEEmod.";
                SpinnerVisible = true;
                IsImporting = true;

                UpdateStatus("Preparing...");
                packageService.StatusUpdated += UpdateStatus;
                statusSubscribed = true;

                await Task.Delay(1000);

                UpdateStatus("Building package...");

                await packageService.Import(PackData, _existingPackageId);

                SpinnerVisible = false;
                IsFinished = true;
                UpdateStatus("Finished.");

                await Task.Delay(2000);

                IsImporting = false;

                try
                {
                    RequestUpdateDialogResult?.Invoke(true);
                    CloseWindow();
                }
                catch { }
            }
            catch (Exception ex)
            {
                SpinnerVisible = false;
                IsFinished = false;
                IsImporting = false;
                ButtonsVisible = true;
                CurrentSetupControl = setupStages[currentStageIndex] as UserControl;
                IsImportVisible = currentStageIndex + 1 >= setupStages.Length;
                CanBacktrack = currentStageIndex > 0;
                this.messageDialogService.ShowError(ex.ToString(), "Error!");
            }
            finally
            {
                if (packageService != null && statusSubscribed)
                {
                    packageService.StatusUpdated -= UpdateStatus;
                }
            }
        }

        private void UpdateStatus(string status)
        {
            var finishUc = CurrentSetupControl as PackageImportingUC;
            StageSummary = status;
            finishUc?.UpdateStatus(status);
        }

        private void CancelSetupCommand(object? obj)
        {
            if (ConfirmCancel())
            {
                CloseWindow();
            }
        }

        // Shared by the Cancel button and the window's X (OnClosing).
        public bool ConfirmCancel()
        {
            var message = _isEditMode
                ? "Are you sure you want to cancel editing this package?"
                : "Are you sure you want to cancel creating a new package?";
            return this.messageDialogService.Confirm(message, "Cancel");
        }

        // Marks the close as approved so OnClosing does not re-prompt.
        public bool AllowClose { get; private set; }

        private void CloseWindow()
        {
            AllowClose = true;
            RequestClose?.Invoke();
        }

        private void BackSetupCommand(object? obj)
        {
            try
            {
                ChangeStage(next: false);
            }
            catch (Exception ex)
            {
                this.messageDialogService.ShowError(ex.ToString(), "Error!");
            }
        }

        private void ContinueSetupCommand(object? obj)
        {
            try
            {
                if (!TryProcessCurrentStage())
                {
                    return;
                }

                if (obj is not null && obj is bool && (bool)obj == true)
                {
                    return;
                }

                ChangeStage(next: true);
            }
            catch (Exception ex)
            {
                this.messageDialogService.ShowError(ex.ToString(), "Error!");
            }
        }

        private bool TryProcessCurrentStage()
        {
            var validation = ((ICreatePackageSetupStage)this.CurrentSetupControl!).Validate();
            if (!validation.IsValid)
            {
                this.messageDialogService.ShowWarning(string.IsNullOrWhiteSpace(validation.Message) ? "Incomplete information, please check your input and try again." : validation.Message);
                return false;
            }

            ProcessCurrentStage(currentStageIndex);
            return true;
        }

        private void ChangeStage(bool next = true)
        {
            currentStageIndex = next ? currentStageIndex + 1 : currentStageIndex - 1;
            this.CurrentSetupControl = setupStages[currentStageIndex] as UserControl;
            IsImportVisible = currentStageIndex + 1 >= setupStages.Length;
            CanBacktrack = currentStageIndex > 0 && !IsImporting;
        }

        private void ProcessCurrentStage(int stageIndex)
        {
            var setupStage = setupStages[stageIndex];
            switch (stageIndex)
            {
                case 0: // package info
                    {
                        var data = setupStage as PackageDetailsUC;
                        if (data is null)
                        {
                            break;
                        }

                        PackData.Name = data.PackName;
                        PackData.Description = data.PackDesc;
                        PackData.DefaultGroup = data.PackGroup ?? data.PackName;

                        break;
                    }

                case 1: // sound files
                    {
                        var data = setupStage as PackageFilesUC;
                        if (data is null)
                        {
                            break;
                        }

                        PackData.Songs.Clear();
                        foreach (var item in data.SongItems)
                        {
                            PackData.Songs.Add(new PackageSong
                            {
                                SongId = item.Guid,
                                Name = item.Name,
                                Description = item.Description,
                                Authors = item.Authors,
                                Group = item.Group,
                                IconFullPath = item.Icon,
                                BaseFullPath = item.BaseMusicPath,
                                BaseLoopPoints = item.BaseLoopPoints?.Clone(),
                                TractorBeamFullPath = item.TractorBeamPath,
                                TractorBeamLoopPoints = item.TractorBeamLoopPoints?.Clone(),
                                UseDefaultTractorBeamMusic = item.UseDefaultTractorBeamMusic,
                                SyncTractorBeamMusic = item.SyncTractorBeamMusic,
                                SpeedGelSfxFullPaths = item.SpeedGelSfxFullPaths,
                                BounceGelSfxFullPaths = item.BounceGelSfxFullPaths
                            });
                        }

                        break;
                    }
            }
        }
    }
}
