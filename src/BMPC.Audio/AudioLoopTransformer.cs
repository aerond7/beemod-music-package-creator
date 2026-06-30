using BMPC.Audio.Objects;
using NAudio.Wave;
using System.Text;

namespace BMPC.Audio
{
    public static class AudioLoopTransformer
    {
        private const int SmplChunkLength = 68;
        private const int SmplChunkDataLength = 60;

        public static AudioLoopInfo ReadLoopInfo(string filePath)
        {
            using var reader = new AudioFileReader(filePath);
            var durationSeconds = reader.TotalTime.TotalSeconds;
            var sampleRate = reader.WaveFormat.SampleRate;

            return new AudioLoopInfo
            {
                SampleRate = sampleRate,
                DurationSeconds = durationSeconds,
                TotalSamples = SecondsToSamples(durationSeconds, sampleRate),
                ExistingLoopPoints = TryReadSmplLoopPoints(filePath)
            };
        }

        public static void AddLoopPointsToWav(
            string inputPath,
            string outputPath,
            string? existingFilePath = null,
            AudioLoopPoints? loopPoints = null)
        {
            var waveData = File.ReadAllBytes(inputPath);
            if (!TryReadWaveLayout(waveData, out var layout))
            {
                return;
            }

            AudioLoopPoints? points = loopPoints?.Clone()
                ?? (existingFilePath is not null ? TryReadSmplLoopPoints(existingFilePath) : null);

            points ??= CreateFullFileLoop(layout);
            points = NormalizeLoop(points, layout.DurationSeconds);

            waveData = RemoveChunk(waveData, "smpl");

            if (points.IsEnabled)
            {
                var smplChunk = CreateSmplChunk(points, layout.SampleRate, layout.TotalSamples);
                waveData = AppendBytes(waveData, smplChunk);
            }

            WriteRiffSize(waveData);
            File.WriteAllBytes(outputPath, waveData);
        }

        private static AudioLoopPoints? TryReadSmplLoopPoints(string filePath)
        {
            if (!Path.GetExtension(filePath).Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var waveData = File.ReadAllBytes(filePath);
            if (!TryReadWaveLayout(waveData, out var layout))
            {
                return null;
            }

            var smplChunk = layout.Chunks.FirstOrDefault(c => c.Id == "smpl");
            if (smplChunk is null || smplChunk.Size < SmplChunkDataLength)
            {
                return null;
            }

            var loopCount = BitConverter.ToInt32(waveData, smplChunk.DataOffset + 28);
            if (loopCount <= 0 || smplChunk.Size < 60)
            {
                return null;
            }

            var startSample = BitConverter.ToUInt32(waveData, smplChunk.DataOffset + 44);
            var endSample = BitConverter.ToUInt32(waveData, smplChunk.DataOffset + 48);
            var startSeconds = SamplesToSeconds(startSample, layout.SampleRate);
            var endSeconds = SamplesToSeconds(endSample, layout.SampleRate);

            return NormalizeLoop(new AudioLoopPoints
            {
                IsEnabled = true,
                StartSeconds = startSeconds,
                EndSeconds = endSeconds
            }, layout.DurationSeconds);
        }

        private static AudioLoopPoints CreateFullFileLoop(WaveLayout layout)
            => new()
            {
                IsEnabled = true,
                StartSeconds = 0,
                EndSeconds = layout.DurationSeconds
            };

        private static AudioLoopPoints NormalizeLoop(AudioLoopPoints points, double durationSeconds)
        {
            if (!points.IsEnabled)
            {
                return points;
            }

            var start = Clamp(points.StartSeconds, 0, durationSeconds);
            var end = Clamp(points.EndSeconds, 0, durationSeconds);
            if (end <= start)
            {
                start = 0;
                end = durationSeconds;
            }

            return new AudioLoopPoints
            {
                IsEnabled = true,
                StartSeconds = start,
                EndSeconds = end
            };
        }

        private static byte[] CreateSmplChunk(AudioLoopPoints points, int sampleRate, long totalSamples)
        {
            var startSample = Clamp(SecondsToSamples(points.StartSeconds, sampleRate), 0, totalSamples);
            var endSample = Clamp(SecondsToSamples(points.EndSeconds, sampleRate), 0, totalSamples);
            if (endSample <= startSample)
            {
                startSample = 0;
                endSample = totalSamples;
            }

            var bytes = new byte[SmplChunkLength];
            Encoding.ASCII.GetBytes("smpl").CopyTo(bytes, 0);
            BitConverter.GetBytes(SmplChunkDataLength).CopyTo(bytes, 4);
            BitConverter.GetBytes(GetSamplePeriodNanoseconds(sampleRate)).CopyTo(bytes, 16);
            BitConverter.GetBytes(60).CopyTo(bytes, 20);
            BitConverter.GetBytes(1).CopyTo(bytes, 36);
            BitConverter.GetBytes((uint)startSample).CopyTo(bytes, 52);
            BitConverter.GetBytes((uint)endSample).CopyTo(bytes, 56);
            return bytes;
        }

        private static bool TryReadWaveLayout(byte[] waveData, out WaveLayout layout)
        {
            layout = new WaveLayout();
            if (waveData.Length < 12 ||
                Encoding.ASCII.GetString(waveData, 0, 4) != "RIFF" ||
                Encoding.ASCII.GetString(waveData, 8, 4) != "WAVE")
            {
                return false;
            }

            var chunks = ReadChunks(waveData);
            var fmt = chunks.FirstOrDefault(c => c.Id == "fmt ");
            var data = chunks.FirstOrDefault(c => c.Id == "data");
            if (fmt is null || data is null || fmt.Size < 16)
            {
                return false;
            }

            var channels = BitConverter.ToUInt16(waveData, fmt.DataOffset + 2);
            var sampleRate = BitConverter.ToInt32(waveData, fmt.DataOffset + 4);
            var blockAlign = BitConverter.ToUInt16(waveData, fmt.DataOffset + 12);
            if (channels == 0 || sampleRate <= 0 || blockAlign == 0)
            {
                return false;
            }

            using var stream = new MemoryStream(waveData, writable: false);
            using var reader = new WaveFileReader(stream);
            var durationSeconds = reader.TotalTime.TotalSeconds;

            layout.Chunks = chunks;
            layout.SampleRate = sampleRate;
            layout.DurationSeconds = durationSeconds;
            layout.TotalSamples = SecondsToSamples(durationSeconds, sampleRate);
            return true;
        }

        private static List<WaveChunk> ReadChunks(byte[] waveData)
        {
            var chunks = new List<WaveChunk>();
            var offset = 12;
            while (offset + 8 <= waveData.Length)
            {
                var id = Encoding.ASCII.GetString(waveData, offset, 4);
                var size = BitConverter.ToInt32(waveData, offset + 4);
                if (size < 0)
                {
                    break;
                }

                var dataOffset = offset + 8;
                if (dataOffset + size > waveData.Length)
                {
                    break;
                }

                chunks.Add(new WaveChunk(id, offset, dataOffset, size));
                offset = dataOffset + size + (size % 2);
            }

            return chunks;
        }

        private static byte[] RemoveChunk(byte[] waveData, string chunkId)
        {
            var result = waveData;
            foreach (var chunk in ReadChunks(result).Where(c => c.Id == chunkId).OrderByDescending(c => c.HeaderOffset))
            {
                var chunkLength = 8 + chunk.Size + (chunk.Size % 2);
                var next = new byte[result.Length - chunkLength];
                Buffer.BlockCopy(result, 0, next, 0, chunk.HeaderOffset);
                Buffer.BlockCopy(result, chunk.HeaderOffset + chunkLength, next, chunk.HeaderOffset, result.Length - chunk.HeaderOffset - chunkLength);
                result = next;
            }

            return result;
        }

        private static byte[] AppendBytes(byte[] left, byte[] right)
        {
            var result = new byte[left.Length + right.Length];
            left.CopyTo(result, 0);
            right.CopyTo(result, left.Length);
            return result;
        }

        private static void WriteRiffSize(byte[] waveData)
            => BitConverter.GetBytes(waveData.Length - 8).CopyTo(waveData, 4);

        private static long SecondsToSamples(double seconds, int sampleRate)
            => (long)Math.Round(seconds * sampleRate, MidpointRounding.AwayFromZero);

        private static double SamplesToSeconds(ulong samples, int sampleRate)
            => samples / (double)sampleRate;

        private static uint GetSamplePeriodNanoseconds(int sampleRate)
            => (uint)Math.Round(1_000_000_000d / sampleRate, MidpointRounding.AwayFromZero);

        private static double Clamp(double value, double min, double max)
            => Math.Min(Math.Max(value, min), max);

        private static long Clamp(long value, long min, long max)
            => Math.Min(Math.Max(value, min), max);

        private sealed class WaveLayout
        {
            public List<WaveChunk> Chunks { get; set; } = [];
            public int SampleRate { get; set; }
            public double DurationSeconds { get; set; }
            public long TotalSamples { get; set; }
        }

        private sealed record WaveChunk(string Id, int HeaderOffset, int DataOffset, int Size);
    }
}
