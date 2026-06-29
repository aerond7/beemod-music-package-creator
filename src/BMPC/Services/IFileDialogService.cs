namespace BMPC.Services
{
    public interface IFileDialogService
    {
        string? OpenFile(string title, string filter);

        IReadOnlyList<string> OpenFiles(string title, string filter);
    }
}
