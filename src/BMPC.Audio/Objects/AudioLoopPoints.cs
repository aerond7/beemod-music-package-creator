namespace BMPC.Audio.Objects
{
    public sealed class AudioLoopPoints
    {
        public bool IsEnabled { get; set; } = true;
        public double StartSeconds { get; set; }
        public double EndSeconds { get; set; }

        public AudioLoopPoints Clone()
            => new()
            {
                IsEnabled = this.IsEnabled,
                StartSeconds = this.StartSeconds,
                EndSeconds = this.EndSeconds
            };
    }
}
