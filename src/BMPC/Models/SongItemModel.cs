namespace BMPC.Models
{
    public class SongItemModel
    {
        public Guid Guid { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string? Group { get; set; }
        public string Icon { get; set; } = string.Empty;
        public string BaseMusicPath { get; set; } = string.Empty;
        public string? TractorBeamPath { get; set; }
        public bool UseDefaultTractorBeamMusic { get; set; }
        public bool SyncTractorBeamMusic { get; set; }
        public List<string> SpeedGelSfxFullPaths { get; set; } = new List<string>();
        public List<string> BounceGelSfxFullPaths { get; set; } = new List<string>();
    }
}
