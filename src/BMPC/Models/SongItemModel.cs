using BMPC.Audio.Objects;
using BMPC.Core.Packaging;

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
        public AudioLoopPoints? BaseLoopPoints { get; set; }
        public string? TractorBeamPath { get; set; }
        public AudioLoopPoints? TractorBeamLoopPoints { get; set; }
        public bool UseDefaultTractorBeamMusic { get; set; }
        public bool SyncTractorBeamMusic { get; set; }
        public List<string> SpeedGelSfxFullPaths { get; set; } = new List<string>();
        public List<string> BounceGelSfxFullPaths { get; set; } = new List<string>();

        /// <summary>Assets already written into the package being edited; used instead of the files on disk.</summary>
        public PackagedSongAssets? PackagedAssets { get; set; }

        /// <summary>Icon extracted from the package being edited.</summary>
        public string? PackagedIconPath { get; set; }

        // Set when new files were selected for the asset, so it is read from disk instead of the package.
        public bool BaseAudioReplaced { get; set; }
        public bool TractorBeamAudioReplaced { get; set; }
        public bool SpeedGelSfxReplaced { get; set; }
        public bool BounceGelSfxReplaced { get; set; }
        public bool IconReplaced { get; set; }

        /// <summary>Icon to display: the packaged one for songs from the package being edited, unless replaced.</summary>
        public string? DisplayIcon => this.IconReplaced || this.PackagedAssets is null ? this.Icon : this.PackagedIconPath;
    }
}
