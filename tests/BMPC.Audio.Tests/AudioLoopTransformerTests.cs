using System.Text;
using BMPC.Audio.Objects;

namespace BMPC.Audio.Tests;

public class AudioLoopTransformerTests
{
    [Fact]
    public void AddLoopPointsToWav_AppendsCueChunk()
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        var outputPath = Path.Combine(temp.Path, "output.wav");
        var input = CreateMinimalWav(dataLength: 4);
        File.WriteAllBytes(inputPath, input);

        AudioLoopTransformer.AddLoopPointsToWav(inputPath, outputPath);

        var output = File.ReadAllBytes(outputPath);
        var cue = IndexOf(output, Encoding.ASCII.GetBytes("cue "));
        Assert.True(cue > 0);
        Assert.Equal(28, BitConverter.ToInt32(output, cue + 4));  // chunk data size
        Assert.Equal(1, BitConverter.ToInt32(output, cue + 8));   // dwCuePoints
        // A full-track loop starts at frame 0.
        Assert.Equal(0u, BitConverter.ToUInt32(output, cue + 32)); // dwSampleOffset
        Assert.Equal(output.Length - 8, BitConverter.ToInt32(output, 4));
    }

    [Fact]
    public void AddLoopPointsToWav_WithExplicitLoopStart_WritesSampleOffset()
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        var outputPath = Path.Combine(temp.Path, "output.wav");
        File.WriteAllBytes(inputPath, CreateMinimalWav(dataLength: 88_200)); // 44100 mono frames = 1s

        AudioLoopTransformer.AddLoopPointsToWav(inputPath, outputPath, new AudioLoopPoints
        {
            StartSeconds = 0.25,
            EndSeconds = 0.75
        });

        var output = File.ReadAllBytes(outputPath);
        var cue = IndexOf(output, Encoding.ASCII.GetBytes("cue "));
        Assert.True(cue > 0);
        // PCM source: cue offset is the plain sample-frame index (no ADPCM halving).
        Assert.Equal(11_025u, BitConverter.ToUInt32(output, cue + 16)); // dwPosition
        Assert.Equal(11_025u, BitConverter.ToUInt32(output, cue + 32)); // dwSampleOffset
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
    public void AddLoopPointsToWav_CanUpdateInputFileInPlace()
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        var input = CreateMinimalWav(dataLength: 4);
        File.WriteAllBytes(inputPath, input);

        AudioLoopTransformer.AddLoopPointsToWav(inputPath, inputPath);

        var output = File.ReadAllBytes(inputPath);
        Assert.True(IndexOf(output, Encoding.ASCII.GetBytes("cue ")) > 0);
        Assert.Equal(output.Length - 8, BitConverter.ToInt32(output, 4));
    }

    [Fact]
    public void ReadLoopInfo_WithExistingCue_ReturnsLoopStartInSeconds()
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        File.WriteAllBytes(inputPath, AppendBytes(CreateMinimalWav(dataLength: 88_200), CreateCueChunk(sampleOffset: 11_025)));

        var info = AudioLoopTransformer.ReadLoopInfo(inputPath);

        Assert.Equal(44100, info.SampleRate);
        Assert.NotNull(info.ExistingLoopPoints);
        Assert.Equal(0.25, info.ExistingLoopPoints.StartSeconds, precision: 3);
        // Cue chunks carry no end, so the loop end is the track duration.
        Assert.Equal(info.DurationSeconds, info.ExistingLoopPoints.EndSeconds, precision: 3);
    }

    [Fact]
    public void ReadLoopInfo_WithExistingSmpl_ReturnsLoopPointsInSeconds()
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        File.WriteAllBytes(inputPath, AppendBytes(CreateMinimalWav(dataLength: 88_200), CreateSmplChunk(startSample: 11_025, endSample: 33_075)));

        var info = AudioLoopTransformer.ReadLoopInfo(inputPath);

        Assert.NotNull(info.ExistingLoopPoints);
        Assert.Equal(0.25, info.ExistingLoopPoints.StartSeconds, precision: 3);
        Assert.Equal(0.75, info.ExistingLoopPoints.EndSeconds, precision: 3);
    }

    [Fact]
    public void AddLoopPointsToWav_ReprocessingDoesNotDuplicateCueChunk()
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        var outputPath = Path.Combine(temp.Path, "output.wav");
        File.WriteAllBytes(inputPath, CreateMinimalWav(dataLength: 88_200));

        AudioLoopTransformer.AddLoopPointsToWav(inputPath, outputPath);
        AudioLoopTransformer.AddLoopPointsToWav(outputPath, outputPath);

        var output = File.ReadAllBytes(outputPath);
        Assert.Equal(1, CountOccurrences(output, Encoding.ASCII.GetBytes("cue ")));
        Assert.Equal(output.Length - 8, BitConverter.ToInt32(output, 4));
    }

    [Fact]
    public void AddLoopPointsToWav_OddDataChunk_AlignsCueToEvenOffset()
    {
        using var temp = TempDirectory.Create();
        var inputPath = Path.Combine(temp.Path, "input.wav");
        var outputPath = Path.Combine(temp.Path, "output.wav");
        // An odd-length data chunk forces a word-alignment pad byte before the cue chunk.
        File.WriteAllBytes(inputPath, CreateMinimalWav(dataLength: 88_201));

        AudioLoopTransformer.AddLoopPointsToWav(inputPath, outputPath);

        var output = File.ReadAllBytes(outputPath);
        var cue = IndexOf(output, Encoding.ASCII.GetBytes("cue "));
        Assert.True(cue > 0);
        Assert.Equal(0, cue % 2);
        Assert.Equal(output.Length - 8, BitConverter.ToInt32(output, 4));
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

    private static byte[] CreateCueChunk(int sampleOffset)
    {
        var bytes = new byte[36];
        Encoding.ASCII.GetBytes("cue ").CopyTo(bytes, 0);
        BitConverter.GetBytes(28).CopyTo(bytes, 4);
        BitConverter.GetBytes(1).CopyTo(bytes, 8);   // dwCuePoints
        BitConverter.GetBytes(1).CopyTo(bytes, 12);  // dwName
        BitConverter.GetBytes(sampleOffset).CopyTo(bytes, 16); // dwPosition
        Encoding.ASCII.GetBytes("data").CopyTo(bytes, 20);
        BitConverter.GetBytes(sampleOffset).CopyTo(bytes, 32); // dwSampleOffset
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

    private static int CountOccurrences(byte[] haystack, byte[] needle)
    {
        var count = 0;
        for (var i = 0; i + needle.Length <= haystack.Length; i++)
        {
            if (Matches(haystack, i, needle))
            {
                count++;
            }
        }

        return count;
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i + needle.Length <= haystack.Length; i++)
        {
            if (Matches(haystack, i, needle))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool Matches(byte[] haystack, int offset, byte[] needle)
    {
        for (var i = 0; i < needle.Length; i++)
        {
            if (haystack[offset + i] != needle[i])
            {
                return false;
            }
        }

        return true;
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
