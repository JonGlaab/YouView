using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YouView.Data;
using YouView.Models;
using System.Security.Claims;

namespace YouView.Pages
{
    public class WatchHistoryModel : PageModel
    {
        private readonly YouViewDbContext _context;

        public WatchHistoryModel(YouViewDbContext context)
        {
            _context = context;
        }
        
        public IList<WatchHistory> History { get; set; } = new List<WatchHistory>();
        public int? NextCursor { get; set; }
        
        public async Task OnGetAsync(int? cursor)
        {
            int pageSize = 20;
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;


            IQueryable<WatchHistory> baseQuery = _context.WatchHistories
                .Include(h => h.Video)
                .ThenInclude(v => v.User)
                .Where(h => h.UserId == userId)
                .OrderByDescending(h => h.HistoryId);

            if (cursor.HasValue)
            {
                baseQuery = baseQuery.Where(h => h.HistoryId < cursor.Value);
            }

            var items = await baseQuery.Take(pageSize + 1).ToListAsync();

            if (items.Count > pageSize)
            {
                NextCursor = items[pageSize - 1].HistoryId;
                items.RemoveAt(pageSize);
            }

            History = items;
        }
    }
}