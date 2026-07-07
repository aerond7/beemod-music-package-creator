namespace BMPC.Audio.Objects
{
    public sealed class AudioLoopInfo
    {
        public int SampleRate { get; set; }
        public double DurationSeconds { get; set; }
        public long TotalSamples { get; set; }
        public AudioLoopPoints? ExistingLoopPoints { get; set; }
    }
}
