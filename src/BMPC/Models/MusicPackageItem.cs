using BMPC.Core.Models;
using System;
using System.Collections.Generic;

namespace BMPC.Models
{
    public class MusicPackageItem
    {
        public required BmpcPackage Package { get; set; }

        public string Name => Package.Name;
        public string? Description => Package.Description;

        public bool HasDescription => !string.IsNullOrWhiteSpace(Package.Description);

        public string SongCount
        {
            get
            {
                var count = Package.SongCount ?? 1;
                return count == 1 ? "1 song" : $"{count} songs";
            }
        }

        public string Added => Package.DateAdded.ToString("G");

        // Detail-pane data.
        public string? Group => Package.EditData?.DefaultGroup;
        public bool HasGroup => !string.IsNullOrWhiteSpace(Package.EditData?.DefaultGroup);
        public bool HasSongDetail => Package.EditData is not null && Package.EditData.Songs.Count > 0;
        public IReadOnlyList<PackageSong> Songs => Package.EditData?.Songs ?? new List<PackageSong>();

        // Sortable backing values.
        public int SongCountValue => Package.SongCount ?? 1;
        public DateTime AddedValue => Package.DateAdded;
    }
}
