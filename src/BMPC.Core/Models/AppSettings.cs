namespace BMPC.Core.Models
{
    public enum PostImportAction
    {
        Ask = 0,
        OpenBeemod = 1,
        DoNothing = 2
    }

    public enum AppThemeMode
    {
        Auto = 0,
        Light = 1,
        Dark = 2
    }

    public class AppSettings
    {
        public PostImportAction PostImportAction { get; set; } = PostImportAction.Ask;
        public bool ConfirmBeforeDelete { get; set; } = true;
        public AppThemeMode ThemeMode { get; set; } = AppThemeMode.Auto;
    }
}


