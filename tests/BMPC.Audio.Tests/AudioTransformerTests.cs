using NAudio.Wave;

namespace BMPC.Audio.Tests;

public class AudioTransformerTests
{
    [Theory]
    [InlineData(96_000, 2)]
    [InlineData(22_050, 2)]
    [InlineData(22_050, 1)]
    [InlineData(44_100, 1)]
    [InlineData(44_100, 2)]
    public void ConvertForSampleMp3_AnySourceSampleRate_EncodesStereo44100Mp3(int sampleRate, int channels)
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        var outputPath = Path.Combine(temp.Path, "sample.mp3");
        CreateToneWav(inputPath, sampleRate, channels);

        var result = AudioTransformer.ConvertForSampleMp3(inputPath, outputPath);

        Assert.True(result.IsSuccessful, result.Exception?.ToString());
        using var mp3 = new MediaFoundationReader(outputPath);
        Assert.Equal(44_100, mp3.WaveFormat.SampleRate);
        Assert.Equal(2, mp3.WaveFormat.Channels);
    }

    [Theory]
    [InlineData(96_000, 2)]
    [InlineData(22_050, 1)]
    public void ConvertForGameWav_AnySourceSampleRate_WritesStereo44100Adpcm(int sampleRate, int channels)
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        var outputPath = Path.Combine(temp.Path, "game.wav");
        CreateToneWav(inputPath, sampleRate, channels);

        var result = AudioTransformer.ConvertForGameWav(inputPath, outputPath);

        Assert.True(result.IsSuccessful, result.Exception?.ToString());
        using var wav = new WaveFileReader(outputPath);
        Assert.Equal(WaveFormatEncoding.Adpcm, wav.WaveFormat.Encoding);
        Assert.Equal(44_100, wav.WaveFormat.SampleRate);
        Assert.Equal(2, wav.WaveFormat.Channels);
    }

    private static void CreateToneWav(string path, int sampleRate, int channels)
    {
        using var writer = new WaveFileWriter(path, new WaveFormat(sampleRate, 16, channels));
        for (var frame = 0; frame < sampleRate; frame++) // 1 second
        {
            var sample = (float)(Math.Sin(2 * Math.PI * 440 * frame / sampleRate) * 0.25);
            for (var channel = 0; channel < channels; channel++)
            {
                writer.WriteSample(sample);
            }
        }
    }

    private sealed class TempDirectory : IDisposable
    {
        private TempDirectory(string path)
        {
            this.Path = path;
        }

        public string Path { get; }

        public static TempDirectory Create()
        {
            var path = System.IO.Path.Combine(AppContext.BaseDirectory, "Temp", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return new TempDirectory(path);
        }

        public void Dispose()
            => Directory.Delete(this.Path, recursive: true);
    }
}
