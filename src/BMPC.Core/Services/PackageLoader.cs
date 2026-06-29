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

        public ICollection<BmpcPackage> LoadPackages()
        {
            var result = new List<BmpcPackage>();

            var files = Directory.GetFiles(this.packageDirectory, "*.bmpc");
            foreach (var f in files)
            {
                var file = new FileInfo(f);
                var package = JsonSerializer.Deserialize<BmpcPackage>(File.ReadAllText(file.FullName));
                if (package is not null)
                {
                    if (!File.Exists(Path.Combine(Constants.BeePackagesDirectory, package.Id + Constants.BeePackageFileExtension)))
                    {
                        file.Delete();
                        continue;
                    }

                    result.Add(package);
                }
            }

            return result;
        }
    }
}
