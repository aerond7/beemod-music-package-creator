using BMPC.Core.Models;
using BMPC.Core.Services;

namespace BMPC.Core.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void Load_WhenSettingsFileMissing_ReturnsDefaults()
    {
        using var scope = CurrentDirectoryScope.Create();
        var service = new SettingsService();

        var settings = service.Load();

        Assert.Equal(PostImportAction.Ask, settings.PostImportAction);
        Assert.True(settings.ConfirmBeforeDelete);
        Assert.Equal(AppThemeMode.Auto, settings.ThemeMode);
    }

    [Fact]
    public void Load_WhenSettingsFileInvalid_ReturnsDefaults()
    {
        using var scope = CurrentDirectoryScope.Create();
        Directory.CreateDirectory("bmpc");
        File.WriteAllText(Path.Combine("bmpc", "settings.json"), "{ not valid json");
        var service = new SettingsService();

        var settings = service.Load();

        Assert.Equal(PostImportAction.Ask, settings.PostImportAction);
        Assert.True(settings.ConfirmBeforeDelete);
        Assert.Equal(AppThemeMode.Auto, settings.ThemeMode);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsSettings()
    {
        using var scope = CurrentDirectoryScope.Create();
        var service = new SettingsService();
        var saved = new AppSettings
        {
            PostImportAction = PostImportAction.OpenBeemod,
            ConfirmBeforeDelete = false,
            ThemeMode = AppThemeMode.Dark
        };

        service.Save(saved);
        var loaded = service.Load();

        Assert.Equal(PostImportAction.OpenBeemod, loaded.PostImportAction);
        Assert.False(loaded.ConfirmBeforeDelete);
        Assert.Equal(AppThemeMode.Dark, loaded.ThemeMode);
    }
}
