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
            var tempVideoPath = Path.Combine(tempFolder, $"{uniqueId}_{VideoFile.FileName}");
            var tempThumbPath = Path.Combine(tempFolder, $"{uniqueId}_thumb.jpg");
            
            // Define GIF Path
            var tempPreviewPath = Path.Combine(tempFolder, $"{uniqueId}_preview.gif"); 
            

            try
            {
                // Save Uploaded Video to Temp File
                using (var stream = new FileStream(tempVideoPath, FileMode.Create))
                {
                    await VideoFile.CopyToAsync(stream);
                }

                // Process Video
                var duration = await _videoProcessor.GetVideoDurationAsync(tempVideoPath);
                
                // Generate GIF
                await _videoProcessor.GenerateGifPreviewAsync(tempVideoPath, tempPreviewPath);
				
				//AI services
                string aiSummary = "Processing...";
                
                // 1. Extract Audio from Video (using the new "Potato Quality" settings)
                var tempAudioPath = Path.Combine(tempFolder, $"{uniqueId}.mp3");
                bool audioExtracted = await _videoProcessor.ExtractAudioAsync(tempVideoPath, tempAudioPath);

                if (audioExtracted)
                {
                    // 2. Send Compressed Audio to Groq (Free & Fast)
                    string transcript = await _aiService.TranscribeAudioAsync(tempAudioPath);
                    
                    if (!string.IsNullOrEmpty(transcript))
                    {
                        // 3. Send Transcript to OpenRouter (Llama 3.3) for Summary
                        aiSummary = await _aiService.GenerateSummaryAsync(transcript);
                    }
                    else 
                    {
                        aiSummary = "Could not transcribe audio.";
                    }

                    // Cleanup Audio File
                    if (System.IO.File.Exists(tempAudioPath)) System.IO.File.Delete(tempAudioPath);
                }
                else
                {
                    aiSummary = "No audio track found.";
                }
               

                // Only generate a thumbnail if the user DIDN'T upload one
                if (ThumbnailFile == null)
                {
                    await _videoProcessor.GenerateThumbnailAsync(tempVideoPath, tempThumbPath);
                }

                //  Upload Video to Azure
                var vidContainer = _blobServiceClient.GetBlobContainerClient("videos");
                await vidContainer.CreateIfNotExistsAsync(PublicAccessType.Blob);
                var vidBlob = vidContainer.GetBlobClient($"{uniqueId}{Path.GetExtension(VideoFile.FileName)}");
                
                await vidBlob.UploadAsync(tempVideoPath, true);
                var videoUrl = vidBlob.Uri.ToString();

                //  Upload Thumbnail to Azure
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
                

                //Save Metadata to Database
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
                //  Cleanup
                if (System.IO.File.Exists(tempVideoPath)) System.IO.File.Delete(tempVideoPath);
                if (System.IO.File.Exists(tempThumbPath)) System.IO.File.Delete(tempThumbPath);
                if (System.IO.File.Exists(tempPreviewPath)) System.IO.File.Delete(tempPreviewPath);
                
            }
        }
    }
}