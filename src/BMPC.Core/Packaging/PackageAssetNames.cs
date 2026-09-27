using BMPC.Core.Models;

namespace BMPC.Core.Packaging
{
    public sealed class PackageAssetNames
    {
        public PackageAssetNames(PackageSong song)
        {
            this.BaseFileName = Utils.ConvertToSafeFileName(song.Name).ToLowerInvariant();
            this.FunnelFileName = this.BaseFileName + "_tb";
            this.IconExtension = Path.GetExtension(song.IconFullPath);
        }

        public string BaseFileName { get; }
        public string FunnelFileName { get; }
        public string IconExtension { get; }
        public string BaseSampleFileName => $"bmpc_sample_{this.BaseFileName}.mp3";
        public string BaseGameFileName => $"bmpc_{this.BaseFileName}.wav";
        public string FunnelSampleFileName => $"bmpc_sample_{this.FunnelFileName}.mp3";
        public string FunnelGameFileName => $"bmpc_{this.FunnelFileName}.wav";
        public string IconFileName => $"bmpc_{this.BaseFileName}_icon{this.IconExtension}";
        public string IconLargeFileName => $"bmpc_{this.BaseFileName}_iconlarge{this.IconExtension}";

        public static string GetSampleAudioEntryPath(string fileName)
            => $"resources/music_samp/bmpc_sample_{fileName}.mp3";

        public static string GetGameAudioEntryPath(string fileName)
            => $"resources/sound/music/bmpc_{fileName}.wav";

        public static string GetBeeResourceEntryPath(string fileName)
            => $"resources/BEE2/{fileName}";
    }
}
