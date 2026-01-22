using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using YouView.Data;
using YouView.Models;
using YouView.Services; 

namespace YouView.Pages
{
    [Authorize]
    public class Create : PageModel
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly YouViewDbContext _db;
        private readonly VideoProcessor _videoProcessor;
        private readonly IWebHostEnvironment _environment;
        private readonly AiService _aiService;

        public Create(
            BlobServiceClient blobServiceClient, 
            YouViewDbContext db, 
            VideoProcessor videoProcessor, 
            IWebHostEnvironment environment,
            AiService aiService)
        {
            _blobServiceClient = blobServiceClient;
            _db = db;
            _videoProcessor = videoProcessor;
            _environment = environment;
            _aiService = aiService;
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
            if (string.IsNullOrEmpty(userId)) return Page();

            // Setup Temp Paths
            var tempFolder = Path.Combine(_environment.WebRootPath, "temp");
            Directory.CreateDirectory(tempFolder);

            var uniqueId = Guid.NewGuid().ToString();
            
            // 1. Raw Path (Original Upload)
            var tempVideoPath = Path.Combine(tempFolder, $"{uniqueId}_{VideoFile.FileName}");
            
            //  Processed Path 
            var tempProcessedPath = Path.Combine(tempFolder, $"{uniqueId}.mp4");
            
            var tempThumbPath = Path.Combine(tempFolder, $"{uniqueId}_thumb.jpg");
            var tempPreviewPath = Path.Combine(tempFolder, $"{uniqueId}_preview.gif"); 
            var tempAudioPath = Path.Combine(tempFolder, $"{uniqueId}.mp3");

            try
            {
                //  Save Uploaded Video to Temp File
                using (var stream = new FileStream(tempVideoPath, FileMode.Create))
                {
                    await VideoFile.CopyToAsync(stream);
                }
                //  Convert to MP4 (Standardize Format)              
                bool conversionSuccess = await _videoProcessor.ProcessVideoUploadAsync(tempVideoPath, tempProcessedPath);
                
                if (!conversionSuccess)
                {
                    Message = "Error converting video format. Please try again.";
                    return Page();
                }

                // Process Metadata 
                var duration = await _videoProcessor.GetVideoDurationAsync(tempProcessedPath);
                
                // Generate GIF from converted file
                await _videoProcessor.GenerateGifPreviewAsync(tempProcessedPath, tempPreviewPath);
                
                // AI Services
                string aiSummary = "Processing...";
                
                // Extract Audio from converted file
                bool audioExtracted = await _videoProcessor.ExtractAudioAsync(tempProcessedPath, tempAudioPath);

                if (audioExtracted)
                {
                    string transcript = await _aiService.TranscribeAudioAsync(tempAudioPath);
                    if (!string.IsNullOrEmpty(transcript))
                    {
                        aiSummary = await _aiService.GenerateSummaryAsync(transcript);
                    }
                    else 
                    {
                        aiSummary = "Could not transcribe audio.";
                    }
                }
                else
                {
                    aiSummary = "No audio track found.";
                }
                
                // Generate Thumbnail (if needed) from converted file
                if (ThumbnailFile == null)
                {
                    await _videoProcessor.GenerateThumbnailAsync(tempProcessedPath, tempThumbPath);
                }

                //  Upload to Azure (Upload the MP4, not the raw file)
                var vidContainer = _blobServiceClient.GetBlobContainerClient("videos");
                await vidContainer.CreateIfNotExistsAsync(PublicAccessType.Blob);
                
                // Force .mp4 extension for consistency
                var vidBlob = vidContainer.GetBlobClient($"{uniqueId}.mp4");
                
                // Upload the PROCESSED file
                await vidBlob.UploadAsync(tempProcessedPath, true);
                var videoUrl = vidBlob.Uri.ToString();

                // Upload Thumbnail to Azure
                string thumbnailUrl = "";
                var thumbContainer = _blobServiceClient.GetBlobContainerClient("thumbnails");
                await thumbContainer.CreateIfNotExistsAsync(PublicAccessType.Blob);

                if (ThumbnailFile != null)
                {
                    var thumbBlob = thumbContainer.GetBlobClient($"{uniqueId}{Path.GetExtension(ThumbnailFile.FileName)}");
                    using var s = ThumbnailFile.OpenReadStream();
                    await thumbBlob.UploadAsync(s, true);
                    thumbnailUrl = thumbBlob.Uri.ToString();
                }
                else if (System.IO.File.Exists(tempThumbPath))
                {
                    var thumbBlob = thumbContainer.GetBlobClient($"{uniqueId}_thumb.jpg");
                    await thumbBlob.UploadAsync(tempThumbPath, true);
                    thumbnailUrl = thumbBlob.Uri.ToString();
                }

                // Upload GIF to Azure
                string previewUrl = "";
                if (System.IO.File.Exists(tempPreviewPath))
                {
                    var prevContainer = _blobServiceClient.GetBlobContainerClient("previews");
                    await prevContainer.CreateIfNotExistsAsync(PublicAccessType.Blob);
                    var prevBlob = prevContainer.GetBlobClient($"{uniqueId}_preview.gif");
                    await prevBlob.UploadAsync(tempPreviewPath, true);
                    previewUrl = prevBlob.Uri.ToString();
                }
                
                // Save to Database
                var video = new Video
                {
                    UserId = userId,
                    Title = Title,
                    Description = Description,
                    VideoUrl = videoUrl,
                    ThumbnailUrl = thumbnailUrl,
                    Duration = duration.ToString(@"hh\:mm\:ss"),
                    PrivacyStatus = PrivacyStatus,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AiSummary = aiSummary,
                    PreviewUrl = previewUrl, 
                    SubtitlesUrl = ""
                };

                _db.Videos.Add(video);
                await _db.SaveChangesAsync();

                Message = "Video uploaded successfully!";
                return Page(); 
            }
            catch (Exception ex)
            {
                Message = $"Error: {ex.Message}";
                return Page();
            }
            finally
            {
                // Cleanup ALL temp files
                if (System.IO.File.Exists(tempVideoPath)) System.IO.File.Delete(tempVideoPath);       // Raw
                if (System.IO.File.Exists(tempProcessedPath)) System.IO.File.Delete(tempProcessedPath); // MP4
                if (System.IO.File.Exists(tempThumbPath)) System.IO.File.Delete(tempThumbPath);
                if (System.IO.File.Exists(tempPreviewPath)) System.IO.File.Delete(tempPreviewPath);
                if (System.IO.File.Exists(tempAudioPath)) System.IO.File.Delete(tempAudioPath);
            }
        }
    }
}