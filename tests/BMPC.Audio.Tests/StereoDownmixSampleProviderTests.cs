using NAudio.Wave;

namespace BMPC.Audio.Tests;

public class StereoDownmixSampleProviderTests
{
    // 5.1 left side: front left 1 + center 0.707 + back left 0.707, scaled down to 1.
    private const float FiveOneScale = 1 / (1 + 2 * 0.70710677f);

    [Fact]
    public void Read_FiveOneFrontLeft_GoesToLeftOnly()
    {
        var (left, right) = Downmix(6, channel: 0);

        Assert.Equal(FiveOneScale, left, 4);
        Assert.Equal(0, right, 4);
    }

    [Fact]
    public void Read_FiveOneFrontRight_GoesToRightOnly()
    {
        var (left, right) = Downmix(6, channel: 1);

        Assert.Equal(0, left, 4);
        Assert.Equal(FiveOneScale, right, 4);
    }

    [Fact]
    public void Read_FiveOneCenter_GoesToBothSidesEqually()
    {
        var (left, right) = Downmix(6, channel: 2);

        Assert.Equal(0.70710677f * FiveOneScale, left, 4);
        Assert.Equal(left, right, 4);
    }

    [Fact]
    public void Read_FiveOneLfe_IsDropped()
    {
        var (left, right) = Downmix(6, channel: 3);

        Assert.Equal(0, left, 4);
        Assert.Equal(0, right, 4);
    }

    [Fact]
    public void Read_FiveOneBackLeft_GoesToLeftOnly()
    {
        var (left, right) = Downmix(6, channel: 4);

        Assert.Equal(0.70710677f * FiveOneScale, left, 4);
        Assert.Equal(0, right, 4);
    }

    [Fact]
    public void Read_SevenOneSideRight_GoesToRightOnly()
    {
        var (left, right) = Downmix(8, channel: 7);

        Assert.Equal(0, left, 4);
        Assert.True(right > 0);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    [InlineData(7)]
    [InlineData(8)]
    [InlineData(12)]
    public void Read_AllChannelsAtFullScale_DoesNotClip(int channels)
    {
        var source = new ConstantSampleProvider(Enumerable.Repeat(1f, channels).ToArray());
        var downmix = new StereoDownmixSampleProvider(source);
        var buffer = new float[2];

        downmix.Read(buffer, 0, 2);

        Assert.InRange(buffer[0], 0.01f, 1.0001f);
        Assert.InRange(buffer[1], 0.01f, 1.0001f);
        Assert.Equal(buffer[0], buffer[1], 4);
    }

    [Fact]
    public void Read_ReturnsStereoFramesForWholeSourceFrames()
    {
        var source = new ConstantSampleProvider(new float[6]);
        var downmix = new StereoDownmixSampleProvider(source);
        var buffer = new float[200];

        var read = downmix.Read(buffer, 0, buffer.Length);

        Assert.Equal(200, read);
        Assert.Equal(2, downmix.WaveFormat.Channels);
        Assert.Equal(48_000, downmix.WaveFormat.SampleRate);
    }

    [Fact]
    public void ChannelMask_ExtensibleWav_ReturnsSpeakerLayout()
    {
        using var temp = TempDirectory.Create();
        var path = Path.Combine(temp.Path, "quad.wav");
        WriteWav(path, new WaveFormatExtensible(48_000, 16, 4), new float[4]);

        using var reader = new AudioSourceReader(path);

        // NAudio fills the first N speaker bits: front left, front right, center, LFE.
        Assert.Equal(0xF, reader.ChannelMask);
    }

    [Fact]
    public void ChannelMask_PlainWav_ReturnsZero()
    {
        using var temp = TempDirectory.Create();
        var path = Path.Combine(temp.Path, "plain.wav");
        WriteWav(path, new WaveFormat(48_000, 16, 6), new float[6]);

        using var reader = new AudioSourceReader(path);

        Assert.Equal(0, reader.ChannelMask);
    }

    [Fact]
    public void DownmixToStereo_UsesSpeakerLayoutFromFile()
    {
        using var temp = TempDirectory.Create();
        var path = Path.Combine(temp.Path, "quad.wav");
        // The 4th channel is LFE in this file, but would be back right in the default quad layout.
        WriteWav(path, new WaveFormatExtensible(48_000, 16, 4), [0, 0, 0, 0.5f]);
        using var reader = new AudioSourceReader(path);

        var stereo = AudioTransformer.DownmixToStereo(reader);
        var buffer = new float[2];
        stereo.Read(buffer, 0, 2);

        Assert.Equal(0, buffer[0], 4);
        Assert.Equal(0, buffer[1], 4);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void DownmixToStereo_MonoOrStereo_ReturnsReaderUnchanged(int channels)
    {
        using var temp = TempDirectory.Create();
        var path = Path.Combine(temp.Path, "input.wav");
        WriteWav(path, new WaveFormat(48_000, 16, channels), new float[channels]);
        using var reader = new AudioSourceReader(path);

        Assert.Same(reader, AudioTransformer.DownmixToStereo(reader));
    }

    private static (float Left, float Right) Downmix(int channels, int channel)
    {
        var frame = new float[channels];
        frame[channel] = 1;
        var downmix = new StereoDownmixSampleProvider(new ConstantSampleProvider(frame));
        var buffer = new float[2];

        Assert.Equal(2, downmix.Read(buffer, 0, 2));
        return (buffer[0], buffer[1]);
    }

    private static void WriteWav(string path, WaveFormat format, float[] frame)
    {
        using var writer = new WaveFileWriter(path, format);
        for (var i = 0; i < 100; i++)
        {
            writer.WriteSamples(frame, 0, frame.Length);
        }
    }

    // Repeats the same frame forever.
    private sealed class ConstantSampleProvider(float[] frame) : ISampleProvider
    {
        public WaveFormat WaveFormat { get; } = WaveFormat.CreateIeeeFloatWaveFormat(48_000, frame.Length);

        public int Read(float[] buffer, int offset, int count)
        {
            count -= count % frame.Length;
            for (var i = 0; i < count; i++)
            {
                buffer[offset + i] = frame[i % frame.Length];
            }

            return count;
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
