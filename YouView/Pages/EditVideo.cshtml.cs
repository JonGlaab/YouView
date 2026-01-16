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

            return RedirectToPage("./MyVideos");
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var userId = _userManager.GetUserId(User);
            var video = await _context.Videos
                .FirstOrDefaultAsync(v => v.VideoId == id && v.UserId == userId);

            if (video != null)
            {
                _context.Videos.Remove(video);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./MyVideos");
        }
    }
}