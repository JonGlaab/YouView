using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YouView.Data;
using YouView.Models;
using System.Security.Claims;

namespace YouView.Pages
{
    public class LikedVideos : PageModel
    {
        private readonly YouViewDbContext _context;

        public LikedVideos(YouViewDbContext context)
        {
            _context = context;
        }

        public IList<LikeDislike> Likes { get; set; } = new List<LikeDislike>();
        public int? NextCursor { get; set; }

        public async Task OnGetAsync(int? cursor)
        {
            int pageSize = 20;
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            IQueryable<LikeDislike> baseQuery = _context.LikeDislikes
                .Include(l => l.Video)
                .ThenInclude(v => v.User)
                .Where(l => l.UserId == userId && l.IsLike && l.VideoId != null)
                .OrderByDescending(l => l.LikeDislikeId);

            if (cursor.HasValue)
            {
                baseQuery = baseQuery.Where(l => l.LikeDislikeId < cursor.Value);
            }

            var items = await baseQuery.Take(pageSize + 1).ToListAsync();

            if (items.Count > pageSize)
            {
                NextCursor = items[pageSize - 1].LikeDislikeId; 
                items.RemoveAt(pageSize); 
            }

            Likes = items;
        }
    }
}
