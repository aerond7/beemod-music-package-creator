using System.IO.Compression;
using System.Text.Json;
using BMPC.Core.Models;
using BMPC.Core.Services;

namespace BMPC.Core.Tests;

public class PackageServiceTests
{
    [Fact]
    public async Task Import_WhenEditOutputInvalid_KeepsOldFilesAndCleansTempDirectory()
    {
        using var scope = CurrentDirectoryScope.Create();
        var tempRoot = CreateTempRoot(scope);
        var oldData = CreatePackageData("Test Package", "Theme");
        var oldPackageId = GetPackageId(oldData.Name);
        CreateExistingPackage(oldPackageId, oldData, "old description");
        var oldBeeBytes = File.ReadAllBytes(GetBeePath(oldPackageId));
        var oldMetadata = File.ReadAllText(GetBmpcPath(oldPackageId));
        var service = new PackageService(tempRoot, new EmptyArchiveWriter());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.Import(CreatePackageData("Test Package", "Theme"), oldPackageId));

        Assert.Contains("was not created or is empty", ex.Message);
        Assert.Equal(oldBeeBytes, File.ReadAllBytes(GetBeePath(oldPackageId)));
        Assert.Equal(oldMetadata, File.ReadAllText(GetBmpcPath(oldPackageId)));
        AssertNoTransactionArtifacts(tempRoot, oldPackageId);
    }

    [Fact]
    public async Task Import_WhenEditSucceeds_ReplacesOldFilesAndCleansTempDirectory()
    {
        using var scope = CurrentDirectoryScope.Create();
        var tempRoot = CreateTempRoot(scope);
        var oldData = CreatePackageData("Test Package", "Theme");
        var packageId = GetPackageId(oldData.Name);
        CreateExistingPackage(packageId, oldData, "old description");
        var newData = CreatePackageData("Test Package", "Theme", "new description");
        var service = new PackageService(tempRoot);

        await service.Import(newData, packageId);

        var package = ReadPackage(packageId);
        Assert.Equal("new description", package.Description);
        Assert.Equal(newData.Description, package.EditData?.Description);
        AssertZipContains(GetBeePath(packageId), "info.txt");
        AssertZipContains(GetBeePath(packageId), "resources/music_samp/bmpc_sample_theme.mp3");
        AssertNoTransactionArtifacts(tempRoot, packageId);
    }

    [Fact]
    public async Task Import_WhenEditRenamesPackage_CreatesNewFilesThenRemovesOldFiles()
    {
        using var scope = CurrentDirectoryScope.Create();
        var tempRoot = CreateTempRoot(scope);
        var oldData = CreatePackageData("Old Package", "Theme");
        var oldPackageId = GetPackageId(oldData.Name);
        CreateExistingPackage(oldPackageId, oldData, "old description");
        var newData = CreatePackageData("New Package", "Theme", "new description");
        var newPackageId = GetPackageId(newData.Name);
        var service = new PackageService(tempRoot);

        await service.Import(newData, oldPackageId);

        Assert.False(File.Exists(GetBeePath(oldPackageId)));
        Assert.False(File.Exists(GetBmpcPath(oldPackageId)));
        Assert.True(File.Exists(GetBeePath(newPackageId)));
        Assert.True(File.Exists(GetBmpcPath(newPackageId)));
        Assert.Equal("new description", ReadPackage(newPackageId).Description);
        AssertNoTransactionArtifacts(tempRoot, oldPackageId, newPackageId);
    }

    [Fact]
    public async Task Import_WhenNewPackageCollides_DoesNotOverwriteExistingFiles()
    {
        using var scope = CurrentDirectoryScope.Create();
        var tempRoot = CreateTempRoot(scope);
        var packageId = GetPackageId("Test Package");
        Directory.CreateDirectory(Constants.BeePackagesDirectory);
        Directory.CreateDirectory(Constants.PackagesDirectory);
        File.WriteAllText(GetBeePath(packageId), "existing bee");
        File.WriteAllText(GetBmpcPath(packageId), "existing metadata");
        var service = new PackageService(tempRoot);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.Import(new PackageData { Name = "Test Package" }));

        Assert.Contains($"Package '{packageId}' already exists.", ex.Message);
        Assert.Equal("existing bee", File.ReadAllText(GetBeePath(packageId)));
        Assert.Equal("existing metadata", File.ReadAllText(GetBmpcPath(packageId)));
        AssertNoTransactionArtifacts(tempRoot, packageId);
    }

    private static string CreateTempRoot(CurrentDirectoryScope scope)
    {
        var tempRoot = Path.Combine(scope.DirectoryPath, "import-temp");
        Directory.CreateDirectory(tempRoot);
        return tempRoot;
    }

    private static PackageData CreatePackageData(string name, string songName, string description = "")
        => new()
        {
            Name = name,
            Description = description,
            DefaultGroup = "Test",
            Songs =
            [
                new PackageSong
                {
                    SongId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    Name = songName,
                    Description = "Song description",
                    Authors = "Tester",
                    BaseFullPath = @"C:\missing\source.wav"
                }
            ]
        };

    private static void CreateExistingPackage(string packageId, PackageData editData, string description)
    {
        Directory.CreateDirectory(Constants.BeePackagesDirectory);
        Directory.CreateDirectory(Constants.PackagesDirectory);

        using (var archive = ZipFile.Open(GetBeePath(packageId), ZipArchiveMode.Create))
        {
            WriteZipEntry(archive, "resources/music_samp/bmpc_sample_theme.mp3", "sample");
            WriteZipEntry(archive, "resources/sound/music/bmpc_theme.wav", "wav");
        }

        File.WriteAllText(GetBmpcPath(packageId), JsonSerializer.Serialize(new BmpcPackage
        {
            Id = packageId,
            Name = editData.Name,
            Description = description,
            SongCount = editData.Songs.Count,
            DateAdded = DateTime.UtcNow,
            EditData = editData
        }));
    }

    private static void WriteZipEntry(ZipArchive archive, string entryName, string contents)
    {
        var entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(contents);
    }

    private static BmpcPackage ReadPackage(string packageId)
        => JsonSerializer.Deserialize<BmpcPackage>(File.ReadAllText(GetBmpcPath(packageId)))!;

    private static string GetPackageId(string name)
        => string.Format(Constants.PackageIdPattern, Utils.ConvertToSafeFileName(name).ToLowerInvariant());

    private static string GetBeePath(string packageId)
        => Path.Combine(Constants.BeePackagesDirectory, packageId + Constants.BeePackageFileExtension);

    private static string GetBmpcPath(string packageId)
        => Path.Combine(Constants.PackagesDirectory, packageId + ".bmpc");

    private static void AssertZipContains(string archivePath, string entryName)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        Assert.NotNull(archive.GetEntry(entryName));
    }

    private static void AssertNoTransactionArtifacts(string tempRoot, params string[] packageIds)
    {
        Assert.Empty(Directory.GetDirectories(tempRoot));

        foreach (var packageId in packageIds)
        {
            Assert.False(File.Exists(GetBeePath(packageId) + ".tmp"));
            Assert.False(File.Exists(GetBmpcPath(packageId) + ".tmp"));
            Assert.False(File.Exists(GetBeePath(packageId) + ".bak"));
            Assert.False(File.Exists(GetBmpcPath(packageId) + ".bak"));
        }
    }

    private sealed class EmptyArchiveWriter : IPackageArchiveWriter
    {
        public void CreateFromDirectory(string sourceDirectoryName, string destinationArchiveFileName)
        {
            using var _ = File.Create(destinationArchiveFileName);
        }
    }
}
