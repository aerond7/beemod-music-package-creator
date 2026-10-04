using NAudio.Wave;

namespace BMPC.Audio.Tests;

public class AudioSourceReaderTests
{
    [Theory]
    [InlineData(2, 16)]
    [InlineData(2, 24)]
    [InlineData(6, 16)]
    [InlineData(6, 24)]
    [InlineData(6, 32)]
    [InlineData(8, 24)]
    public void Read_ExtensibleWav_ReadsSamplesAndLength(int channels, int bits)
    {
        using var temp = TempDirectory.Create();
        var path = Path.Combine(temp.Path, "input.wav");
        WriteChannelLevelsWav(path, new WaveFormatExtensible(48_000, bits, channels), frames: 48_000);

        using var reader = new AudioSourceReader(path);
        var frame = new float[channels];
        reader.Read(frame, 0, frame.Length);

        Assert.Equal(48_000, reader.WaveFormat.SampleRate);
        Assert.Equal(channels, reader.WaveFormat.Channels);
        Assert.Equal(WaveFormatEncoding.IeeeFloat, reader.WaveFormat.Encoding);
        Assert.Equal(1.0, reader.TotalTime.TotalSeconds, 3);
        for (var channel = 0; channel < channels; channel++)
        {
            Assert.Equal(ChannelValue(channel), frame[channel], 3);
        }
    }

    [Fact]
    public void CurrentTime_ExtensibleWav_SeeksToFrame()
    {
        using var temp = TempDirectory.Create();
        var path = Path.Combine(temp.Path, "input.wav");
        WriteChannelLevelsWav(path, new WaveFormatExtensible(48_000, 24, 6), frames: 48_000);

        using var reader = new AudioSourceReader(path)
        {
            CurrentTime = TimeSpan.FromSeconds(0.5)
        };

        Assert.Equal(0.5, reader.CurrentTime.TotalSeconds, 3);
        Assert.Equal(24_000L * 6 * 4, reader.Position);
    }

    [Fact]
    public void Dispose_ExtensibleWav_ReleasesFile()
    {
        using var temp = TempDirectory.Create();
        var path = Path.Combine(temp.Path, "input.wav");
        WriteChannelLevelsWav(path, new WaveFormatExtensible(48_000, 16, 6), frames: 100);

        new AudioSourceReader(path).Dispose();

        File.Delete(path);
        Assert.False(File.Exists(path));
    }

    [Fact]
    public void Read_PlainWav_MatchesAudioFileReader()
    {
        using var temp = TempDirectory.Create();
        var path = Path.Combine(temp.Path, "input.wav");
        WriteChannelLevelsWav(path, new WaveFormat(44_100, 16, 2), frames: 1_000);

        using var expected = new AudioFileReader(path);
        using var actual = new AudioSourceReader(path);
        var expectedSamples = new float[2_000];
        var actualSamples = new float[2_000];

        Assert.Equal(expected.Read(expectedSamples, 0, 2_000), actual.Read(actualSamples, 0, 2_000));
        Assert.Equal(expectedSamples, actualSamples);
        Assert.Equal(expected.Length, actual.Length);
        Assert.Equal(expected.WaveFormat.ToString(), actual.WaveFormat.ToString());
    }

    // Each channel holds its own constant level so channel order can be checked.
    private static float ChannelValue(int channel) => (channel + 1) * 0.1f;

    private static void WriteChannelLevelsWav(string path, WaveFormat format, int frames)
    {
        var frame = Enumerable.Range(0, format.Channels).Select(ChannelValue).ToArray();
        using var writer = new WaveFileWriter(path, format);
        for (var i = 0; i < frames; i++)
        {
            if (format.BitsPerSample == 32)
            {
                // NAudio's WriteSamples writes silence for 32-bit extensible files.
                foreach (var sample in frame)
                {
                    writer.Write(BitConverter.GetBytes(sample), 0, 4);
                }
            }
            else
            {
                writer.WriteSamples(frame, 0, frame.Length);
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
