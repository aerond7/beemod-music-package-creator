namespace BMPC.Services
{
    public interface IProcessLauncher
    {
        bool IsProcessRunning(string processName);

        bool TryLaunch(string fileName);

        bool RevealInFileExplorer(string path);

        bool OpenFolder(string path);
    }
}
