using System.Text;
using BMPC.Audio.Objects;

namespace BMPC.Audio.Tests;

public class AudioLoopTransformerTests
{
    [Fact]
    public void AddLoopPointsToWav_AppendsSmplChunk()
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        var outputPath = Path.Combine(temp.Path, "output.wav");
        var input = CreateMinimalWav(dataLength: 4);
        File.WriteAllBytes(inputPath, input);

        AudioLoopTransformer.AddLoopPointsToWav(inputPath, outputPath);

        var output = File.ReadAllBytes(outputPath);
        Assert.Equal(input.Length + 68, output.Length);
        Assert.Equal("smpl", Encoding.ASCII.GetString(output, input.Length, 4));
        Assert.Equal(60, BitConverter.ToInt32(output, input.Length + 4));
        Assert.Equal(1, BitConverter.ToInt32(output, input.Length + 36));
        Assert.Equal(0, BitConverter.ToInt32(output, input.Length + 52));
        Assert.Equal(2, BitConverter.ToInt32(output, input.Length + 56));
    }

    [Fact]
    public void AddLoopPointsToWav_UpdatesRiffSize()
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        var outputPath = Path.Combine(temp.Path, "output.wav");
        File.WriteAllBytes(inputPath, CreateMinimalWav(dataLength: 8));

        AudioLoopTransformer.AddLoopPointsToWav(inputPath, outputPath);

        var output = File.ReadAllBytes(outputPath);
        Assert.Equal(output.Length - 8, BitConverter.ToInt32(output, 4));
    }

    [Fact]
    public void AddLoopPointsToWav_UsesExistingSmplLoopPoints()
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        var existingPath = Path.Combine(temp.Path, "existing.wav");
        var outputPath = Path.Combine(temp.Path, "output.wav");
        var input = CreateMinimalWav(dataLength: 4);
        var customSmplChunk = CreateSmplChunk(startSample: 1, endSample: 2);
        File.WriteAllBytes(inputPath, input);
        File.WriteAllBytes(existingPath, AppendBytes(CreateMinimalWav(dataLength: 4), customSmplChunk));

        AudioLoopTransformer.AddLoopPointsToWav(inputPath, outputPath, existingPath);

        var output = File.ReadAllBytes(outputPath);
        Assert.Equal(1, BitConverter.ToInt32(output, input.Length + 52));
        Assert.Equal(2, BitConverter.ToInt32(output, input.Length + 56));
    }

    [Fact]
    public void AddLoopPointsToWav_WithExplicitLoopPoints_WritesSamplePositions()
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        var outputPath = Path.Combine(temp.Path, "output.wav");
        var input = CreateMinimalWav(dataLength: 88_200);
        File.WriteAllBytes(inputPath, input);

        AudioLoopTransformer.AddLoopPointsToWav(inputPath, outputPath, loopPoints: new AudioLoopPoints
        {
            StartSeconds = 0.25,
            EndSeconds = 0.75
        });

        var output = File.ReadAllBytes(outputPath);
        Assert.Equal(11_025, BitConverter.ToInt32(output, input.Length + 52));
        Assert.Equal(33_075, BitConverter.ToInt32(output, input.Length + 56));
    }

    [Fact]
    public void ReadLoopInfo_WithExistingSmpl_ReturnsLoopPointsInSeconds()
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        File.WriteAllBytes(inputPath, AppendBytes(CreateMinimalWav(dataLength: 88_200), CreateSmplChunk(startSample: 11_025, endSample: 33_075)));

        var info = AudioLoopTransformer.ReadLoopInfo(inputPath);

        Assert.Equal(44100, info.SampleRate);
        Assert.NotNull(info.ExistingLoopPoints);
        Assert.Equal(0.25, info.ExistingLoopPoints.StartSeconds, precision: 3);
        Assert.Equal(0.75, info.ExistingLoopPoints.EndSeconds, precision: 3);
    }

    [Fact]
    public void AddLoopPointsToWav_WhenRiffHeaderMissing_DoesNotWriteOutput()
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "invalid.wav");
        var outputPath = Path.Combine(temp.Path, "output.wav");
        File.WriteAllBytes(inputPath, Encoding.ASCII.GetBytes("not a riff file"));

        AudioLoopTransformer.AddLoopPointsToWav(inputPath, outputPath);

        Assert.False(File.Exists(outputPath));
    }

    private static byte[] CreateMinimalWav(int dataLength)
    {
        var bytes = new byte[44 + dataLength];
        Encoding.ASCII.GetBytes("RIFF").CopyTo(bytes, 0);
        BitConverter.GetBytes(bytes.Length - 8).CopyTo(bytes, 4);
        Encoding.ASCII.GetBytes("WAVE").CopyTo(bytes, 8);
        Encoding.ASCII.GetBytes("fmt ").CopyTo(bytes, 12);
        BitConverter.GetBytes(16).CopyTo(bytes, 16);
        BitConverter.GetBytes((short)1).CopyTo(bytes, 20);
        BitConverter.GetBytes((short)1).CopyTo(bytes, 22);
        BitConverter.GetBytes(44100).CopyTo(bytes, 24);
        BitConverter.GetBytes(88200).CopyTo(bytes, 28);
        BitConverter.GetBytes((short)2).CopyTo(bytes, 32);
        BitConverter.GetBytes((short)16).CopyTo(bytes, 34);
        Encoding.ASCII.GetBytes("data").CopyTo(bytes, 36);
        BitConverter.GetBytes(dataLength).CopyTo(bytes, 40);

        for (var i = 0; i < dataLength; i++)
        {
            bytes[44 + i] = (byte)(i + 1);
        }

        return bytes;
    }

    private static byte[] CreateSmplChunk(int startSample, int endSample)
    {
        var bytes = new byte[68];
        Encoding.ASCII.GetBytes("smpl").CopyTo(bytes, 0);
        BitConverter.GetBytes(60).CopyTo(bytes, 4);
        BitConverter.GetBytes(22676).CopyTo(bytes, 16);
        BitConverter.GetBytes(1).CopyTo(bytes, 36);
        BitConverter.GetBytes(startSample).CopyTo(bytes, 52);
        BitConverter.GetBytes(endSample).CopyTo(bytes, 56);
        return bytes;
    }

    private static byte[] AppendBytes(byte[] left, byte[] right)
    {
        var result = new byte[left.Length + right.Length];
        left.CopyTo(result, 0);
        right.CopyTo(result, left.Length);
        return result;
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
