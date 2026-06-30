using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using BMPC.Audio.Objects;
using System.Text;

namespace BMPC.Audio
{
    public static class AudioTransformer
    {
        private const int DesiredSampleRate = 44100;
        private const int DesiredBitRate = 16;

        public static AudioTransformResult ConvertForGameWav(string inputFilePath, string outputFilePath)
        {
            string? tempPcmPath = null;

            try
            {
                tempPcmPath = Path.Combine(Path.GetTempPath(), $"bmpc-{Guid.NewGuid():N}.wav");

                using var reader = new AudioFileReader(inputFilePath);

                // resample to 44100Hz if necessary
                var resampler = new WdlResamplingSampleProvider(reader, DesiredSampleRate);

                if (reader.WaveFormat.SampleRate == DesiredSampleRate && reader.WaveFormat.Channels == 2)
                {
                    resampler = null; // skip resampling if already desired sample rate and stereo
                }

                // convert to stereo if needed
                ISampleProvider stereo = resampler == null ? reader : resampler.ToStereo();

                var outFormat = new WaveFormat(DesiredSampleRate, 16, 2);
                var waveProvider = new SampleToWaveProvider16(stereo);
                using (var writer = new WaveFileWriter(tempPcmPath, outFormat))
                {
                    byte[] buffer = new byte[waveProvider.WaveFormat.AverageBytesPerSecond];
                    int bytesRead;

                    while ((bytesRead = waveProvider.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        writer.Write(buffer, 0, bytesRead);
                    }
                }

                using var pcmWaveStream = new WaveFileReader(tempPcmPath);
                var adpcmFormat = new AdpcmWaveFormat(DesiredSampleRate, 2);
                using var conversionStream = new WaveFormatConversionStream(adpcmFormat, pcmWaveStream);
                WaveFileWriter.CreateWaveFile(outputFilePath, conversionStream);
            }
            catch (Exception ex)
            {
                return new AudioTransformResult
                {
                    IsSuccessful = false,
                    Exception = ex
                };
            }
            finally
            {
                if (tempPcmPath != null && File.Exists(tempPcmPath))
                {
                    try
                    {
                        File.Delete(tempPcmPath);
                    }
                    catch
                    {
                    }
                }
            }

            try
            {
                AudioLoopTransformer.AddLoopPointsToWav(outputFilePath, outputFilePath, inputFilePath);
            }
            catch (Exception ex)
            {
                return new AudioTransformResult
                {
                    IsSuccessful = false,
                    Exception = ex
                };
            }

            return new AudioTransformResult
            {
                IsSuccessful = true
            };
        }

        public static AudioTransformResult ConvertForSampleMp3(string inputFilePath, string outputFilePath)
        {
            try
            {
                using (var reader = new AudioFileReader(inputFilePath))
                {
                    MediaFoundationEncoder.EncodeToMp3(reader, outputFilePath, 64000); // 64kbps is enough for a sample
                }
            }
            catch (Exception ex)
            {
                return new AudioTransformResult
                {
                    IsSuccessful = false,
                    Exception = ex
                };
            }

            return new AudioTransformResult
            {
                IsSuccessful = true
            };
        }
    }
}
