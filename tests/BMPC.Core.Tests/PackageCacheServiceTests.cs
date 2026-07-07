using BMPC.Audio.Objects;
using BMPC.Core.Models;
using BMPC.Core.Packaging;

namespace BMPC.Core.Tests;

public class PackageCacheServiceTests
{
    [Fact]
    public void GetSongCache_MatchesOldSongBySongIdBeforeBasePath()
    {
        var service = new PackageCacheService();
        var songId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var context = CreateContext(
            new PackageSong { SongId = Guid.NewGuid(), Name = "Wrong", BaseFullPath = "base.wav" },
            new PackageSong { SongId = songId, Name = "Right Song", BaseFullPath = "new-base.wav" });
        var song = new PackageSong { SongId = songId, Name = "Current", BaseFullPath = "base.wav" };

        var cache = service.GetSongCache(context, song);

        Assert.Equal("rightsong", cache.OldBaseFileName);
        Assert.False(cache.IsBaseAudioCached(song));
    }

    [Fact]
    public void GetSongCache_FallsBackToBasePathWhenSongIdMissing()
    {
        var service = new PackageCacheService();
        var context = CreateContext(new PackageSong { Name = "Old Song", BaseFullPath = "same.wav" });
        var song = new PackageSong { Name = "Current", BaseFullPath = "same.wav" };

        var cache = service.GetSongCache(context, song);

        Assert.Equal("oldsong", cache.OldBaseFileName);
        Assert.True(cache.IsBaseAudioCached(song));
    }

    [Fact]
    public void CacheFlags_RequireUnchangedSourcePaths()
    {
        var service = new PackageCacheService();
        var songId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var context = CreateContext(new PackageSong
        {
            SongId = songId,
            Name = "Old Song",
            IconFullPath = "icon.png",
            BaseFullPath = "base.wav",
            TractorBeamFullPath = "tb.wav",
            SpeedGelSfxFullPaths = ["speed.wav"],
            BounceGelSfxFullPaths = ["bounce.wav"]
        });
        var song = new PackageSong
        {
            SongId = songId,
            Name = "Current",
            IconFullPath = "new-icon.png",
            BaseFullPath = "new-base.wav",
            TractorBeamFullPath = "new-tb.wav",
            SpeedGelSfxFullPaths = ["new-speed.wav"],
            BounceGelSfxFullPaths = ["new-bounce.wav"]
        };

        var cache = service.GetSongCache(context, song);

        Assert.False(cache.IsBaseAudioCached(song));
        Assert.False(cache.IsTractorBeamCached(song));
        Assert.False(cache.IsIconCached(song));
        Assert.False(cache.IsSpeedGelSfxCached(song, 0));
        Assert.False(cache.IsBounceGelSfxCached(song, 0));
    }

    [Fact]
    public void CacheFlags_RequireUnchangedLoopPoints()
    {
        var service = new PackageCacheService();
        var songId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var context = CreateContext(new PackageSong
        {
            SongId = songId,
            Name = "Old Song",
            BaseFullPath = "base.wav",
            BaseLoopPoints = new AudioLoopPoints { StartSeconds = 0, EndSeconds = 10 },
            TractorBeamFullPath = "tb.wav",
            TractorBeamLoopPoints = new AudioLoopPoints { StartSeconds = 1, EndSeconds = 9 }
        });
        var song = new PackageSong
        {
            SongId = songId,
            Name = "Current",
            BaseFullPath = "base.wav",
            BaseLoopPoints = new AudioLoopPoints { StartSeconds = 2, EndSeconds = 10 },
            TractorBeamFullPath = "tb.wav",
            TractorBeamLoopPoints = new AudioLoopPoints { StartSeconds = 1, EndSeconds = 8 }
        };

        var cache = service.GetSongCache(context, song);

        Assert.False(cache.IsBaseAudioCached(song));
        Assert.False(cache.IsTractorBeamCached(song));
    }

    private static PackageImportContext CreateContext(params PackageSong[] oldSongs)
        => new()
        {
            Data = new PackageData(),
            PackageId = "BMPC_TEST_PACK",
            TempRoot = "",
            BeeResourcesPath = "",
            BeeSamplePath = "",
            GameMusicPath = "",
            OldBeePackPath = "old.bee_pack",
            OldEditData = new PackageData { Songs = oldSongs.ToList() }
        };
}
