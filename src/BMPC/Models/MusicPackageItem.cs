using BMPC.Core.Models;

namespace BMPC.Models
{
    public class MusicPackageItem
    {
        public required BmpcPackage Package { get; set; }

        public string Name => Package.Name;
        public string SongCount => Package.SongCount?.ToString() + " song(s)" ?? "1 song";
        public string Added => Package.DateAdded.ToString("G");
    }
}
