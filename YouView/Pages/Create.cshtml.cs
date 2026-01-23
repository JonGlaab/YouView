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

        public Create (
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
            var originalExtension = Path.GetExtension(VideoFile.FileName);
            
            // 1. Raw Path (We will use this for everything)
            var tempVideoPath = Path.Combine(tempFolder, $"{uniqueId}{originalExtension}");
            
            var tempThumbPath = Path.Combine(tempFolder, $"{uniqueId}_thumb.jpg");
            var tempPreviewPath = Path.Combine(tempFolder, $"{uniqueId}_preview.gif"); 
            var tempAudioPath = Path.Combine(tempFolder, $"{uniqueId}.mp3");

            try
            {
                
                using (var stream = new FileStream(tempVideoPath, FileMode.Create))
                {
                    await VideoFile.CopyToAsync(stream);
                }

                
                TimeSpan duration = TimeSpan.Zero;
                string aiSummary = "Processing...";
                string previewUrl = "";

                try 
                {
                    // 1. Get Duration
                    duration = await _videoProcessor.GetVideoDurationAsync(tempVideoPath);

                    // 2. Generate GIF Preview
                    if (await _videoProcessor.GenerateGifPreviewAsync(tempVideoPath, tempPreviewPath))
                    {
                        var prevContainer = _blobServiceClient.GetBlobContainerClient("previews");
                        await prevContainer.CreateIfNotExistsAsync(PublicAccessType.Blob);
                        var prevBlob = prevContainer.GetBlobClient($"{uniqueId}_preview.gif");
                        await prevBlob.UploadAsync(tempPreviewPath, true);
                        previewUrl = prevBlob.Uri.ToString();
                    }

                    // 3. AI Pipeline (Audio -> Transcribe -> Summary)
                    if (await _videoProcessor.ExtractAudioAsync(tempVideoPath, tempAudioPath))
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
                } 
                catch (Exception ex)
                {
                    Console.WriteLine($"Asset generation failed: {ex.Message}");
                    aiSummary = "AI Summary unavailable.";
                }
                
                // Step C: Generate Thumbnail (if user didn't provide one)
                if (ThumbnailFile == null)
                {
                    try {
                        await _videoProcessor.GenerateThumbnailAsync(tempVideoPath, tempThumbPath);
                    } catch { /* Ignore if fails */ }
                }

                // Step D: Upload Video to Azure (The Original File)
                var vidContainer = _blobServiceClient.GetBlobContainerClient("videos");
                await vidContainer.CreateIfNotExistsAsync(PublicAccessType.Blob);
                
                var vidBlob = vidContainer.GetBlobClient($"{uniqueId}{originalExtension}");
                
                await vidBlob.UploadAsync(tempVideoPath, true);
                var videoUrl = vidBlob.Uri.ToString();
                
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

                // Step F: Save to Database
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
                // Cleanup
                if (System.IO.File.Exists(tempVideoPath)) System.IO.File.Delete(tempVideoPath);
                if (System.IO.File.Exists(tempThumbPath)) System.IO.File.Delete(tempThumbPath);
                if (System.IO.File.Exists(tempPreviewPath)) System.IO.File.Delete(tempPreviewPath);
                if (System.IO.File.Exists(tempAudioPath)) System.IO.File.Delete(tempAudioPath);
            }
        }
    }
}