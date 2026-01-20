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
        public IList<User> CreatorResults { get; set; } = new List<User>();
        
        [BindProperty(SupportsGet = true)]
        public string Q { get; set; }

        public async Task OnGetAsync()
        {
            if (string.IsNullOrWhiteSpace(Q))
            {
                return;
            }
            
            var searchTerms = Q.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            var videoQuery = _context.Videos
                .Include(v => v.User)
                .AsQueryable();

            foreach (var term in searchTerms)
            {
                string t = term.ToLower(); 
                
                videoQuery = videoQuery.Where(v => 
                    v.Title.ToLower().Contains(t) || 
                    v.Description.ToLower().Contains(t));
            }

            VideoResults = await videoQuery
                .OrderByDescending(v => v.CreatedAt)
                .ToListAsync();
            
            var creatorQuery = _context.Users.AsQueryable();

            foreach (var term in searchTerms)
            {
                string t = term.ToLower();
                creatorQuery = creatorQuery.Where(u => u.UserName.ToLower().Contains(t));
            }

            CreatorResults = await creatorQuery
                .Take(20)
                .ToListAsync();
        }
    }
}