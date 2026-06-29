namespace BMPC.Services
{
    public interface IAppPaths
    {
        string RootDirectory { get; }

        string PackagesDirectory { get; }

        string ResourcesDirectory { get; }

        string TempDirectory { get; }

        string BeePackagesDirectory { get; }

        string BeeExecutableName { get; }
    }
}
