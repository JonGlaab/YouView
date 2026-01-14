using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using YouView.Data;
using YouView.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;


namespace YouView.Pages
{
    [Authorize]
    public class Create : PageModel
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly YouViewDbContext _db;

        public Create(BlobServiceClient blobServiceClient, YouViewDbContext db)
        {
            _blobServiceClient = blobServiceClient;
            _db = db;
        }

        [BindProperty] public IFormFile VideoFile { get; set; }

        [BindProperty] public string Title { get; set; }

        [BindProperty] public string Description { get; set; }

        [BindProperty] public PrivacyStatus PrivacyStatus { get; set; }
        
        [BindProperty] public IFormFile? ThumbnailFile { get; set; }


        public string Message { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (VideoFile == null || VideoFile.Length == 0)
            {
                Message = "Please select a video file.";
                return Page();
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                Message = "Unable to determine user.";
                return Page();
            }
            
            try
            {
                var container = _blobServiceClient.GetBlobContainerClient("videos");
                await container.CreateIfNotExistsAsync();

                var blobName = $"{Guid.NewGuid()}{Path.GetExtension(VideoFile.FileName)}";
                var blobClient = container.GetBlobClient(blobName);

                string? thumbnailUrl = null;

                if (ThumbnailFile != null && ThumbnailFile.Length > 0)
                {
                    var thumbContainer = _blobServiceClient.GetBlobContainerClient("thumbnails");
                    await thumbContainer.CreateIfNotExistsAsync();

                    var thumbName = $"{Guid.NewGuid()}{Path.GetExtension(ThumbnailFile.FileName)}";
                    var thumbBlob = thumbContainer.GetBlobClient(thumbName);

                    using var thumbStream = ThumbnailFile.OpenReadStream();
                    await thumbBlob.UploadAsync(thumbStream, overwrite: true);

                    thumbnailUrl = thumbBlob.Uri.ToString();
                }
                else
                {
                    thumbnailUrl = "";

                }
                
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

                var video = new Video
                {
                    UserId = userId,
                    Title = Title,
                    Description = Description,
                    VideoUrl = blobUrl,
                    PrivacyStatus = PrivacyStatus,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AiSummary = "",
                    Duration = "",
                    SubtitlesUrl = "",
                    ThumbnailUrl = thumbnailUrl

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