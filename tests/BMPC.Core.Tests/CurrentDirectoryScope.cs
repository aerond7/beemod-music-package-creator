namespace BMPC.Core.Tests;

internal sealed class CurrentDirectoryScope : IDisposable
{
    private readonly string originalDirectory;

    private CurrentDirectoryScope(string directoryPath)
    {
        this.originalDirectory = Environment.CurrentDirectory;
        this.DirectoryPath = directoryPath;
        Environment.CurrentDirectory = directoryPath;
    }

    public string DirectoryPath { get; }

    public static CurrentDirectoryScope Create()
    {
        var directoryPath = Path.Combine(AppContext.BaseDirectory, "Temp", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directoryPath);
        return new CurrentDirectoryScope(directoryPath);
    }

    public void Dispose()
    {
        Environment.CurrentDirectory = this.originalDirectory;
        Directory.Delete(this.DirectoryPath, recursive: true);
    }
}
