using System.IO.Compression;
using BMPC.Core.Models;
using BMPC.Core.Packaging;

namespace BMPC.Core.Tests;

public class PackagedSongAssetsTests
{
    [Fact]
    public void Find_ReturnsNullWhenPackageMissing()
    {
        using var scope = CurrentDirectoryScope.Create();

        Assert.Null(PackagedSongAssets.Find("bmpc_missing", CreateSong()));
    }

    [Fact]
    public void GetEntryPaths_OnlyMatchPackagedSourcePaths()
    {
        using var scope = CurrentDirectoryScope.Create();
        CreatePackage("bmpc_test");

        var assets = PackagedSongAssets.Find("bmpc_test", CreateSong())!;

        Assert.Equal("resources/sound/music/bmpc_theme.wav", assets.GetBaseEntryPath("base.wav"));
        Assert.Null(assets.GetBaseEntryPath("other.wav"));
        Assert.Equal("resources/sound/music/bmpc_theme_tb.wav", assets.GetTractorBeamEntryPath("tb.wav"));
        Assert.Null(assets.GetTractorBeamEntryPath("other.wav"));
    }

    [Fact]
    public void ExtractIcon_ExtractsPackagedIconIntoDirectory()
    {
        using var scope = CurrentDirectoryScope.Create();
        CreatePackage("bmpc_test");
        var assets = PackagedSongAssets.Find("bmpc_test", CreateSong())!;

        var iconPath = assets.ExtractIcon("extracted");

        Assert.NotNull(iconPath);
        Assert.True(Path.IsPathFullyQualified(iconPath));
        Assert.Equal(".png", Path.GetExtension(iconPath));
        Assert.Equal(Path.GetFullPath("extracted"), Path.GetDirectoryName(iconPath));
        Assert.Equal("packaged icon", File.ReadAllText(iconPath));
    }

    [Fact]
    public void ExtractToFile_ReturnsNullWhenEntryMissing()
    {
        using var scope = CurrentDirectoryScope.Create();
        CreatePackage("bmpc_test");
        var assets = PackagedSongAssets.Find("bmpc_test", CreateSong())!;

        Assert.Null(assets.ExtractToFile("resources/sound/music/missing.wav", "extracted"));
        Assert.Empty(Directory.GetFiles("extracted"));
    }

    private static PackageSong CreateSong()
        => new()
        {
            Name = "Theme",
            IconFullPath = "icon.png",
            BaseFullPath = "base.wav",
            TractorBeamFullPath = "tb.wav"
        };

    private static void CreatePackage(string packageId)
    {
        Directory.CreateDirectory(Constants.BeePackagesDirectory);
        using var archive = ZipFile.Open(BmpcMetadataStore.GetBeePackagePath(packageId), ZipArchiveMode.Create);
        var entry = archive.CreateEntry("resources/BEE2/bmpc_theme_icon.png");
        using var writer = new StreamWriter(entry.Open());
        writer.Write("packaged icon");
    }
}
