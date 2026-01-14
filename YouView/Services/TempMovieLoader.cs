namespace YouView.Services;

using Azure.Storage.Blobs;
using YouView.Data;
using YouView.Models;

public class TempMovieLoader
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly YouViewDbContext _db;

    public TempMovieLoader(BlobServiceClient blobServiceClient, YouViewDbContext db)
    {
        _blobServiceClient = blobServiceClient;
        _db = db;
    }

    public async Task<string> UploadFileAsync(string localFilePath)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient("videos");
        await containerClient.CreateIfNotExistsAsync();

        var fileName = Path.GetFileName(localFilePath);
        var blobName = $"{Guid.NewGuid()}{Path.GetExtension(fileName)}";

        var blobClient = containerClient.GetBlobClient(blobName);

        using var fileStream = File.OpenRead(localFilePath);
        await blobClient.UploadAsync(fileStream, overwrite: true);

        return blobClient.Uri.ToString();
    }

    public async Task<Video> AddVideoRecordAsync(
        string blobUrl,
        string userId,
        string title = "Test Video",
        string? description = null,
        PrivacyStatus privacyStatus = PrivacyStatus.Public)
    {
        var video = new Video
        {
            UserId = userId,
            Title = title,
            Description = description,
            VideoUrl = blobUrl,
            PrivacyStatus = privacyStatus,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Videos.Add(video);
        await _db.SaveChangesAsync();

        return video;
    }
}