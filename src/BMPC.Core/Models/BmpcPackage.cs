namespace BMPC.Core.Models
{
    public class BmpcPackage
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public int? SongCount { get; set; }
        public DateTime DateAdded { get; set; }
        public PackageData? EditData { get; set; }
    }
}
