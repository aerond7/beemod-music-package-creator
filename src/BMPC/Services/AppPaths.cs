using BMPC.Core;

namespace BMPC.Services
{
    public sealed class AppPaths : IAppPaths
    {
        public string RootDirectory => Constants.RootDirectory;

        public string PackagesDirectory => Constants.PackagesDirectory;

        public string ResourcesDirectory => Constants.ResourcesDirectory;

        public string TempDirectory => Constants.TempDirectory;

        public string BeePackagesDirectory => Constants.BeePackagesDirectory;

        public string BeeExecutableName => Constants.BeeExecutableName;
    }
}
