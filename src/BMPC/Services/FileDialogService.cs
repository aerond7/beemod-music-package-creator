using Microsoft.Win32;

namespace BMPC.Services
{
    public sealed class FileDialogService : IFileDialogService
    {
        public string? OpenFile(string title, string filter)
        {
            var dialog = CreateDialog(title, filter);
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        public IReadOnlyList<string> OpenFiles(string title, string filter)
        {
            var dialog = CreateDialog(title, filter);
            dialog.Multiselect = true;
            return dialog.ShowDialog() == true ? dialog.FileNames.ToList() : Array.Empty<string>();
        }

        private static OpenFileDialog CreateDialog(string title, string filter)
            => new OpenFileDialog
            {
                AddExtension = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = filter,
                Title = title
            };
    }
}
