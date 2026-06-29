namespace BMPC.Audio.Objects
{
    public class AudioTransformResult
    {
        public required bool IsSuccessful { get; set; }
        public Exception? Exception { get; set; }
    }
}
