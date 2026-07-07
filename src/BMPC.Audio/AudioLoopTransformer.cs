using BMPC.Audio.Objects;
using NAudio.Wave;
using System.Text;

namespace BMPC.Audio
{
    // Source engine (GoldSrc/Source, incl. Portal 2 / BEEmod) loops WAV audio through the
    // RIFF "cue " chunk, NOT the "smpl" chunk. It reads a single loop-start sample offset,
    // plays to the end of the audio data, then jumps back to that cue point. There is no
    // loop-end concept in the engine: a loop end is realized by physically trimming the
    // audio at the end point (done upstream in AudioTransformer before encoding).
    //
    // For Microsoft ADPCM output the engine mis-scales the cue sample offset and expects it
    // HALVED relative to the true PCM sample-frame index (documented Source quirk; a loop
    // that starts at frame 0 is unaffected since 0/2 == 0).
    public static class AudioLoopTransformer
    {
        private const int BufferSize = 81920;
        private const int CueChunkLength = 36;   // 8 header + 4 count + 24 (one cue point)
        private const int CueChunkDataLength = 28; // 4 count + 24 cue point
        private const int SmplChunkDataLength = 60;
        private static readonly byte[] RiffMarker = Encoding.ASCII.GetBytes("RIFF");
        private const string SmplId = "smpl";
        private const string CueId = "cue ";

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
                ExistingLoopPoints = TryReadSourceLoopPoints(filePath)
            };
        }

        // Writes a single "cue " loop-start chunk for the Source engine. Any pre-existing
        // "cue "/"smpl" chunks are stripped so re-processing never accumulates duplicates.
        // Every game asset loops; a null loop means "loop the whole track" (cue at frame 0).
        public static void AddLoopPointsToWav(
            string inputPath,
            string outputPath,
            AudioLoopPoints? loopPoints = null)
        {
            if (!TryReadWaveLayout(inputPath, out var layout))
            {
                return;
            }

            if (!TryReadChunks(inputPath, out var chunks))
            {
                return;
            }

            var startSeconds = loopPoints is { IsEnabled: true } ? loopPoints.StartSeconds : 0;
            var startFrame = Clamp(
                SecondsToSamples(startSeconds, layout.SampleRate),
                0,
                Math.Max(0, layout.TotalSamples - 1));

            var cueOffset = layout.IsCompressed ? startFrame / 2 : startFrame;
            var cueChunk = CreateCueChunk((uint)cueOffset);

            RebuildFile(inputPath, outputPath, chunks, cueChunk);
        }

        private static AudioLoopPoints? TryReadSourceLoopPoints(string filePath)
        {
            if (!Path.GetExtension(filePath).Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (!TryReadWaveLayout(filePath, out var layout))
            {
                return null;
            }

            if (!TryReadChunks(filePath, out var chunks))
            {
                return null;
            }

            // A "smpl" chunk carries both start and end, so prefer it when present. Otherwise
            // fall back to a "cue " chunk, which only defines the loop start.
            return TryReadSmplLoop(filePath, chunks, layout)
                ?? TryReadCueLoop(filePath, chunks, layout);
        }

        private static AudioLoopPoints? TryReadSmplLoop(string filePath, List<RiffChunk> chunks, WaveLayout layout)
        {
            RiffChunk? smpl = null;
            foreach (var chunk in chunks)
            {
                if (chunk.Id == SmplId)
                {
                    smpl = chunk;
                }
            }

            if (smpl is not { } smplChunk || smplChunk.DataSize < SmplChunkDataLength)
            {
                return null;
            }

            using var stream = File.OpenRead(filePath);
            Span<byte> bytes = stackalloc byte[4];

            stream.Position = smplChunk.DataOffset + 28; // dwNumSampleLoops
            stream.ReadExactly(bytes);
            if (BitConverter.ToInt32(bytes) <= 0)
            {
                return null;
            }

            stream.Position = smplChunk.DataOffset + 44; // loop dwStart
            stream.ReadExactly(bytes);
            var startSample = BitConverter.ToUInt32(bytes);

            stream.ReadExactly(bytes); // loop dwEnd
            var endSample = BitConverter.ToUInt32(bytes);

            return NormalizeLoop(new AudioLoopPoints
            {
                IsEnabled = true,
                StartSeconds = SamplesToSeconds(startSample, layout.SampleRate),
                EndSeconds = SamplesToSeconds(endSample, layout.SampleRate)
            }, layout.DurationSeconds);
        }

        private static AudioLoopPoints? TryReadCueLoop(string filePath, List<RiffChunk> chunks, WaveLayout layout)
        {
            RiffChunk? cue = null;
            foreach (var chunk in chunks)
            {
                if (chunk.Id == CueId)
                {
                    cue = chunk;
                }
            }

            if (cue is not { } cueChunk || cueChunk.DataSize < CueChunkDataLength)
            {
                return null;
            }

            using var stream = File.OpenRead(filePath);
            Span<byte> bytes = stackalloc byte[4];

            stream.Position = cueChunk.DataOffset; // dwCuePoints
            stream.ReadExactly(bytes);
            if (BitConverter.ToInt32(bytes) <= 0)
            {
                return null;
            }

            stream.Position = cueChunk.DataOffset + 4 + 20; // first cue point dwSampleOffset
            stream.ReadExactly(bytes);
            var cueOffset = BitConverter.ToUInt32(bytes);

            // Reverse the ADPCM half-scaling to recover the true sample-frame index.
            var startFrame = layout.IsCompressed ? cueOffset * 2 : cueOffset;

            return NormalizeLoop(new AudioLoopPoints
            {
                IsEnabled = true,
                StartSeconds = SamplesToSeconds(startFrame, layout.SampleRate),
                EndSeconds = layout.DurationSeconds
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
                    TotalSamples = SecondsToSamples(durationSeconds, sampleRate),
                    IsCompressed = waveReader.WaveFormat.Encoding != WaveFormatEncoding.Pcm
                };
                return true;
            }
            catch
            {
                return false;
            }
        }

        // Walks the RIFF chunk tree instead of scanning raw bytes, so audio data that happens
        // to contain an ASCII chunk id ("cue ", "smpl", "RIFF") is never mistaken for a header.
        private static bool TryReadChunks(string filePath, out List<RiffChunk> chunks)
        {
            chunks = new List<RiffChunk>();

            try
            {
                using var stream = File.OpenRead(filePath);
                if (stream.Length < 12)
                {
                    return false;
                }

                Span<byte> header = stackalloc byte[12];
                stream.ReadExactly(header);
                if (!header[..4].SequenceEqual(RiffMarker) || Encoding.ASCII.GetString(header[8..12]) != "WAVE")
                {
                    return false;
                }

                long position = 12;
                Span<byte> chunkHeader = stackalloc byte[8];
                while (position + 8 <= stream.Length)
                {
                    stream.Position = position;
                    stream.ReadExactly(chunkHeader);
                    var id = Encoding.ASCII.GetString(chunkHeader[..4]);
                    var dataSize = BitConverter.ToUInt32(chunkHeader[4..8]);
                    var dataOffset = position + 8;
                    if (dataOffset + dataSize > stream.Length)
                    {
                        break; // truncated/invalid chunk, stop walking
                    }

                    chunks.Add(new RiffChunk(id, position, dataOffset, dataSize));

                    // Chunks are word aligned: odd-sized data is followed by a pad byte.
                    position = dataOffset + dataSize + (dataSize & 1);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        // Copies every chunk except "cue "/"smpl" verbatim (re-normalizing word alignment),
        // then appends the freshly built cue chunk (if any) and patches the RIFF size.
        private static void RebuildFile(string inputPath, string outputPath, List<RiffChunk> chunks, byte[]? cueChunk)
        {
            var tempPath = outputPath + ".tmp";

            using (var input = File.OpenRead(inputPath))
            using (var output = File.Create(tempPath))
            {
                input.Position = 0;
                CopyBytes(input, output, 12); // RIFF/size/WAVE header; size patched below.

                foreach (var chunk in chunks)
                {
                    if (chunk.Id == CueId || chunk.Id == SmplId)
                    {
                        continue;
                    }

                    input.Position = chunk.HeaderOffset;
                    CopyBytes(input, output, 8 + chunk.DataSize);
                    if ((chunk.DataSize & 1) == 1)
                    {
                        output.WriteByte(0); // restore word-alignment pad byte
                    }
                }

                if (cueChunk is not null)
                {
                    if ((output.Length & 1) == 1)
                    {
                        output.WriteByte(0); // align the cue chunk to an even offset
                    }

                    output.Write(cueChunk, 0, cueChunk.Length);
                }

                WriteRiffFileSize(output, 4, output.Length);
            }

            File.Move(tempPath, outputPath, overwrite: true);
        }

        private static void CopyBytes(Stream source, Stream destination, long count)
        {
            var buffer = new byte[BufferSize];
            while (count > 0)
            {
                var toRead = (int)Math.Min(buffer.Length, count);
                source.ReadExactly(buffer, 0, toRead);
                destination.Write(buffer, 0, toRead);
                count -= toRead;
            }
        }

        private static AudioLoopPoints NormalizeLoop(AudioLoopPoints points, double durationSeconds)
        {
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

        private static byte[] CreateCueChunk(uint sampleOffset)
        {
            var bytes = new byte[CueChunkLength];
            Encoding.ASCII.GetBytes(CueId).CopyTo(bytes, 0);
            BitConverter.GetBytes(CueChunkDataLength).CopyTo(bytes, 4);
            BitConverter.GetBytes(1).CopyTo(bytes, 8);   // dwCuePoints
            // Cue point:
            BitConverter.GetBytes(1).CopyTo(bytes, 12);  // dwName (unique id)
            BitConverter.GetBytes(sampleOffset).CopyTo(bytes, 16); // dwPosition
            Encoding.ASCII.GetBytes("data").CopyTo(bytes, 20);     // fccChunk
            BitConverter.GetBytes(0).CopyTo(bytes, 24);  // dwChunkStart
            BitConverter.GetBytes(0).CopyTo(bytes, 28);  // dwBlockStart
            BitConverter.GetBytes(sampleOffset).CopyTo(bytes, 32); // dwSampleOffset (loop point)
            return bytes;
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

        private static double Clamp(double value, double min, double max)
            => Math.Min(Math.Max(value, min), max);

        private static long Clamp(long value, long min, long max)
            => Math.Min(Math.Max(value, min), max);

        private readonly record struct RiffChunk(string Id, long HeaderOffset, long DataOffset, long DataSize);

        private sealed class WaveLayout
        {
            public int SampleRate { get; init; }
            public double DurationSeconds { get; init; }
            public long TotalSamples { get; init; }
            public bool IsCompressed { get; init; }
        }
    }
}
