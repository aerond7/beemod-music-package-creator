using BMPC.Core.Models;

namespace BMPC.Core.Packaging
{
    public sealed class PackageCacheService
    {
        public PackageSongCache GetSongCache(PackageImportContext context, PackageSong song)
        {
            var oldSong = FindOldSong(context.OldEditData, song);
            var oldBaseFileName = oldSong != null
                ? Utils.ConvertToSafeFileName(oldSong.Name).ToLowerInvariant()
                : Utils.ConvertToSafeFileName(song.Name).ToLowerInvariant();

            return new PackageSongCache(
                context.OldBeePackPath,
                oldSong,
                oldBaseFileName,
                oldBaseFileName + "_tb");
        }

        private static PackageSong? FindOldSong(PackageData? oldEditData, PackageSong song)
        {
            if (oldEditData == null)
            {
                return null;
            }

            PackageSong? oldSong = null;
            if (song.SongId.HasValue)
            {
                oldSong = oldEditData.Songs.FirstOrDefault(s => s.SongId == song.SongId);
            }

            return oldSong ?? oldEditData.Songs.FirstOrDefault(s => s.BaseFullPath == song.BaseFullPath);
        }
    }

    public sealed class PackageSongCache
    {
        public PackageSongCache(string? oldBeePackPath, PackageSong? oldSong, string oldBaseFileName, string oldFunnelFileName)
        {
            this.OldBeePackPath = oldBeePackPath;
            this.OldSong = oldSong;
            this.OldBaseFileName = oldBaseFileName;
            this.OldFunnelFileName = oldFunnelFileName;
        }

        public string? OldBeePackPath { get; }
        public PackageSong? OldSong { get; }
        public string OldBaseFileName { get; }
        public string OldFunnelFileName { get; }

        public bool IsBaseAudioCached(PackageSong song)
            => this.OldBeePackPath != null
                && this.OldSong != null
                && song.BaseFullPath == this.OldSong.BaseFullPath
                && AreLoopPointsEqual(song.BaseLoopPoints, this.OldSong.BaseLoopPoints);

        public bool IsTractorBeamCached(PackageSong song)
            => this.OldBeePackPath != null
                && this.OldSong != null
                && song.TractorBeamFullPath != null
                && song.TractorBeamFullPath == this.OldSong.TractorBeamFullPath
                && AreLoopPointsEqual(song.TractorBeamLoopPoints, this.OldSong.TractorBeamLoopPoints);

        public bool IsIconCached(PackageSong song)
            => this.OldBeePackPath != null
                && this.OldSong != null
                && !string.IsNullOrWhiteSpace(song.IconFullPath)
                && song.IconFullPath == this.OldSong.IconFullPath;

        public bool IsSpeedGelSfxCached(PackageSong song, int index)
            => this.OldBeePackPath != null
                && this.OldSong != null
                && index < this.OldSong.SpeedGelSfxFullPaths.Count
                && song.SpeedGelSfxFullPaths[index] == this.OldSong.SpeedGelSfxFullPaths[index];

        public bool IsBounceGelSfxCached(PackageSong song, int index)
            => this.OldBeePackPath != null
                && this.OldSong != null
                && index < this.OldSong.BounceGelSfxFullPaths.Count
                && song.BounceGelSfxFullPaths[index] == this.OldSong.BounceGelSfxFullPaths[index];

        private static bool AreLoopPointsEqual(BMPC.Audio.Objects.AudioLoopPoints? left, BMPC.Audio.Objects.AudioLoopPoints? right)
            => left?.IsEnabled == right?.IsEnabled
                && left?.StartSeconds == right?.StartSeconds
                && left?.EndSeconds == right?.EndSeconds;
    }
}
