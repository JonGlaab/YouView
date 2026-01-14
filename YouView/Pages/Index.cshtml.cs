using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YouView.Models; 
using YouView.Data;

namespace YouView.Pages
{
    public class IndexModel : PageModel
    {
        private readonly YouViewDbContext _context; // CHANGE THIS to your actual DbContext name if different

        public IndexModel(YouViewDbContext context)
        {
            _context = context;
        }

        // We use the actual Video model now, not a ViewModel
        public IList<Video> RecentVideos { get; set; } = default!;

        public async Task OnGetAsync()
        {
            if (_context.Videos != null)
            {
                // Logic:
                // 1. Where(...): Only show Public videos (PrivacyStatus == 0)
                // 2. OrderByDescending(...): Show newest videos first
                // 3. Take(20): Limit to 20 so we don't crash the page if there are 1000s
                
                RecentVideos = await _context.Videos
                    .Include(v => v.User) 
                    .Where(v => v.PrivacyStatus == PrivacyStatus.Public)
                    .OrderByDescending(v => v.CreatedAt)
                    .Take(20)
                    .ToListAsync();
            }
        }
    }
}