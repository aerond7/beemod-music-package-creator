using System.IO.Compression;
using BMPC.Core.Models;
using BMPC.Core.Packaging;

namespace BMPC.Core.Tests;

public class IconAssetWriterTests
{
    private static readonly Guid SongId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    [Fact]
    public void WriteIcon_WhenIconUnchanged_UsesPackagedIconInsteadOfFileOnDisk()
    {
        using var scope = CurrentDirectoryScope.Create();
        var context = CreateContext(scope);
        var song = CreateSong();
        File.WriteAllText(song.IconFullPath, "changed on disk");

        WriteIcon(context, song);

        Assert.Equal("packaged icon", File.ReadAllText(Path.Combine(context.BeeResourcesPath, "bmpc_theme_icon.png")));
        Assert.Equal("packaged icon large", File.ReadAllText(Path.Combine(context.BeeResourcesPath, "bmpc_theme_iconlarge.png")));
    }

    [Fact]
    public void WriteIcon_WhenIconReplaced_CopiesFileFromDiskEvenWithSamePath()
    {
        using var scope = CurrentDirectoryScope.Create();
        var context = CreateContext(scope);
        var song = CreateSong();
        song.IconReplaced = true;
        File.WriteAllText(song.IconFullPath, "changed on disk");

        WriteIcon(context, song);

        Assert.Equal("changed on disk", File.ReadAllText(Path.Combine(context.BeeResourcesPath, "bmpc_theme_icon.png")));
        Assert.Equal("changed on disk", File.ReadAllText(Path.Combine(context.BeeResourcesPath, "bmpc_theme_iconlarge.png")));
    }

    private static void WriteIcon(PackageImportContext context, PackageSong song)
    {
        var cache = new PackageCacheService().GetSongCache(context, song);
        new IconAssetWriter().WriteIcon(context, song, new PackageAssetNames(song), cache);
    }

    private static PackageSong CreateSong()
        => new()
        {
            SongId = SongId,
            Name = "Theme",
            IconFullPath = Path.GetFullPath("icon.png"),
            BaseFullPath = Path.GetFullPath("base.wav")
        };

    private static PackageImportContext CreateContext(CurrentDirectoryScope scope)
    {
        var oldBeePackPath = Path.Combine(scope.DirectoryPath, "old.bee_pack");
        using (var archive = ZipFile.Open(oldBeePackPath, ZipArchiveMode.Create))
        {
            WriteZipEntry(archive, "resources/BEE2/bmpc_theme_icon.png", "packaged icon");
            WriteZipEntry(archive, "resources/BEE2/bmpc_theme_iconlarge.png", "packaged icon large");
        }

        var tempRoot = Path.Combine(scope.DirectoryPath, "import");
        var context = new PackageImportContext
        {
            Data = new PackageData(),
            OldPackageId = "bmpc_test",
            PackageId = "bmpc_test",
            TempRoot = tempRoot,
            BeeResourcesPath = Path.Combine(tempRoot, "resources", "BEE2"),
            BeeSamplePath = Path.Combine(tempRoot, "resources", "music_samp"),
            GameMusicPath = Path.Combine(tempRoot, "resources", "sound", "music"),
            OldBeePackPath = oldBeePackPath,
            OldEditData = new PackageData { Songs = [CreateSong()] }
        };

        Directory.CreateDirectory(context.BeeResourcesPath);
        return context;
    }

    private static void WriteZipEntry(ZipArchive archive, string entryName, string contents)
    {
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(contents);
    }
}
