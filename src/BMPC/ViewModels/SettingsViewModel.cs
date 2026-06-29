using BMPC.Core.Models;
using BMPC.Core.Services;
using BMPC.Mvvm;

namespace BMPC.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly SettingsService settingsService;

        private PostImportAction postImportAction;
        public PostImportAction SelectedPostImportAction
        {
            get => postImportAction;
            set
            {
                postImportAction = value;
                NotifyPropertyChanged(nameof(SelectedPostImportAction));
            }
        }

        private bool confirmBeforeDelete;
        public bool ConfirmBeforeDelete
        {
            get => confirmBeforeDelete;
            set { confirmBeforeDelete = value; NotifyPropertyChanged(nameof(ConfirmBeforeDelete)); }
        }

        private AppThemeMode themeMode;
        public AppThemeMode SelectedThemeMode
        {
            get => themeMode;
            set
            {
                themeMode = value;
                NotifyPropertyChanged(nameof(SelectedThemeMode));
            }
        }


        public SettingsViewModel(SettingsService settingsService)
        {
            this.settingsService = settingsService;
            var settings = settingsService.Load();
            postImportAction = settings.PostImportAction;
            confirmBeforeDelete = settings.ConfirmBeforeDelete;
            themeMode = settings.ThemeMode;
        }

        public void Save()
        {
            settingsService.Save(new AppSettings
            {
                PostImportAction = postImportAction,
                ConfirmBeforeDelete = confirmBeforeDelete,
                ThemeMode = themeMode
            });
        }
    }
}


