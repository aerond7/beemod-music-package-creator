using System.IO.Compression;

namespace BMPC.Core.Packaging
{
    internal static class PackageArchiveEntryExtractor
    {
        public static void Extract(string zipPath, string entryPath, string destPath, bool optional = false)
        {
            using var zip = ZipFile.OpenRead(zipPath);
            var entry = zip.GetEntry(entryPath);
            if (entry == null)
            {
                if (optional)
                {
                    return;
                }

                throw new InvalidOperationException($"Expected entry '{entryPath}' was not found in the existing package.");
            }

            entry.ExtractToFile(destPath, overwrite: true);
        }
    }
}
