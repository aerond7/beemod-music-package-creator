using BMPC.Core.Packaging;
using System.Threading;

namespace BMPC.Core.Services
{
    public interface IPackageArchiveWriter
    {
        void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName);
    }

    public sealed class PackageArchiveWriter : IPackageArchiveWriter
    {
        public void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
            => System.IO.Compression.ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName);
    }

    public class PackageService
    {
        public event Action<string>? StatusUpdated;

        private readonly PackageImportService importService;
        private readonly BmpcMetadataStore metadataStore;

        public PackageService(string? tempPath = null, IPackageArchiveWriter? archiveWriter = null)
        {
            this.metadataStore = new BmpcMetadataStore();
            this.importService = new PackageImportService(
                tempPath,
                this.metadataStore,
                new BeePackageWriter(archiveWriter ?? new PackageArchiveWriter(), this.metadataStore));
        }

        public Task Import(Models.PackageData data, string? oldPackageId = null)
        {
            var progress = new PackageStatusProgress(
                message => this.StatusUpdated?.Invoke(message),
                SynchronizationContext.Current);
            return Task.Run(() => this.importService.Import(data, oldPackageId, progress));
        }

        public void DeletePackage(string packageId)
            => this.metadataStore.DeletePackage(packageId);

        private sealed class PackageStatusProgress : IProgress<string>
        {
            private readonly Action<string> report;
            private readonly SynchronizationContext? synchronizationContext;

            public PackageStatusProgress(Action<string> report, SynchronizationContext? synchronizationContext)
            {
                this.report = report;
                this.synchronizationContext = synchronizationContext;
            }

            public void Report(string value)
            {
                if (this.synchronizationContext == null)
                {
                    this.report(value);
                    return;
                }

                this.synchronizationContext.Post(_ => this.report(value), null);
            }
        }
    }
}
