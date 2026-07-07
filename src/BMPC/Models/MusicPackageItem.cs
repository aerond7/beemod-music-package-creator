using BMPC.Core.Models;

namespace BMPC.Models
{
    public class MusicPackageItem
    {
        public required BmpcPackage Package { get; set; }

        public string Name => Package.Name;
        public string? Description => Package.Description;

        public string SongCount
        {
            get
            {
                var count = Package.SongCount ?? 1;
                return count == 1 ? "1 song" : $"{count} songs";
            }
        }

        public string Added => Package.DateAdded.ToString("G");

        // Sortable backing values (grid columns bind display strings).
        public int SongCountValue => Package.SongCount ?? 1;
        public DateTime AddedValue => Package.DateAdded;
    }
}
