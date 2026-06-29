using System.Text.Json;
using BMPC.Core.Models;

namespace BMPC.Core.Packaging
{
    public sealed class BmpcMetadataStore
    {
        public async Task<PackageData?> LoadEditData(string packageId)
        {
            var path = GetBmpcPackagePath(packageId);
            if (!File.Exists(path))
            {
                return null;
            }

            var package = JsonSerializer.Deserialize<BmpcPackage>(await File.ReadAllTextAsync(path));
            return package?.EditData;
        }

        public Task WriteTempPackage(string path, BmpcPackage package)
            => File.WriteAllTextAsync(path, JsonSerializer.Serialize(package));

        public void DeletePackage(string packageId)
        {
            DeleteIfExists(GetBeePackagePath(packageId));
            DeleteIfExists(GetBmpcPackagePath(packageId));
        }

        public static string GetBeePackagePath(string packageId)
            => Path.Combine(Constants.BeePackagesDirectory, packageId + Constants.BeePackageFileExtension);

        public static string GetBmpcPackagePath(string packageId)
            => Path.Combine(Constants.PackagesDirectory, packageId + ".bmpc");

        internal static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
