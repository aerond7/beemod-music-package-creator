using NAudio.Dmo;
using NAudio.Wave;

namespace BMPC.Audio
{
    // Same as NAudio's AudioFileReader, but also opens WAVE_FORMAT_EXTENSIBLE WAV files, which
    // AudioFileReader can't read ("NoDriver calling acmFormatSuggest"). Most multichannel WAVs
    // and many 24-bit or float WAVs use that format, even though the audio inside is plain PCM
    // or float.
    public sealed class AudioSourceReader : WaveStream, ISampleProvider
    {
        private readonly WaveStream stream;
        private readonly ISampleProvider samples;

        public AudioSourceReader(string filePath)
        {
            var wavReader = TryOpenExtensibleWav(filePath, out var standardFormat, out var channelMask);
            if (wavReader is not null)
            {
                this.stream = wavReader;
                this.samples = new ReformattedWaveStream(wavReader, standardFormat!).ToSampleProvider();
                this.ChannelMask = channelMask;
            }
            else
            {
                var reader = new AudioFileReader(filePath);
                this.stream = reader;
                this.samples = reader;
            }
        }

        // Speaker position of each channel (WAVE_FORMAT_EXTENSIBLE dwChannelMask), or 0 if the
        // file doesn't say.
        public int ChannelMask { get; }

        public override WaveFormat WaveFormat => this.samples.WaveFormat;

        // Length and Position are in bytes of the float output, like AudioFileReader.
        public override long Length => this.stream.Length / this.stream.WaveFormat.BlockAlign * this.WaveFormat.BlockAlign;

        public override long Position
        {
            get => this.stream.Position / this.stream.WaveFormat.BlockAlign * this.WaveFormat.BlockAlign;
            set => this.stream.Position = value / this.WaveFormat.BlockAlign * this.stream.WaveFormat.BlockAlign;
        }

        public int Read(float[] buffer, int offset, int count)
            => this.samples.Read(buffer, offset, count);

        public override int Read(byte[] buffer, int offset, int count)
        {
            var waveBuffer = new WaveBuffer(buffer);
            return this.Read(waveBuffer.FloatBuffer, offset / 4, count / 4) * 4;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.stream.Dispose();
            }

            base.Dispose(disposing);
        }

        private static WaveFileReader? TryOpenExtensibleWav(string filePath, out WaveFormat? standardFormat, out int channelMask)
        {
            standardFormat = null;
            channelMask = 0;

            if (!Path.GetExtension(filePath).Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            WaveFileReader? reader = null;
            try
            {
                reader = new WaveFileReader(filePath);
                var format = reader.WaveFormat;

                // Extensible extra data: valid bits (2), channel mask (4), sub format GUID (16).
                if (format.Encoding == WaveFormatEncoding.Extensible
                    && format is WaveFormatExtraData { ExtraData.Length: >= 22 } extensible)
                {
                    var subFormat = new Guid(extensible.ExtraData.AsSpan(6, 16));
                    if (subFormat == AudioMediaSubtypes.MEDIASUBTYPE_PCM)
                    {
                        standardFormat = new WaveFormat(format.SampleRate, format.BitsPerSample, format.Channels);
                    }
                    else if (subFormat == AudioMediaSubtypes.MEDIASUBTYPE_IEEE_FLOAT)
                    {
                        standardFormat = WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRate, format.Channels);
                    }

                    if (standardFormat is not null)
                    {
                        channelMask = BitConverter.ToInt32(extensible.ExtraData, 2);
                        return reader;
                    }
                }
            }
            catch
            {
                // Not a WAV NAudio can parse. AudioFileReader reports the error.
            }

            reader?.Dispose();
            return null;
        }

        // Passes the audio data through unchanged under a different (equivalent) format header.
        private sealed class ReformattedWaveStream(WaveStream source, WaveFormat format) : WaveStream
        {
            public override WaveFormat WaveFormat => format;

            public override long Length => source.Length;

            public override long Position
            {
                get => source.Position;
                set => source.Position = value;
            }

            public override int Read(byte[] buffer, int offset, int count)
                => source.Read(buffer, offset, count);
        }
    }
}
