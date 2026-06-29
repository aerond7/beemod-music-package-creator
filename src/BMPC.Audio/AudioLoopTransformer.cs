using System.Text;

namespace BMPC.Audio
{
    public static class AudioLoopTransformer
    {
        private static byte[] ReadWaveData(string filePath)
        {
            return File.ReadAllBytes(filePath);
        }

        private static byte[]? GetSmplChunkData(string filePath)
        {
            byte[]? chunkData = null;

            var waveData = ReadWaveData(filePath);

            // find index (byte offset) of smpl chunk if it exists
            int smplIndex = -1;
            for (int i = waveData.Length - 4 - 1; i >= 0; i--) // start search from end of file going backward
            {
                if (waveData[i] == 's' && waveData[i + 1] == 'm' && waveData[i + 2] == 'p' && waveData[i + 3] == 'l')
                {
                    smplIndex = i;
                    break;
                }
            }

            // if the smpl chunk already exists, remove it
            if (smplIndex != -1)
            {
                int smplChunkSize = BitConverter.ToInt32(waveData, smplIndex + 4) + 8; // smpl chunk size is specified 4 bytes after start of smpl chunk id
                chunkData = new byte[smplChunkSize];
                Array.Copy(waveData, smplIndex, chunkData, 0, smplChunkSize);
            }

            return chunkData;
        }

        public static void AddLoopPointsToWav(string inputPath, string outputPath, string? existingFilePath = null)
        {
            var waveData = ReadWaveData(inputPath);

            // define our loop start and end
            double start = 0.0;
            double end = waveData.Length - 44.0;

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

            // append smpl chunk to wave file
            byte[] newWaveDataWithSmpl = new byte[waveData.Length + smplChunk.Length];
            waveData.CopyTo(newWaveDataWithSmpl, 0);
            smplChunk.CopyTo(newWaveDataWithSmpl, waveData.Length);
            waveData = newWaveDataWithSmpl;

            // change wave file main header data to increase the file size to include smpl chunk (loop points)
            int fileSizeIndex = -1;
            for (int i = 0; i < waveData.Length - 4; i++)
            {
                if (waveData[i] == 'R' && waveData[i + 1] == 'I' && waveData[i + 2] == 'F' && waveData[i + 3] == 'F')
                {
                    fileSizeIndex = i + 4; // file size is 4 bytes after start of RIFF chunk id
                    break;
                }
            }

            if (fileSizeIndex == -1)
                return;

            int fileSize = waveData.Length - 8; // get final length of wave file, minus 8 bytes to not include the RIFF chunk header itself
            BitConverter.GetBytes(fileSize).CopyTo(waveData, fileSizeIndex); // write new file length

            // write new wave file
            using (var outputStream = File.Create(outputPath))
            {
                outputStream.Write(waveData, 0, waveData.Length);
            }
        }
    }
}
