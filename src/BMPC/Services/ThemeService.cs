using BMPC.Core.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;

namespace BMPC.Services
{
    public static class ThemeService
    {
        private const string PersonalizeRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string AppsUseLightThemeValue = "AppsUseLightTheme";
        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaUseImmersiveDarkModeBeforeWindows11 = 19;

        private static Func<AppThemeMode>? getCurrentThemeMode;

        public static void StartWatchingSystemThemeChanges(Func<AppThemeMode> getThemeMode)
        {
            getCurrentThemeMode = getThemeMode;
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        public static void StopWatchingSystemThemeChanges()
        {
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
            getCurrentThemeMode = null;
        }

        public static void Apply(AppThemeMode mode)
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            var baseTheme = ResolveBaseTheme(mode);

            theme.SetBaseTheme(baseTheme);
            theme.SetSecondaryColor(baseTheme == BaseTheme.Dark
                ? Color.FromRgb(189, 189, 189)
                : Color.FromRgb(97, 97, 97));
            paletteHelper.SetTheme(theme);
            SetAppBrushes(baseTheme);
            ApplyNativeTitleBars(baseTheme);
        }

        public static void PrepareWindow(Window window)
        {
            window.SetResourceReference(Window.BackgroundProperty, "MaterialDesign.Brush.Background");
            window.SetResourceReference(TextElement.ForegroundProperty, "MaterialDesign.Brush.Foreground");
            window.SourceInitialized += (_, _) => ApplyNativeTitleBar(window, ResolveCurrentBaseTheme());
        }

        private static BaseTheme ResolveBaseTheme(AppThemeMode mode)
        {
            return mode switch
            {
                AppThemeMode.Light => BaseTheme.Light,
                AppThemeMode.Dark => BaseTheme.Dark,
                _ => IsOsAppThemeDark() ? BaseTheme.Dark : BaseTheme.Light
            };
        }

        private static bool IsOsAppThemeDark()
        {
            using var key = Registry.CurrentUser.OpenSubKey(PersonalizeRegistryKey);
            var value = key?.GetValue(AppsUseLightThemeValue);

            return value is int appsUseLightTheme && appsUseLightTheme == 0;
        }

        private static BaseTheme ResolveCurrentBaseTheme()
        {
            return ResolveBaseTheme(getCurrentThemeMode?.Invoke() ?? AppThemeMode.Auto);
        }

        private static void SetAppBrushes(BaseTheme baseTheme)
        {
            if (Application.Current == null)
            {
                return;
            }

            var isDark = baseTheme == BaseTheme.Dark;
            Application.Current.Resources["BmpcWarningBrush"] = new SolidColorBrush(isDark
                ? Color.FromRgb(255, 183, 77)
                : Color.FromRgb(217, 108, 0));
            Application.Current.Resources["BmpcSuccessBrush"] = new SolidColorBrush(isDark
                ? Color.FromRgb(129, 199, 132)
                : Color.FromRgb(46, 125, 50));
        }

        private static void ApplyNativeTitleBars(BaseTheme baseTheme)
        {
            if (Application.Current == null)
            {
                return;
            }

            foreach (Window window in Application.Current.Windows)
            {
                ApplyNativeTitleBar(window, baseTheme);
            }
        }

        private static void ApplyNativeTitleBar(Window window, BaseTheme baseTheme)
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero)
            {
                return;
            }

            var useDarkMode = baseTheme == BaseTheme.Dark ? 1 : 0;
            _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkMode, ref useDarkMode, sizeof(int));
            _ = DwmSetWindowAttribute(handle, DwmwaUseImmersiveDarkModeBeforeWindows11, ref useDarkMode, sizeof(int));
        }

        private static void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category is not (UserPreferenceCategory.Color or UserPreferenceCategory.General))
            {
                return;
            }

            var mode = getCurrentThemeMode?.Invoke();
            if (mode == AppThemeMode.Auto)
            {
                Apply(mode.Value);
            }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);
    }
}
