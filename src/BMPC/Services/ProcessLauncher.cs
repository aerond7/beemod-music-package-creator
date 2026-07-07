using System.Diagnostics;
using System.IO;

namespace BMPC.Services
{
    public sealed class ProcessLauncher : IProcessLauncher
    {
        public bool IsProcessRunning(string processName)
            => Process.GetProcessesByName(processName).Length > 0;

        public bool TryLaunch(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return false;
            }

            Process.Start(fileName);
            return true;
        }

        public bool RevealInFileExplorer(string path)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
            {
                return false;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{path}\"",
                UseShellExecute = true
            });
            return true;
        }
    }
}
