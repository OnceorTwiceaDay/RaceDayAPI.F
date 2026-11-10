using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace RaceDay.Api.Services;

public sealed class BlobImageStore
{
    private readonly BlobServiceClient? _client;
    public bool IsConfigured => _client is not null;
    private static readonly Dictionary<string, string> MimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp"
    };
    // The same Blob SDK works with local Azurite and real Azure Storage.
    // Only the connection string changes between environments.
    public BlobImageStore(IConfiguration config)
    {
        var connection = config["AzureStorage:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(connection))
            _client = new BlobServiceClient(connection);
    }

    public static bool IsAccepted(IFormFile file)
    {
        if (file.Length == 0 || file.Length > 5 * 1024 * 1024) return false;
        var ext = Path.GetExtension(file.FileName);
        return MimeTypes.ContainsKey(ext) &&
               MimeTypes[ext].Equals(file.ContentType, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<string> Upload(IFormFile file, string containerName, CancellationToken token)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var name = $"{Guid.NewGuid():N}{extension}";
        var container = (_client ?? throw new InvalidOperationException("Azure Blob Storage is not configured.")).GetBlobContainerClient(containerName);
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: token);
        var blob = container.GetBlobClient(name);
        await using var stream = file.OpenReadStream();
        await blob.UploadAsync(stream, new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = MimeTypes[extension] }
        }, token);
        return $"/api/media/{containerName}/{name}";
    }

    public async Task<(Stream Stream, string ContentType)?> Download(string containerName, string name, CancellationToken token)
    {
        var blob = (_client ?? throw new InvalidOperationException("Blob Storage is not configured.")).GetBlobContainerClient(containerName).GetBlobClient(name);
        if (!await blob.ExistsAsync(token)) return null;
        var result = await blob.DownloadStreamingAsync(cancellationToken: token);
        return (result.Value.Content, result.Value.Details.ContentType ?? "application/octet-stream");
    }
}
