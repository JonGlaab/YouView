using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YouView.Data;
using YouView.Models;

namespace YouView.Pages
{
    public class SearchModel : PageModel
    {
        private readonly YouViewDbContext _context;

        public SearchModel(YouViewDbContext context)
        {
            _context = context;
        }

        public IList<Video> VideoResults { get; set; } = new List<Video>();
        public IList<User> CreatorResults { get; set; } = new List<User>(); // New List
        
        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Q))
            {
                return;
            }

            // 1. Search Videos (Title or Description)
            VideoResults = await _context.Videos
                .Include(v => v.User)
                .Where(v => v.Title.ToLower().Contains(Q.ToLower()) || v.Description.ToLower().Contains(Q.ToLower()))
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();

            // 2. Search Creators (Username)
            CreatorResults = await _context.Users
                .Where(u => u.UserName.ToLower().Contains(Q.ToLower()))
                .Take(20) // Limit to 20 creators to keep it clean
                .ToListAsync();
        }
    }
}