using BMPC.Audio.Objects;
using NAudio.Wave;
using System.Text;

namespace BMPC.Audio
{
    public static class AudioLoopTransformer
    {
        private const int BufferSize = 81920;
        private const int SmplChunkLength = 68;
        private const int SmplChunkDataLength = 60;
        private static readonly byte[] RiffMarker = Encoding.ASCII.GetBytes("RIFF");
        private static readonly byte[] SmplMarker = Encoding.ASCII.GetBytes("smpl");

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
            if (!TryReadWaveLayout(inputPath, out var layout))
            {
                return;
            }

            var riffIndex = FindFirstMarker(inputPath, RiffMarker);
            if (riffIndex == -1)
            {
                return;
            }

            AudioLoopPoints? points = loopPoints?.Clone()
                ?? (existingFilePath is not null ? TryReadSmplLoopPoints(existingFilePath) : null);

            points ??= CreateFullFileLoop(layout);
            points = NormalizeLoop(points, layout.DurationSeconds);

            var isInPlace = string.Equals(
                Path.GetFullPath(inputPath),
                Path.GetFullPath(outputPath),
                StringComparison.OrdinalIgnoreCase);

            if (!points.IsEnabled)
            {
                if (!isInPlace)
                {
                    using var inputStream = File.OpenRead(inputPath);
                    using var outputStream = File.Create(outputPath);
                    inputStream.CopyTo(outputStream);
                }

                return;
            }

            var smplChunk = CreateSmplChunk(points, layout.SampleRate, layout.TotalSamples);

            if (isInPlace)
            {
                using var outputStream = new FileStream(outputPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                outputStream.Position = outputStream.Length;
                outputStream.Write(smplChunk, 0, smplChunk.Length);
                WriteRiffFileSize(outputStream, riffIndex + 4, outputStream.Length);
                return;
            }

            using (var inputStream = File.OpenRead(inputPath))
            using (var outputStream = File.Create(outputPath))
            {
                inputStream.CopyTo(outputStream);
                outputStream.Write(smplChunk, 0, smplChunk.Length);
                WriteRiffFileSize(outputStream, riffIndex + 4, outputStream.Length);
            }
        }

        private static AudioLoopPoints? TryReadSmplLoopPoints(string filePath)
        {
            if (!Path.GetExtension(filePath).Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!TryReadWaveLayout(filePath, out var layout))
            {
                return null;
            }

            var smplIndex = FindLastMarker(filePath, SmplMarker);
            if (smplIndex == -1)
            {
                return null;
            }

            using var stream = File.OpenRead(filePath);
            if (stream.Length < smplIndex + 68)
            {
                return null;
            }

            stream.Position = smplIndex + 4;
            Span<byte> bytes = stackalloc byte[4];
            stream.ReadExactly(bytes);
            var smplChunkSize = BitConverter.ToInt32(bytes);
            if (smplChunkSize < SmplChunkDataLength)
            {
                return null;
            }

            stream.Position = smplIndex + 36;
            stream.ReadExactly(bytes);
            var loopCount = BitConverter.ToInt32(bytes);
            if (loopCount <= 0)
            {
                return null;
            }

            stream.Position = smplIndex + 52;
            stream.ReadExactly(bytes);
            var startSample = BitConverter.ToUInt32(bytes);

            stream.ReadExactly(bytes);
            var endSample = BitConverter.ToUInt32(bytes);

            return NormalizeLoop(new AudioLoopPoints
            {
                IsEnabled = true,
                StartSeconds = SamplesToSeconds(startSample, layout.SampleRate),
                EndSeconds = SamplesToSeconds(endSample, layout.SampleRate)
            }, layout.DurationSeconds);
        }

        private static bool TryReadWaveLayout(string filePath, out WaveLayout layout)
        {
            layout = new WaveLayout();

            try
            {
                using var stream = File.OpenRead(filePath);
                Span<byte> header = stackalloc byte[12];
                if (stream.Length < header.Length)
                {
                    return false;
                }

                stream.ReadExactly(header);
                if (!header[..4].SequenceEqual(RiffMarker) || Encoding.ASCII.GetString(header[8..12]) != "WAVE")
                {
                    return false;
                }

                using var waveReader = new WaveFileReader(filePath);
                var durationSeconds = waveReader.TotalTime.TotalSeconds;
                var sampleRate = waveReader.WaveFormat.SampleRate;

                layout = new WaveLayout
                {
                    SampleRate = sampleRate,
                    DurationSeconds = durationSeconds,
                    TotalSamples = SecondsToSamples(durationSeconds, sampleRate)
                };
                return true;
            }
            catch
            {
                return false;
            }
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

        private static long FindFirstMarker(string filePath, byte[] marker)
        {
            using var stream = File.OpenRead(filePath);
            var matched = 0;

            for (var position = 0L; position < stream.Length; position++)
            {
                var value = stream.ReadByte();
                if (value == marker[matched])
                {
                    matched++;
                    if (matched == marker.Length)
                    {
                        return position - marker.Length + 1;
                    }

                    continue;
                }

                matched = value == marker[0] ? 1 : 0;
            }

            return -1;
        }

        private static long FindLastMarker(string filePath, byte[] marker)
        {
            using var stream = File.OpenRead(filePath);
            if (stream.Length < marker.Length)
            {
                return -1;
            }

            var buffer = new byte[BufferSize + marker.Length - 1];
            var carry = Array.Empty<byte>();
            var blockEnd = stream.Length;

            while (blockEnd > 0)
            {
                var readSize = (int)Math.Min(BufferSize, blockEnd);
                blockEnd -= readSize;

                stream.Position = blockEnd;
                stream.ReadExactly(buffer.AsSpan(0, readSize));
                carry.CopyTo(buffer.AsSpan(readSize));

                var searchLength = readSize + carry.Length;
                for (var i = searchLength - marker.Length; i >= 0; i--)
                {
                    if (IsMatch(buffer, i, marker))
                    {
                        return blockEnd + i;
                    }
                }

                var carryLength = Math.Min(marker.Length - 1, readSize);
                carry = buffer[..carryLength].ToArray();
            }

            return -1;
        }

        private static bool IsMatch(byte[] buffer, int offset, byte[] marker)
        {
            for (var i = 0; i < marker.Length; i++)
            {
                if (buffer[offset + i] != marker[i])
                {
                    return false;
                }
            }

            return true;
        }

        private static void WriteRiffFileSize(Stream stream, long fileSizeIndex, long outputLength)
        {
            stream.Position = fileSizeIndex;
            int fileSize = (int)(outputLength - 8);
            Span<byte> fileSizeBytes = stackalloc byte[4];
            BitConverter.GetBytes(fileSize).CopyTo(fileSizeBytes);
            stream.Write(fileSizeBytes);
        }

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
            public int SampleRate { get; init; }
            public double DurationSeconds { get; init; }
            public long TotalSamples { get; init; }
        }
    }
}
