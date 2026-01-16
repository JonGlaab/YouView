using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using YouView.Data;
using YouView.Models;

namespace YouView.Pages
{
    public class WatchModel : PageModel
    {
        private readonly YouViewDbContext _context;
        private readonly UserManager<User> _userManager;

        public WatchModel(YouViewDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public Video Video { get; set; } = default!;
        public bool? UserLikeStatus { get; set; } // null = none, true = like, false = dislike
        public int LikeCount { get; set; }
        public int DislikeCount { get; set; }

        [BindProperty]
        [Required]
        public string NewCommentContent { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null || _context.Videos == null)
            {
                return NotFound();
            }

            var video = await _context.Videos
                .Include(v => v.User)           // Fetch the Uploader
                .Include(v => v.Comments)       // Fetch Comments
                .ThenInclude(c => c.User)   // Fetch the Comment Authors
                .FirstOrDefaultAsync(m => m.VideoId == id);

            if (video == null)
            {
                return NotFound();
            }

            Video = video;
            
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);

                var lastWatch = await _context.WatchHistories
                    .Where(h => h.UserId == userId && h.VideoId == video.VideoId)
                    .OrderByDescending(h => h.WatchedAt)
                    .FirstOrDefaultAsync();

                if (lastWatch == null || (DateTime.UtcNow - lastWatch.WatchedAt).TotalMinutes > 5)
                {
                    var historyEntry = new WatchHistory
                    {
                        UserId = userId,
                        VideoId = video.VideoId,
                        WatchedAt = DateTime.UtcNow
                    };

                    _context.WatchHistories.Add(historyEntry);
                    await _context.SaveChangesAsync();
                }
            }

            // Get Like/Dislike counts
            LikeCount = await _context.LikeDislikes.CountAsync(l => l.VideoId == id && l.IsLike);
            DislikeCount = await _context.LikeDislikes.CountAsync(l => l.VideoId == id && !l.IsLike);

            // Check if current user has liked/disliked
            if (User.Identity.IsAuthenticated)
            {
                var userId = _userManager.GetUserId(User);
                var existingInteraction = await _context.LikeDislikes
                    .FirstOrDefaultAsync(l => l.VideoId == id && l.UserId == userId);
                
                if (existingInteraction != null)
                {
                    UserLikeStatus = existingInteraction.IsLike;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAddCommentAsync(int id)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Challenge(); // Forces login
            }

            if (string.IsNullOrWhiteSpace(NewCommentContent))
            {
                return RedirectToPage(new { id });
            }

            var userId = _userManager.GetUserId(User);
            
            var comment = new Comment
            {
                VideoId = id,
                UserId = userId,
                Content = NewCommentContent,
                CreatedAt = DateTime.UtcNow
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostToggleLikeAsync(int id, bool isLike)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return new JsonResult(new { success = false, message = "Not logged in" });
            }

            var userId = _userManager.GetUserId(User);
            var existingInteraction = await _context.LikeDislikes
                .FirstOrDefaultAsync(l => l.VideoId == id && l.UserId == userId);

            if (existingInteraction != null)
            {
                if (existingInteraction.IsLike == isLike)
                {
                    // User clicked the same button again -> Remove interaction (Toggle off)
                    _context.LikeDislikes.Remove(existingInteraction);
                }
                else
                {
                    // User switched from Like to Dislike (or vice versa) -> Update it
                    existingInteraction.IsLike = isLike;
                }
            }
            else
            {
                // New interaction
                var newInteraction = new LikeDislike
                {
                    UserId = userId,
                    VideoId = id,
                    IsLike = isLike,
                    CreatedAt = DateTime.UtcNow
                };
                _context.LikeDislikes.Add(newInteraction);
            }

            await _context.SaveChangesAsync();

            // Return new counts
            var newLikeCount = await _context.LikeDislikes.CountAsync(l => l.VideoId == id && l.IsLike);
            var newDislikeCount = await _context.LikeDislikes.CountAsync(l => l.VideoId == id && !l.IsLike);
            
            // Determine new status for UI
            bool? newStatus = null;
            var updatedInteraction = await _context.LikeDislikes
                .FirstOrDefaultAsync(l => l.VideoId == id && l.UserId == userId);
            if (updatedInteraction != null) newStatus = updatedInteraction.IsLike;

            return new JsonResult(new { 
                success = true, 
                likes = newLikeCount, 
                dislikes = newDislikeCount, 
                userStatus = newStatus 
            });
        }
    }
}