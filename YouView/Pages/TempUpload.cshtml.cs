using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YouView.Data;
using YouView.Models;

namespace YouView.Pages
{
    public class TempUpload : PageModel
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly YouViewDbContext _db;

        public TempUpload(BlobServiceClient blobServiceClient, YouViewDbContext db)
        {
            _blobServiceClient = blobServiceClient;
            _db = db;
        }

        [BindProperty] public IFormFile VideoFile { get; set; }

        [BindProperty] public string Title { get; set; }

        [BindProperty] public string Description { get; set; }

        [BindProperty] public PrivacyStatus PrivacyStatus { get; set; }

        public string Message { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (VideoFile == null || VideoFile.Length == 0)
            {
                Message = "Please select a video file.";
                return Page();
            }

            try
            {
                // 1️⃣ Upload to Azure Blob Storage
                var container = _blobServiceClient.GetBlobContainerClient("videos");
                await container.CreateIfNotExistsAsync();

                var blobName = $"{Guid.NewGuid()}{Path.GetExtension(VideoFile.FileName)}";
                var blobClient = container.GetBlobClient(blobName);

                var uploadOptions = new Azure.Storage.Blobs.Models.BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders
                    {
                        ContentType = "video/mp4"
                    },
                    TransferOptions = new Azure.Storage.StorageTransferOptions
                    {
                        MaximumTransferSize = 4 * 1024 * 1024, // 4 MB
                        InitialTransferSize = 4 * 1024 * 1024
                    }
                };

                using var stream = VideoFile.OpenReadStream();
                await blobClient.UploadAsync(stream, uploadOptions);

                var blobUrl = blobClient.Uri.ToString();

                // 2️⃣ Create Video record in DB
                var video = new Video
                {
                    UserId = "e388a552-d07c-4b47-8caf-869fed5b88a1",
                    Title = Title,
                    Description = Description,
                    VideoUrl = blobUrl,
                    PrivacyStatus = PrivacyStatus,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AiSummary = "",
                    Duration = "00:19",
                    SubtitlesUrl = "",
                    ThumbnailUrl = ""
                };

                _db.Videos.Add(video);
                await _db.SaveChangesAsync();

                Message = $"Video uploaded! URL: {blobUrl} | Video ID: {video.VideoId}";
            }
            catch (Exception ex)
            {
                Message = "Upload failed: " + ex.Message;
            }

            return Page();
        }
    }
}