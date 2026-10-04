using NAudio.Wave;

namespace BMPC.Audio
{
    // Mixes multichannel audio (for example 5.1 or 7.1) down to stereo with the usual weights:
    // center, surround and height channels at -3 dB, LFE dropped. The gains are scaled so the
    // mix can't clip.
    public sealed class StereoDownmixSampleProvider : ISampleProvider
    {
        private const float Minus3Db = 0.70710677f;

        // WAVE_FORMAT_EXTENSIBLE speaker positions (dwChannelMask bits).
        private const int FrontLeft = 0x1;
        private const int FrontRight = 0x2;
        private const int FrontCenter = 0x4;
        private const int LowFrequency = 0x8;
        private const int BackLeft = 0x10;
        private const int BackRight = 0x20;
        private const int FrontLeftOfCenter = 0x40;
        private const int FrontRightOfCenter = 0x80;
        private const int BackCenter = 0x100;
        private const int SideLeft = 0x200;
        private const int SideRight = 0x400;
        private const int TopFrontLeft = 0x1000;
        private const int TopFrontRight = 0x4000;
        private const int TopBackLeft = 0x8000;
        private const int TopBackRight = 0x20000;
        private const int AllSpeakers = 0x3FFFF;

        private readonly ISampleProvider source;
        private readonly float[] leftGains;
        private readonly float[] rightGains;
        private float[] sourceBuffer = [];

        public StereoDownmixSampleProvider(ISampleProvider source, int channelMask = 0)
        {
            var channels = source.WaveFormat.Channels;
            var speakers = GetSpeakers(channels, channelMask);

            this.source = source;
            this.WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(source.WaveFormat.SampleRate, 2);
            this.leftGains = new float[channels];
            this.rightGains = new float[channels];

            for (var i = 0; i < channels; i++)
            {
                (this.leftGains[i], this.rightGains[i]) = GetGains(speakers[i]);
            }

            var loudest = Math.Max(this.leftGains.Sum(), this.rightGains.Sum());
            if (loudest > 1)
            {
                for (var i = 0; i < channels; i++)
                {
                    this.leftGains[i] /= loudest;
                    this.rightGains[i] /= loudest;
                }
            }
        }

        public WaveFormat WaveFormat { get; }

        public int Read(float[] buffer, int offset, int count)
        {
            var channels = this.leftGains.Length;
            var needed = count / 2 * channels;
            if (this.sourceBuffer.Length < needed)
            {
                this.sourceBuffer = new float[needed];
            }

            var frames = this.source.Read(this.sourceBuffer, 0, needed) / channels;
            for (var frame = 0; frame < frames; frame++)
            {
                float left = 0;
                float right = 0;
                var sourceIndex = frame * channels;

                for (var channel = 0; channel < channels; channel++)
                {
                    var sample = this.sourceBuffer[sourceIndex + channel];
                    left += sample * this.leftGains[channel];
                    right += sample * this.rightGains[channel];
                }

                buffer[offset++] = left;
                buffer[offset++] = right;
            }

            return frames * 2;
        }

        // Channels are stored in the order of their speaker bits. Channels beyond the mask have
        // no known position.
        private static int[] GetSpeakers(int channels, int channelMask)
        {
            var mask = channelMask != 0 ? channelMask : GetDefaultMask(channels);
            var speakers = new int[channels];
            var channel = 0;

            for (var bit = 1; bit <= AllSpeakers && channel < channels; bit <<= 1)
            {
                if ((mask & bit) != 0)
                {
                    speakers[channel++] = bit;
                }
            }

            return speakers;
        }

        private static int GetDefaultMask(int channels) => channels switch
        {
            1 => FrontCenter,
            2 => FrontLeft | FrontRight,
            3 => FrontLeft | FrontRight | FrontCenter,
            4 => FrontLeft | FrontRight | BackLeft | BackRight,
            5 => FrontLeft | FrontRight | FrontCenter | BackLeft | BackRight,
            6 => FrontLeft | FrontRight | FrontCenter | LowFrequency | BackLeft | BackRight,
            7 => FrontLeft | FrontRight | FrontCenter | LowFrequency | BackLeft | BackRight | BackCenter,
            8 => FrontLeft | FrontRight | FrontCenter | LowFrequency | BackLeft | BackRight | SideLeft | SideRight,
            _ => 0
        };

        private static (float Left, float Right) GetGains(int speaker) => speaker switch
        {
            FrontLeft or FrontLeftOfCenter => (1, 0),
            FrontRight or FrontRightOfCenter => (0, 1),
            LowFrequency => (0, 0),
            BackLeft or SideLeft or TopFrontLeft or TopBackLeft => (Minus3Db, 0),
            BackRight or SideRight or TopFrontRight or TopBackRight => (0, Minus3Db),
            _ => (Minus3Db, Minus3Db) // center positions and channels with no known position
        };
    }
}
