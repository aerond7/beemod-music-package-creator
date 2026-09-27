using System.IO.Compression;
using BMPC.Audio.Objects;
using BMPC.Core.Models;
using BMPC.Core.Packaging;

namespace BMPC.Core.Tests;

public class AudioAssetWriterTests
{
    private static readonly Guid SongId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    [Fact]
    public void WriteSongAudio_WhenLoopPointsUnchanged_ExtractsPackagedAudioWithoutEncoding()
    {
        using var scope = CurrentDirectoryScope.Create();
        var transformer = new RecordingAudioTransformer();
        var context = CreateContext(scope, CreateOldSong());
        var song = CreateOldSong();

        WriteSongAudio(transformer, context, song);

        Assert.Empty(transformer.Calls);
        Assert.Equal("packaged sample", File.ReadAllText(Path.Combine(context.BeeSamplePath, "bmpc_sample_theme.mp3")));
        Assert.Equal("packaged wav", File.ReadAllText(Path.Combine(context.GameMusicPath, "bmpc_theme.wav")));
        Assert.Equal("packaged tb wav", File.ReadAllText(Path.Combine(context.GameMusicPath, "bmpc_theme_tb.wav")));
        Assert.Equal("packaged speed", File.ReadAllText(Path.Combine(context.GameMusicPath, "bmpc_theme_speed0.wav")));
    }

    [Fact]
    public void WriteSongAudio_WhenLoopPointsChanged_ReencodesPackagedAudioInsteadOfSourceFiles()
    {
        using var scope = CurrentDirectoryScope.Create();
        var transformer = new RecordingAudioTransformer();
        var context = CreateContext(scope, CreateOldSong());
        var song = CreateOldSong();
        CreateSourceFiles(song);
        song.BaseLoopPoints = new AudioLoopPoints { StartSeconds = 2, EndSeconds = 5 };
        song.TractorBeamLoopPoints = new AudioLoopPoints { StartSeconds = 1, EndSeconds = 4 };

        WriteSongAudio(transformer, context, song);

        Assert.Collection(
            transformer.Calls,
            call =>
            {
                Assert.Equal(nameof(IAudioTransformer.ConvertForGameWav), call.Method);
                Assert.Equal("packaged wav", call.InputContents);
                Assert.Same(song.BaseLoopPoints, call.LoopPoints);
                Assert.False(File.Exists(call.InputPath));
            },
            call =>
            {
                Assert.Equal(nameof(IAudioTransformer.ConvertForGameWav), call.Method);
                Assert.Equal("packaged tb wav", call.InputContents);
                Assert.Same(song.TractorBeamLoopPoints, call.LoopPoints);
                Assert.False(File.Exists(call.InputPath));
            });
        Assert.Equal("packaged sample", File.ReadAllText(Path.Combine(context.BeeSamplePath, "bmpc_sample_theme.mp3")));
        Assert.Equal("packaged tb sample", File.ReadAllText(Path.Combine(context.BeeSamplePath, "bmpc_sample_theme_tb.mp3")));
    }

    [Fact]
    public void WriteSongAudio_WhenTracksReplaced_EncodesFromSourceFilesEvenWithSamePaths()
    {
        using var scope = CurrentDirectoryScope.Create();
        var transformer = new RecordingAudioTransformer();
        var context = CreateContext(scope, CreateOldSong());
        var song = CreateOldSong();
        CreateSourceFiles(song);
        song.BaseAudioReplaced = true;
        song.TractorBeamAudioReplaced = true;
        song.SpeedGelSfxReplaced = true;

        WriteSongAudio(transformer, context, song);

        Assert.Equal(
            [
                (nameof(IAudioTransformer.ConvertForSampleMp3), "source base"),
                (nameof(IAudioTransformer.ConvertForGameWav), "source base"),
                (nameof(IAudioTransformer.ConvertForSampleMp3), "source tb"),
                (nameof(IAudioTransformer.ConvertForGameWav), "source tb"),
                (nameof(IAudioTransformer.ConvertForGameWav), "source speed")
            ],
            transformer.Calls.Select(c => (c.Method, c.InputContents)));
    }

    private static void WriteSongAudio(RecordingAudioTransformer transformer, PackageImportContext context, PackageSong song)
    {
        var cache = new PackageCacheService().GetSongCache(context, song);
        new AudioAssetWriter(transformer).WriteSongAudio(context, song, new PackageAssetNames(song), cache, new Progress<string>());
    }

    private static PackageSong CreateOldSong()
        => new()
        {
            SongId = SongId,
            Name = "Theme",
            BaseFullPath = Path.GetFullPath("source-base.wav"),
            BaseLoopPoints = new AudioLoopPoints { StartSeconds = 1, EndSeconds = 8 },
            TractorBeamFullPath = Path.GetFullPath("source-tb.wav"),
            TractorBeamLoopPoints = new AudioLoopPoints { StartSeconds = 0, EndSeconds = 6 },
            SpeedGelSfxFullPaths = [Path.GetFullPath("source-speed.wav")]
        };

    private static void CreateSourceFiles(PackageSong song)
    {
        File.WriteAllText(song.BaseFullPath, "source base");
        File.WriteAllText(song.TractorBeamFullPath!, "source tb");
        File.WriteAllText(song.SpeedGelSfxFullPaths[0], "source speed");
    }

    private static PackageImportContext CreateContext(CurrentDirectoryScope scope, PackageSong oldSong)
    {
        var oldBeePackPath = Path.Combine(scope.DirectoryPath, "old.bee_pack");
        using (var archive = ZipFile.Open(oldBeePackPath, ZipArchiveMode.Create))
        {
            WriteZipEntry(archive, "resources/music_samp/bmpc_sample_theme.mp3", "packaged sample");
            WriteZipEntry(archive, "resources/sound/music/bmpc_theme.wav", "packaged wav");
            WriteZipEntry(archive, "resources/music_samp/bmpc_sample_theme_tb.mp3", "packaged tb sample");
            WriteZipEntry(archive, "resources/sound/music/bmpc_theme_tb.wav", "packaged tb wav");
            WriteZipEntry(archive, "resources/sound/music/bmpc_theme_speed0.wav", "packaged speed");
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
            OldEditData = new PackageData { Songs = [oldSong] }
        };

        Directory.CreateDirectory(context.BeeSamplePath);
        Directory.CreateDirectory(context.GameMusicPath);
        return context;
    }

    private static void WriteZipEntry(ZipArchive archive, string entryName, string contents)
    {
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(contents);
    }

    private sealed record TransformCall(string Method, string InputPath, string InputContents, AudioLoopPoints? LoopPoints);

    private sealed class RecordingAudioTransformer : IAudioTransformer
    {
        public List<TransformCall> Calls { get; } = [];

        public AudioTransformResult ConvertForGameWav(string inputFilePath, string outputFilePath, AudioLoopPoints? loopPoints = null)
            => this.Record(nameof(ConvertForGameWav), inputFilePath, outputFilePath, loopPoints);

        public AudioTransformResult ConvertForSampleMp3(string inputFilePath, string outputFilePath)
            => this.Record(nameof(ConvertForSampleMp3), inputFilePath, outputFilePath, null);

        private AudioTransformResult Record(string method, string inputFilePath, string outputFilePath, AudioLoopPoints? loopPoints)
        {
            this.Calls.Add(new TransformCall(method, inputFilePath, File.ReadAllText(inputFilePath), loopPoints));
            File.WriteAllText(outputFilePath, "encoded");
            return new AudioTransformResult { IsSuccessful = true };
        }
    }
}
