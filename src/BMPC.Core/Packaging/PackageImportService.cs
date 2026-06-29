using BMPC.Core.Beemod;
using BMPC.Core.Models;

namespace BMPC.Core.Packaging
{
    public sealed class PackageImportService
    {
        private readonly string tempPath = Path.GetTempPath();
        private readonly Guid importerGuid = Guid.NewGuid();
        private readonly BmpcMetadataStore metadataStore;
        private readonly BeePackageWriter beePackageWriter;
        private readonly PackageCacheService cacheService;
        private readonly AudioAssetWriter audioAssetWriter;
        private readonly IconAssetWriter iconAssetWriter;

        public PackageImportService(
            string? tempPath = null,
            BmpcMetadataStore? metadataStore = null,
            BeePackageWriter? beePackageWriter = null,
            PackageCacheService? cacheService = null,
            AudioAssetWriter? audioAssetWriter = null,
            IconAssetWriter? iconAssetWriter = null)
        {
            if (tempPath != null && Directory.Exists(tempPath))
            {
                this.tempPath = tempPath;
            }

            this.metadataStore = metadataStore ?? new BmpcMetadataStore();
            this.beePackageWriter = beePackageWriter ?? new BeePackageWriter(new BMPC.Core.Services.PackageArchiveWriter(), this.metadataStore);
            this.cacheService = cacheService ?? new PackageCacheService();
            this.audioAssetWriter = audioAssetWriter ?? new AudioAssetWriter();
            this.iconAssetWriter = iconAssetWriter ?? new IconAssetWriter();
        }

        public async Task Import(PackageData data, string? oldPackageId = null, IProgress<string>? progress = null)
        {
            progress ??= new Progress<string>();
            var context = await this.CreateContext(data, oldPackageId);

            try
            {
                var builder = new BeemodMetaBuilder()
                    .WithName(data.Name)
                    .WithDescription(data.Description);

                foreach (var song in data.Songs)
                {
                    await this.ImportSong(context, song, builder, progress);
                }

                progress.Report("Preparing BEE package...");

                using var beemodPackMeta = builder.Build();
                await this.beePackageWriter.WriteInfoFile(context.TempRoot, beemodPackMeta);

                progress.Report("Packaging...");

                await this.beePackageWriter.WritePackage(context, new BmpcPackage
                {
                    Id = context.PackageId,
                    Name = data.Name,
                    Description = data.Description,
                    SongCount = data.Songs.Count,
                    DateAdded = DateTime.Now,
                    EditData = data
                });

                progress.Report("Cleaning up...");
            }
            finally
            {
                if (Directory.Exists(context.TempRoot))
                {
                    Directory.Delete(context.TempRoot, true);
                }
            }
        }

        private async Task<PackageImportContext> CreateContext(PackageData data, string? oldPackageId)
        {
            var tempRoot = this.CreateImporterTempDirectory();
            var beeResourcesPath = Path.Combine(tempRoot, "resources", "BEE2");
            var beeSamplePath = Path.Combine(tempRoot, "resources", "music_samp");
            var gameMusicPath = Path.Combine(tempRoot, "resources", "sound", "music");

            Directory.CreateDirectory(beeResourcesPath);
            Directory.CreateDirectory(beeSamplePath);
            Directory.CreateDirectory(gameMusicPath);

            return new PackageImportContext
            {
                Data = data,
                OldPackageId = oldPackageId,
                PackageId = GetPackageId(data),
                TempRoot = tempRoot,
                BeeResourcesPath = beeResourcesPath,
                BeeSamplePath = beeSamplePath,
                GameMusicPath = gameMusicPath,
                OldEditData = oldPackageId != null ? await this.metadataStore.LoadEditData(oldPackageId) : null,
                OldBeePackPath = GetOldBeePackPath(oldPackageId)
            };
        }

        private async Task ImportSong(
            PackageImportContext context,
            PackageSong song,
            BeemodMetaBuilder builder,
            IProgress<string> progress)
        {
            progress.Report($"Processing {song.Name}...");

            await Task.Delay(500);

            var names = new PackageAssetNames(song);
            var cache = this.cacheService.GetSongCache(context, song);
            var audioResult = this.audioAssetWriter.WriteSongAudio(context, song, names, cache, progress);

            progress.Report("Copying resources...");
            this.iconAssetWriter.WriteIcon(context, song, names, cache);

            builder.AddMusic(new BeemodPackMusic
            {
                Name = song.Name,
                Description = song.Description,
                Authors = song.Authors,
                Group = song.Group ?? context.Data.DefaultGroup,
                Icon = names.IconFileName,
                IconLarge = names.IconLargeFileName,
                UseDefaultTractorBeamMusic = song.UseDefaultTractorBeamMusic,
                SyncTractorBeamMusic = song.SyncTractorBeamMusic,
                SampleBase = names.BaseSampleFileName,
                SampleTractorBeam = song.TractorBeamFullPath != null ? names.FunnelSampleFileName : null,
                SoundscriptBase = string.Join('/', "music", names.BaseGameFileName),
                SoundscriptTractorBeam = song.TractorBeamFullPath != null ? string.Join('/', "music", names.FunnelGameFileName) : null,
                SpeedGelSfx = audioResult.SpeedGelSfxs.Select(x => string.Join('/', "music", x)).ToList(),
                BounceGelSfx = audioResult.BounceGelSfxs.Select(x => string.Join('/', "music", x)).ToList()
            });
        }

        private string CreateImporterTempDirectory()
        {
            var path = Path.Combine(this.tempPath, $"import-{this.importerGuid}");
            Directory.CreateDirectory(path);
            return path;
        }

        private static string GetPackageId(PackageData data)
            => string.Format(Constants.PackageIdPattern, Utils.ConvertToSafeFileName(data.Name).ToUpperInvariant());

        private static string? GetOldBeePackPath(string? oldPackageId)
        {
            if (oldPackageId == null)
            {
                return null;
            }

            var candidatePath = BmpcMetadataStore.GetBeePackagePath(oldPackageId);
            return File.Exists(candidatePath) ? candidatePath : null;
        }
    }
}
