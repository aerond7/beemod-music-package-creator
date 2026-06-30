using System.Text;

namespace BMPC.Audio
{
    public static class AudioLoopTransformer
    {
        private const int BufferSize = 81920;
        private static readonly byte[] RiffMarker = Encoding.ASCII.GetBytes("RIFF");
        private static readonly byte[] SmplMarker = Encoding.ASCII.GetBytes("smpl");

        private static byte[]? GetSmplChunkData(string filePath)
        {
            var smplIndex = FindLastMarker(filePath, SmplMarker);
            if (smplIndex == -1)
            {
                return null;
            }

            using var stream = File.OpenRead(filePath);
            stream.Position = smplIndex + 4;

            Span<byte> sizeBytes = stackalloc byte[4];
            stream.ReadExactly(sizeBytes);

            int smplChunkSize = BitConverter.ToInt32(sizeBytes) + 8; // smpl chunk size is specified 4 bytes after start of smpl chunk id
            byte[] chunkData = new byte[smplChunkSize];
            stream.Position = smplIndex;
            stream.ReadExactly(chunkData);
            return chunkData;
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

        public static void AddLoopPointsToWav(string inputPath, string outputPath, string? existingFilePath = null)
        {
            var riffIndex = FindFirstMarker(inputPath, RiffMarker);
            if (riffIndex == -1)
            {
                return;
            }

            var inputLength = new FileInfo(inputPath).Length;

            // define our loop start and end
            double start = 0.0;
            double end = inputLength - 44.0;

            // try to read smpl chunk from existing file
            byte[]? smplChunk = existingFilePath is not null ? GetSmplChunkData(existingFilePath) : null;

            // if there isn't any existing smpl chunk then create a brand new one and set loop at start and end
            if (smplChunk is null)
            {
                smplChunk = new byte[68]; // the default smpl chunk written here is 60 bytes long
                Array.Fill(smplChunk, (byte)0); // fill array with zeros
                Encoding.ASCII.GetBytes("smpl").CopyTo(smplChunk, 0);
                BitConverter.GetBytes(60).CopyTo(smplChunk, 4); // default smpl chunk length is 60 bytes
                BitConverter.GetBytes(60).CopyTo(smplChunk, 20); // middle C is MIDI note 60, therefore make MIDI unity note 60
                BitConverter.GetBytes(1).CopyTo(smplChunk, 36); // write at byte offset 36 that there is one loop cue info in the file
                BitConverter.GetBytes((int)start).CopyTo(smplChunk, 52); // write loop start point at byte offset 52
                BitConverter.GetBytes((int)end).CopyTo(smplChunk, 56); // write loop end point at byte offset 56
            }

            var isInPlace = string.Equals(
                Path.GetFullPath(inputPath),
                Path.GetFullPath(outputPath),
                StringComparison.OrdinalIgnoreCase);

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

        private static void WriteRiffFileSize(Stream stream, long fileSizeIndex, long outputLength)
        {
            stream.Position = fileSizeIndex;
            int fileSize = (int)(outputLength - 8); // get final length of wave file, minus 8 bytes to not include the RIFF chunk header itself
            Span<byte> fileSizeBytes = stackalloc byte[4];
            BitConverter.GetBytes(fileSize).CopyTo(fileSizeBytes); // write new file length
            stream.Write(fileSizeBytes);
        }
    }
}
