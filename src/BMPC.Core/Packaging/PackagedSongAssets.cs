using BMPC.Core.Models;

namespace BMPC.Core.Packaging
{
    /// <summary>
    /// Locates the assets previously written into an existing BEE package for a song. When editing,
    /// these are what gets shown and edited instead of the source files on disk, which may have changed
    /// or been removed. Packaged game audio shares the original timeline but is trimmed at the loop end
    /// it was encoded with.
    /// </summary>
    public sealed class PackagedSongAssets
    {
        private readonly string baseEntryPath;
        private readonly string tractorBeamEntryPath;
        private readonly string? iconEntryPath;

        private PackagedSongAssets(string beePackPath, PackageSong song)
        {
            var names = new PackageAssetNames(song);
            this.BeePackPath = beePackPath;
            this.BaseFullPath = song.BaseFullPath;
            this.TractorBeamFullPath = song.TractorBeamFullPath;
            this.baseEntryPath = PackageAssetNames.GetGameAudioEntryPath(names.BaseFileName);
            this.tractorBeamEntryPath = PackageAssetNames.GetGameAudioEntryPath(names.FunnelFileName);
            this.iconEntryPath = string.IsNullOrWhiteSpace(song.IconFullPath)
                ? null
                : PackageAssetNames.GetBeeResourceEntryPath(names.IconFileName);
        }

        public string BeePackPath { get; }
        public string BaseFullPath { get; }
        public string? TractorBeamFullPath { get; }

        public static PackagedSongAssets? Find(string packageId, PackageSong song)
        {
            var beePackPath = BmpcMetadataStore.GetBeePackagePath(packageId);
            return File.Exists(beePackPath) ? new PackagedSongAssets(beePackPath, song) : null;
        }

        /// <summary>Returns the package entry encoded from <paramref name="sourcePath"/> as base music, if any.</summary>
        public string? GetBaseEntryPath(string sourcePath)
            => sourcePath == this.BaseFullPath ? this.baseEntryPath : null;

        /// <summary>Returns the package entry encoded from <paramref name="sourcePath"/> as tractor beam music, if any.</summary>
        public string? GetTractorBeamEntryPath(string sourcePath)
            => this.TractorBeamFullPath != null && sourcePath == this.TractorBeamFullPath ? this.tractorBeamEntryPath : null;

        /// <summary>Extracts the packaged icon into <paramref name="directory"/>. Returns null when there is none.</summary>
        public string? ExtractIcon(string directory)
            => this.iconEntryPath != null ? this.ExtractToFile(this.iconEntryPath, directory) : null;

        /// <summary>
        /// Extracts a package entry to a new uniquely named file in <paramref name="directory"/> and returns its
        /// full path. Returns null when it cannot be extracted.
        /// </summary>
        public string? ExtractToFile(string entryPath, string directory)
        {
            var filePath = Path.GetFullPath(Path.Combine(directory, $"bmpc-{Guid.NewGuid():N}{Path.GetExtension(entryPath)}"));

            try
            {
                Directory.CreateDirectory(directory);
                PackageArchiveEntryExtractor.Extract(this.BeePackPath, entryPath, filePath, optional: true);
            }
            catch (Exception)
            {
                BmpcMetadataStore.DeleteIfExists(filePath);
                return null;
            }

            return File.Exists(filePath) ? filePath : null;
        }
    }
}
