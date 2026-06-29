using BMPC.Audio;
using BMPC.Audio.Objects;
using BMPC.Core.Models;

namespace BMPC.Core.Packaging
{
    public interface IAudioTransformer
    {
        AudioTransformResult ConvertForGameWav(string inputFilePath, string outputFilePath);
        AudioTransformResult ConvertForSampleMp3(string inputFilePath, string outputFilePath);
    }

    public sealed class AudioTransformerAdapter : IAudioTransformer
    {
        public AudioTransformResult ConvertForGameWav(string inputFilePath, string outputFilePath)
            => AudioTransformer.ConvertForGameWav(inputFilePath, outputFilePath);

        public AudioTransformResult ConvertForSampleMp3(string inputFilePath, string outputFilePath)
            => AudioTransformer.ConvertForSampleMp3(inputFilePath, outputFilePath);
    }

    public sealed class AudioAssetWriter
    {
        private readonly IAudioTransformer audioTransformer;

        public AudioAssetWriter(IAudioTransformer? audioTransformer = null)
        {
            this.audioTransformer = audioTransformer ?? new AudioTransformerAdapter();
        }

        public AudioAssetWriteResult WriteSongAudio(
            PackageImportContext context,
            PackageSong song,
            PackageAssetNames names,
            PackageSongCache cache,
            IProgress<string> progress)
        {
            this.WriteBaseAudio(context, song, names, cache, progress);
            this.WriteTractorBeamAudio(context, song, names, cache, progress);

            var speedGelSfxs = this.WriteGelSfxs(
                song.SpeedGelSfxFullPaths,
                cache.OldBeePackPath,
                context.GameMusicPath,
                i => $"bmpc_{names.BaseFileName}_speed{i}.wav",
                i => $"bmpc_{cache.OldBaseFileName}_speed{i}.wav",
                i => cache.IsSpeedGelSfxCached(song, i),
                "speed gel");

            var bounceGelSfxs = this.WriteGelSfxs(
                song.BounceGelSfxFullPaths,
                cache.OldBeePackPath,
                context.GameMusicPath,
                i => $"bmpc_{names.BaseFileName}_bounce{i}.wav",
                i => $"bmpc_{cache.OldBaseFileName}_bounce{i}.wav",
                i => cache.IsBounceGelSfxCached(song, i),
                "bounce gel");

            return new AudioAssetWriteResult(speedGelSfxs, bounceGelSfxs);
        }

        private void WriteBaseAudio(
            PackageImportContext context,
            PackageSong song,
            PackageAssetNames names,
            PackageSongCache cache,
            IProgress<string> progress)
        {
            if (cache.IsBaseAudioCached(song))
            {
                progress.Report($"Using cached audio for {song.Name}...");
                PackageArchiveEntryExtractor.Extract(
                    cache.OldBeePackPath!,
                    $"resources/music_samp/bmpc_sample_{cache.OldBaseFileName}.mp3",
                    Path.Combine(context.BeeSamplePath, names.BaseSampleFileName));
                PackageArchiveEntryExtractor.Extract(
                    cache.OldBeePackPath!,
                    $"resources/sound/music/bmpc_{cache.OldBaseFileName}.wav",
                    Path.Combine(context.GameMusicPath, names.BaseGameFileName));
                return;
            }

            progress.Report("Encoding sample audio...");
            ThrowIfFailed(
                this.audioTransformer.ConvertForSampleMp3(song.BaseFullPath, Path.Combine(context.BeeSamplePath, names.BaseSampleFileName)),
                "Failed to resample base music for BEE sample");

            progress.Report("Encoding game audio...");
            ThrowIfFailed(
                this.audioTransformer.ConvertForGameWav(song.BaseFullPath, Path.Combine(context.GameMusicPath, names.BaseGameFileName)),
                "Failed to resample base music for game");
        }

        private void WriteTractorBeamAudio(
            PackageImportContext context,
            PackageSong song,
            PackageAssetNames names,
            PackageSongCache cache,
            IProgress<string> progress)
        {
            if (song.TractorBeamFullPath == null)
            {
                return;
            }

            if (cache.IsTractorBeamCached(song))
            {
                progress.Report("Using cached tractor beam audio...");
                PackageArchiveEntryExtractor.Extract(
                    cache.OldBeePackPath!,
                    $"resources/music_samp/bmpc_sample_{cache.OldFunnelFileName}.mp3",
                    Path.Combine(context.BeeSamplePath, names.FunnelSampleFileName));
                PackageArchiveEntryExtractor.Extract(
                    cache.OldBeePackPath!,
                    $"resources/sound/music/bmpc_{cache.OldFunnelFileName}.wav",
                    Path.Combine(context.GameMusicPath, names.FunnelGameFileName));
                return;
            }

            ThrowIfFailed(
                this.audioTransformer.ConvertForSampleMp3(song.TractorBeamFullPath, Path.Combine(context.BeeSamplePath, names.FunnelSampleFileName)),
                "Failed to resample funnel music for BEE sample");
            ThrowIfFailed(
                this.audioTransformer.ConvertForGameWav(song.TractorBeamFullPath, Path.Combine(context.GameMusicPath, names.FunnelGameFileName)),
                "Failed to resample funnel music for game");
        }

        private List<string> WriteGelSfxs(
            IReadOnlyList<string> sfxPaths,
            string? oldBeePackPath,
            string gameMusicPath,
            Func<int, string> getFileName,
            Func<int, string> getOldFileName,
            Func<int, bool> isCached,
            string sfxName)
        {
            var writtenFiles = new List<string>();

            for (var i = 0; i < sfxPaths.Count; i++)
            {
                var fileName = getFileName(i);
                var packPath = Path.Combine(gameMusicPath, fileName);

                if (isCached(i))
                {
                    PackageArchiveEntryExtractor.Extract(
                        oldBeePackPath ?? throw new InvalidOperationException("Old package path is required for cached SFX extraction."),
                        $"resources/sound/music/{getOldFileName(i)}",
                        packPath);
                }
                else
                {
                    ThrowIfFailed(
                        this.audioTransformer.ConvertForGameWav(sfxPaths[i], packPath),
                        $"Failed to resample {sfxName} sfx for game");
                }

                writtenFiles.Add(fileName);
            }

            return writtenFiles;
        }

        private static void ThrowIfFailed(AudioTransformResult result, string message)
        {
            if (!result.IsSuccessful)
            {
                throw result.Exception ?? new InvalidOperationException(message);
            }
        }
    }

    public sealed record AudioAssetWriteResult(List<string> SpeedGelSfxs, List<string> BounceGelSfxs);
}
