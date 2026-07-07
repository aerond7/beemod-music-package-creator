using BMPC.Audio.Objects;

namespace BMPC.Core.Models
{
    public class PackageData
    {
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string DefaultGroup { get; set; } = "";
        public List<PackageSong> Songs { get; set; } = new List<PackageSong>();
    }

    public class PackageSong
    {
        /// <summary>Stable identifier used to match this song across edits.</summary>
        public Guid? SongId { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public string Authors { get; set; } = "";
        public string? Group { get; set; }
        public string IconFullPath { get; set; } = "";
        public string BaseFullPath { get; set; } = "";
        public AudioLoopPoints? BaseLoopPoints { get; set; }
        public string? TractorBeamFullPath { get; set; }
        public AudioLoopPoints? TractorBeamLoopPoints { get; set; }
        public bool UseDefaultTractorBeamMusic { get; set; }
        public bool SyncTractorBeamMusic { get; set; }
        public List<string> SpeedGelSfxFullPaths { get; set; } = new List<string>();
        public List<string> BounceGelSfxFullPaths { get; set; } = new List<string>();
    }
}
