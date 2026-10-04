using BMPC.Core.Models;
using System.Text.Json;

namespace BMPC.Core.Services
{
    public class PackageLoader
    {
        private readonly string packageDirectory;

        public PackageLoader(string directory)
        {
            this.packageDirectory = directory;
        }

        /// <summary>Full paths of package files skipped by the last <see cref="LoadPackages"/> call because they could not be read.</summary>
        public IReadOnlyList<string> InvalidFiles { get; private set; } = Array.Empty<string>();

        public ICollection<BmpcPackage> LoadPackages()
        {
            var result = new List<BmpcPackage>();
            var invalidFiles = new List<string>();

            var files = Directory.GetFiles(this.packageDirectory, "*.bmpc");
            foreach (var f in files)
            {
                var file = new FileInfo(f);
                var package = TryReadPackage(file);
                if (package is null)
                {
                    invalidFiles.Add(file.FullName);
                    continue;
                }

                if (!File.Exists(Path.Combine(Constants.BeePackagesDirectory, package.Id + Constants.BeePackageFileExtension)))
                {
                    file.Delete();
                    continue;
                }

                result.Add(package);
            }

            this.InvalidFiles = invalidFiles;
            return result;
        }

        // A damaged file (e.g. truncated by an interrupted write) must not stop the other packages from loading.
        private static BmpcPackage? TryReadPackage(FileInfo file)
        {
            try
            {
                var package = JsonSerializer.Deserialize<BmpcPackage>(File.ReadAllText(file.FullName));
                return string.IsNullOrWhiteSpace(package?.Id) ? null : package;
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                return null;
            }
        }
    }
}
