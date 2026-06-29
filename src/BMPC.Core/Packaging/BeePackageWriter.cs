using BMPC.Core.Models;
using BMPC.Core.Services;

namespace BMPC.Core.Packaging
{
    public sealed class BeePackageWriter
    {
        private readonly IPackageArchiveWriter archiveWriter;
        private readonly BmpcMetadataStore metadataStore;

        public BeePackageWriter(IPackageArchiveWriter archiveWriter, BmpcMetadataStore? metadataStore = null)
        {
            this.archiveWriter = archiveWriter;
            this.metadataStore = metadataStore ?? new BmpcMetadataStore();
        }

        public async Task WritePackage(PackageImportContext context, BmpcPackage package)
        {
            Directory.CreateDirectory(Constants.BeePackagesDirectory);
            Directory.CreateDirectory(Constants.PackagesDirectory);

            var beeFinalPath = BmpcMetadataStore.GetBeePackagePath(context.PackageId);
            var bmpcFinalPath = BmpcMetadataStore.GetBmpcPackagePath(context.PackageId);
            var beeTempPath = beeFinalPath + ".tmp";
            var bmpcTempPath = bmpcFinalPath + ".tmp";
            var isEdit = context.OldPackageId != null;
            var isSamePackageEdit = context.OldPackageId == context.PackageId;

            if (!isEdit || !isSamePackageEdit)
            {
                ThrowIfPackageCollision(context.PackageId);
            }

            var success = false;
            try
            {
                BmpcMetadataStore.DeleteIfExists(beeTempPath);
                BmpcMetadataStore.DeleteIfExists(bmpcTempPath);

                this.archiveWriter.CreateFromDirectory(context.TempRoot, beeTempPath);
                await this.metadataStore.WriteTempPackage(bmpcTempPath, package);

                ValidateOutputFile(beeTempPath);
                ValidateOutputFile(bmpcTempPath);

                if (isSamePackageEdit)
                {
                    MoveReplacementIntoPlace(beeTempPath, beeFinalPath);
                    MoveReplacementIntoPlace(bmpcTempPath, bmpcFinalPath);
                }
                else
                {
                    File.Move(beeTempPath, beeFinalPath);
                    File.Move(bmpcTempPath, bmpcFinalPath);

                    if (context.OldPackageId != null)
                    {
                        BackupPackageFiles(context.OldPackageId);
                    }
                }

                success = true;
            }
            catch
            {
                BmpcMetadataStore.DeleteIfExists(beeTempPath);
                BmpcMetadataStore.DeleteIfExists(bmpcTempPath);
                if (isSamePackageEdit)
                {
                    RestorePackageBackups(context.PackageId);
                }
                else
                {
                    BmpcMetadataStore.DeleteIfExists(beeFinalPath);
                    BmpcMetadataStore.DeleteIfExists(bmpcFinalPath);
                    if (context.OldPackageId != null)
                    {
                        RestorePackageBackups(context.OldPackageId);
                    }
                }

                throw;
            }
            finally
            {
                if (success)
                {
                    DeletePackageBackups(context.PackageId);
                    if (context.OldPackageId != null && context.OldPackageId != context.PackageId)
                    {
                        DeletePackageBackups(context.OldPackageId);
                    }
                }
            }
        }

        public async Task WriteInfoFile(string sourceDirectory, Stream metadata)
        {
            using var reader = new StreamReader(metadata);
            await File.WriteAllTextAsync(Path.Combine(sourceDirectory, "info.txt"), await reader.ReadToEndAsync());
        }

        private static string GetBackupPath(string path)
            => path + ".bak";

        private static void ThrowIfPackageCollision(string packageId)
        {
            if (File.Exists(BmpcMetadataStore.GetBeePackagePath(packageId)) || File.Exists(BmpcMetadataStore.GetBmpcPackagePath(packageId)))
            {
                throw new InvalidOperationException($"Package '{packageId}' already exists.");
            }
        }

        private static void ValidateOutputFile(string path)
        {
            if (!File.Exists(path) || new FileInfo(path).Length == 0)
            {
                throw new InvalidOperationException($"Package output '{path}' was not created or is empty.");
            }
        }

        private static void MoveReplacementIntoPlace(string tempPath, string finalPath)
        {
            var backupPath = GetBackupPath(finalPath);
            BmpcMetadataStore.DeleteIfExists(backupPath);

            if (File.Exists(finalPath))
            {
                File.Move(finalPath, backupPath);
            }

            File.Move(tempPath, finalPath);
        }

        private static void BackupPackageFiles(string packageId)
        {
            MoveExistingFileToBackup(BmpcMetadataStore.GetBeePackagePath(packageId));
            MoveExistingFileToBackup(BmpcMetadataStore.GetBmpcPackagePath(packageId));
        }

        private static void MoveExistingFileToBackup(string finalPath)
        {
            var backupPath = GetBackupPath(finalPath);
            BmpcMetadataStore.DeleteIfExists(backupPath);

            if (File.Exists(finalPath))
            {
                File.Move(finalPath, backupPath);
            }
        }

        private static void RestorePackageBackups(string packageId)
        {
            RestoreBackup(BmpcMetadataStore.GetBeePackagePath(packageId));
            RestoreBackup(BmpcMetadataStore.GetBmpcPackagePath(packageId));
        }

        private static void DeletePackageBackups(string packageId)
        {
            BmpcMetadataStore.DeleteIfExists(GetBackupPath(BmpcMetadataStore.GetBeePackagePath(packageId)));
            BmpcMetadataStore.DeleteIfExists(GetBackupPath(BmpcMetadataStore.GetBmpcPackagePath(packageId)));
        }

        private static void RestoreBackup(string finalPath)
        {
            var backupPath = GetBackupPath(finalPath);
            if (!File.Exists(backupPath))
            {
                return;
            }

            BmpcMetadataStore.DeleteIfExists(finalPath);
            File.Move(backupPath, finalPath);
        }
    }
}
