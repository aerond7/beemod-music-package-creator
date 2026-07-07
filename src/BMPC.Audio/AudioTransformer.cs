using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using BMPC.Audio.Objects;

namespace BMPC.Audio
{
    public static class AudioTransformer
    {
        private const int DesiredSampleRate = 44100;
        public static AudioTransformResult ConvertForGameWav(
            string inputFilePath,
            string outputFilePath,
            AudioLoopPoints? loopPoints = null)
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

                // Source has no loop-end; it plays to the end of the audio data and loops back
                // to the cue point. So a loop end is realized by trimming the PCM here, before
                // ADPCM encoding, to the loop-end frame. A missing/zero end means "whole track".
                long? maxBytes = null;
                if (loopPoints is { IsEnabled: true, EndSeconds: > 0 })
                {
                    var endFrame = (long)Math.Round(loopPoints.EndSeconds * DesiredSampleRate, MidpointRounding.AwayFromZero);
                    maxBytes = endFrame * outFormat.BlockAlign;
                }

                using (var writer = new WaveFileWriter(tempPcmPath, outFormat))
                {
                    byte[] buffer = new byte[waveProvider.WaveFormat.AverageBytesPerSecond];
                    int bytesRead;
                    long written = 0;

                    while ((bytesRead = waveProvider.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        var toWrite = bytesRead;
                        if (maxBytes is long limit)
                        {
                            if (written >= limit)
                            {
                                break;
                            }

                            toWrite = (int)Math.Min(toWrite, limit - written);
                        }

                        writer.Write(buffer, 0, toWrite);
                        written += toWrite;
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
                // The audio is already trimmed to the loop end, so the cue only carries the
                // loop start. A null loop writes a full-track loop (cue at frame 0).
                AudioLoopTransformer.AddLoopPointsToWav(outputFilePath, outputFilePath, loopPoints);
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
