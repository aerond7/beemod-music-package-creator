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
    [InlineData(44_100, 4)]
    [InlineData(48_000, 6)]
    [InlineData(96_000, 8)]
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
    [InlineData(44_100, 4)]
    [InlineData(48_000, 6)]
    [InlineData(96_000, 8)]
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

    [Theory]
    [InlineData(2, 24)]
    [InlineData(6, 24)]
    [InlineData(8, 32)]
    public void Convert_ExtensibleWav_Succeeds(int channels, int bits)
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        CreateToneWav(inputPath, new WaveFormatExtensible(48_000, bits, channels));

        var mp3 = AudioTransformer.ConvertForSampleMp3(inputPath, Path.Combine(temp.Path, "sample.mp3"));
        var wav = AudioTransformer.ConvertForGameWav(inputPath, Path.Combine(temp.Path, "game.wav"));

        Assert.True(mp3.IsSuccessful, mp3.Exception?.ToString());
        Assert.True(wav.IsSuccessful, wav.Exception?.ToString());
    }

    private static void CreateToneWav(string path, int sampleRate, int channels)
        => CreateToneWav(path, new WaveFormat(sampleRate, 16, channels));

    private static void CreateToneWav(string path, WaveFormat format)
    {
        using var writer = new WaveFileWriter(path, format);
        for (var frame = 0; frame < format.SampleRate; frame++) // 1 second
        {
            var sample = (float)(Math.Sin(2 * Math.PI * 440 * frame / format.SampleRate) * 0.25);
            for (var channel = 0; channel < format.Channels; channel++)
            {
                if (format.BitsPerSample == 32)
                {
                    // NAudio's WriteSample writes silence for 32-bit extensible files.
                    writer.Write(BitConverter.GetBytes(sample), 0, 4);
                }
                else
                {
                    writer.WriteSample(sample);
                }
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
