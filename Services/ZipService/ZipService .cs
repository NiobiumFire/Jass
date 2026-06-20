using System.IO.Compression;

namespace BelotWebApp.Services.ZipService
{
    public class ZipService : IZipService
    {
        //private readonly ILogger<ZipService> _logger;

        //public ZipService(ILogger<ZipService> logger)
        public ZipService()
        {
            //_logger = logger;
        }

        public void Zip(string sourceFilePath, string zipFilePath, bool deleteSource = true)
        {
            if (!File.Exists(sourceFilePath))
            {
                return;
            }

            using var zipStream = new FileStream(zipFilePath, FileMode.Create);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);
            archive.CreateEntryFromFile(sourceFilePath, Path.GetFileName(sourceFilePath), CompressionLevel.SmallestSize);

            if (deleteSource)
            {
                File.Delete(sourceFilePath);
            }

            //_logger.LogInformation("Zipped {Source} to {Zip}", sourceFilePath, zipFilePath);
        }

        public string? ReadText(string zipFilePath, string? entryName = null)
        {
            using var zipStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            var entry = entryName != null ? archive.GetEntry(entryName) : archive.Entries[0];

            if (entry == null)
            {
                return null;
            }

            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            return reader.ReadToEnd();
        }

        public async Task<string?> ReadTextAsync(string zipFilePath, string? entryName = null)
        {
            using var zipStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, useAsync: true);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            var entry = entryName != null ? archive.GetEntry(entryName) : archive.Entries[0];

            if (entry == null)
            {
                return null;
            }

            using var entryStream = entry.Open();
            using var reader = new StreamReader(entryStream);
            return await reader.ReadToEndAsync();
        }

        public IEnumerable<string?> ReadLines(string zipFilePath, string? entryName = null)
        {
            using var zipStream = new FileStream(zipFilePath, FileMode.Open, FileAccess.Read);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);
            var entry = entryName != null ? archive.GetEntry(entryName) : archive.Entries[0];

            if (entry == null)
            {
                yield return null;
            }
            else
            {
                using var entryStream = entry.Open();
                using var reader = new StreamReader(entryStream);
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    yield return line;
                }
            }
        }
    }
}
