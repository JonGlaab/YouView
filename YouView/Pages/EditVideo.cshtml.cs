using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YouView.Data;
using YouView.Models;
using YouView.Services;

namespace YouView.Pages
{
    [Authorize]
    public class EditVideoModel : PageModel
    {
        private readonly YouViewDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly BlobService _blobService;

        public EditVideoModel(YouViewDbContext context, UserManager<User> userManager, BlobService blobService)
        {
            _context = context;
            _userManager = userManager;
            _blobService = blobService;
        }

        [BindProperty]
        public Video Video { get; set; }

        [BindProperty]
        public IFormFile? NewThumbnail { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = _userManager.GetUserId(User);
            
            Video = await _context.Videos
                .FirstOrDefaultAsync(v => v.VideoId == id && v.UserId == userId);

            if (Video == null)
            {
                return NotFound();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            ModelState.Remove("NewThumbnail");
            ModelState.Remove("Video.User");
            ModelState.Remove("Video.UserId");
            ModelState.Remove("Video.VideoUrl");
            ModelState.Remove("Video.Duration");
            ModelState.Remove("Video.ThumbnailUrl");
            ModelState.Remove("Video.PreviewUrl");
            ModelState.Remove("Video.SubtitlesUrl");
            ModelState.Remove("Video.AiSummary");
            ModelState.Remove("Video.Comments");
            ModelState.Remove("Video.PlaylistVideos");
            ModelState.Remove("Video.WatchHistories");

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var userId = _userManager.GetUserId(User);
            var videoToUpdate = await _context.Videos
                .FirstOrDefaultAsync(v => v.VideoId == Video.VideoId && v.UserId == userId);

            if (videoToUpdate == null)
            {
                return NotFound();
            }

            // Update allowed fields
            videoToUpdate.Title = Video.Title;
            videoToUpdate.Description = Video.Description;
            videoToUpdate.PrivacyStatus = Video.PrivacyStatus;
            videoToUpdate.UpdatedAt = DateTime.UtcNow;

            // Handle Thumbnail Upload
            if (NewThumbnail != null)
            {
                // Upload new thumbnail
                var newThumbnailUrl = await _blobService.UploadFileAsync(NewThumbnail);
                
                // Delete old thumbnail if it exists
                await _blobService.DeleteFileAsync(videoToUpdate.ThumbnailUrl);

                videoToUpdate.ThumbnailUrl = newThumbnailUrl;
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("./MyChannel");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var userId = _userManager.GetUserId(User);
            var video = await _context.Videos
                .Include(v => v.Comments)
                .Include(v => v.PlaylistVideos)
                .Include(v => v.WatchHistories)
                .FirstOrDefaultAsync(v => v.VideoId == id && v.UserId == userId);

            if (video != null)
            {
                // Remove related entities manually
                if (video.Comments != null) _context.Comments.RemoveRange(video.Comments);
                if (video.PlaylistVideos != null) _context.PlaylistVideos.RemoveRange(video.PlaylistVideos);
                if (video.WatchHistories != null) _context.WatchHistories.RemoveRange(video.WatchHistories);
                
                // Remove Likes/Dislikes (Need to query separately as they are not in the Video navigation property in this context)
                var likesOrDislikes = _context.LikeDislikes.Where(l => l.VideoId == id);
                _context.LikeDislikes.RemoveRange(likesOrDislikes);

                // Delete files from Azure Storage (Optional but recommended)
                await _blobService.DeleteFileAsync(video.VideoUrl);
                await _blobService.DeleteFileAsync(video.ThumbnailUrl);
                if (!string.IsNullOrEmpty(video.PreviewUrl)) await _blobService.DeleteFileAsync(video.PreviewUrl);

                // Remove the video itself
                _context.Videos.Remove(video);
                
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./MyChannel");
        }
    }
}