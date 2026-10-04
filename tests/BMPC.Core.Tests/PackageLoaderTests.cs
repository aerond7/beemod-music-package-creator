using System.Text.Json;
using BMPC.Core.Models;
using BMPC.Core.Services;

namespace BMPC.Core.Tests;

public class PackageLoaderTests
{
    [Fact]
    public void LoadPackages_WhenBmpcAndBeePackExist_LoadsPackage()
    {
        using var scope = CurrentDirectoryScope.Create();
        var metadataDirectory = Path.Combine(scope.DirectoryPath, Constants.PackagesDirectory);
        var beePackageDirectory = Path.Combine(scope.DirectoryPath, Constants.BeePackagesDirectory);
        Directory.CreateDirectory(metadataDirectory);
        Directory.CreateDirectory(beePackageDirectory);

        var package = new BmpcPackage
        {
            Id = "BMPC_TEST_PACK",
            Name = "Test package",
            DateAdded = new DateTime(2026, 6, 29, 0, 0, 0, DateTimeKind.Utc)
        };
        File.WriteAllText(Path.Combine(metadataDirectory, "test.bmpc"), JsonSerializer.Serialize(package));
        File.WriteAllText(Path.Combine(beePackageDirectory, "BMPC_TEST_PACK.bee_pack"), "bee");

        var packages = new PackageLoader(metadataDirectory).LoadPackages();

        var loaded = Assert.Single(packages);
        Assert.Equal("BMPC_TEST_PACK", loaded.Id);
        Assert.Equal("Test package", loaded.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("{\"Id\":\"BMPC_BRO")]
    [InlineData("null")]
    [InlineData("{}")]
    public void LoadPackages_WhenFileCorrupt_SkipsItAndLoadsOtherPackages(string corruptContents)
    {
        using var scope = CurrentDirectoryScope.Create();
        var metadataDirectory = Path.Combine(scope.DirectoryPath, Constants.PackagesDirectory);
        var beePackageDirectory = Path.Combine(scope.DirectoryPath, Constants.BeePackagesDirectory);
        Directory.CreateDirectory(metadataDirectory);
        Directory.CreateDirectory(beePackageDirectory);

        File.WriteAllText(Path.Combine(metadataDirectory, "valid.bmpc"), JsonSerializer.Serialize(new BmpcPackage
        {
            Id = "BMPC_VALID_PACK",
            Name = "Valid package"
        }));
        File.WriteAllText(Path.Combine(beePackageDirectory, "BMPC_VALID_PACK.bee_pack"), "bee");
        var corruptPath = Path.Combine(metadataDirectory, "corrupt.bmpc");
        File.WriteAllText(corruptPath, corruptContents);

        var loader = new PackageLoader(metadataDirectory);
        var packages = loader.LoadPackages();

        var loaded = Assert.Single(packages);
        Assert.Equal("BMPC_VALID_PACK", loaded.Id);
        Assert.Equal(corruptPath, Assert.Single(loader.InvalidFiles));
        Assert.True(File.Exists(corruptPath));
    }

    [Fact]
    public void LoadPackages_WhenBeePackMissing_DeletesStaleMetadataAndSkipsPackage()
    {
        using var scope = CurrentDirectoryScope.Create();
        var metadataDirectory = Path.Combine(scope.DirectoryPath, Constants.PackagesDirectory);
        Directory.CreateDirectory(metadataDirectory);
        Directory.CreateDirectory(Path.Combine(scope.DirectoryPath, Constants.BeePackagesDirectory));
        var metadataPath = Path.Combine(metadataDirectory, "stale.bmpc");
        File.WriteAllText(metadataPath, JsonSerializer.Serialize(new BmpcPackage
        {
            Id = "BMPC_STALE_PACK",
            Name = "Stale package"
        }));

        var packages = new PackageLoader(metadataDirectory).LoadPackages();

        Assert.Empty(packages);
        Assert.False(File.Exists(metadataPath));
    }
}
