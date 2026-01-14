using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YouView.Data;
using YouView.Models;

namespace YouView.Pages
{
    public class WatchModel : PageModel
    {
        private readonly YouViewDbContext _context;

        public WatchModel(YouViewDbContext context)
        {
            _context = context;
        }

        public Video Video { get; set; } = default!;

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
            return Page();
        }
    }
}