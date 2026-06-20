namespace BelotWebApp.Services.ZipService
{
    public interface IZipService
    {
        void Zip(string sourceFilePath, string zipFilePath, bool deleteSource = true);
        string? ReadText(string zipFilePath, string? entryName = null);
        Task<string?> ReadTextAsync(string zipFilePath, string? entryName = null);
        IEnumerable<string?> ReadLines(string zipFilePath, string? entryName = null);
    }
}
