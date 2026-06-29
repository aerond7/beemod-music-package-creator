using BMPC.Core.Models;

namespace BMPC.Core.Packaging
{
    public sealed class IconAssetWriter
    {
        public void WriteIcon(
            PackageImportContext context,
            PackageSong song,
            PackageAssetNames names,
            PackageSongCache cache)
        {
            if (cache.IsIconCached(song))
            {
                var oldIconExt = Path.GetExtension(cache.OldSong!.IconFullPath);
                PackageArchiveEntryExtractor.Extract(
                    cache.OldBeePackPath!,
                    $"resources/BEE2/bmpc_{cache.OldBaseFileName}_icon{oldIconExt}",
                    Path.Combine(context.BeeResourcesPath, names.IconFileName),
                    optional: true);
                PackageArchiveEntryExtractor.Extract(
                    cache.OldBeePackPath!,
                    $"resources/BEE2/bmpc_{cache.OldBaseFileName}_iconlarge{oldIconExt}",
                    Path.Combine(context.BeeResourcesPath, names.IconLargeFileName),
                    optional: true);
                return;
            }

            if (string.IsNullOrWhiteSpace(song.IconFullPath) || !File.Exists(song.IconFullPath))
            {
                return;
            }

            File.Copy(song.IconFullPath, Path.Combine(context.BeeResourcesPath, names.IconFileName));
            File.Copy(song.IconFullPath, Path.Combine(context.BeeResourcesPath, names.IconLargeFileName));
        }
    }
}
