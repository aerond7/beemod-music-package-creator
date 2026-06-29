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
    }
}
