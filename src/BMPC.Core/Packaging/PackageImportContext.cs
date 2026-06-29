using BMPC.Core.Models;

namespace BMPC.Core.Packaging
{
    public sealed class PackageImportContext
    {
        public required PackageData Data { get; init; }
        public string? OldPackageId { get; init; }
        public required string PackageId { get; init; }
        public required string TempRoot { get; init; }
        public required string BeeResourcesPath { get; init; }
        public required string BeeSamplePath { get; init; }
        public required string GameMusicPath { get; init; }
        public string? OldBeePackPath { get; init; }
        public PackageData? OldEditData { get; init; }
    }
}
