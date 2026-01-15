using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YouView.Data;
using YouView.Models;

namespace YouView.Pages
{
    public class IndexModel : PageModel
    {
        private readonly YouViewDbContext _context;

        public IndexModel(YouViewDbContext context)
        {
            _context = context;
        }

        public IList<Video> RecentVideos { get; set; } = default!;
        
        //holds the ID for the Next Page button
        public int? NextCursor { get; set; }
        
        // If it's null, it means it's on the first page.
        public async Task OnGetAsync(int? cursor)
        {
            int pageSize = 20;

            // Base Query: Public videos, sorted by Newest (Descending ID)
            var query = _context.Videos
                .Include(v => v.User)
                .Where(v => v.PrivacyStatus == PrivacyStatus.Public)
                .OrderByDescending(v => v.VideoId); // Use ID for speed. It matches CreatedAt order.

            // Apply Cursor: If user clicked "Next", only get videos OLDER (smaller ID) than the cursor
            if (cursor.HasValue)
            {
                query = (IOrderedQueryable<Video>)query.Where(v => v.VideoId < cursor.Value);
            }

            // Fetch 21 items 
            var fetchedVideos = await query.Take(pageSize + 1).ToListAsync();

            // Determine if need a "Next" button
            if (fetchedVideos.Count > pageSize)
            {
                // The 20th video's ID becomes the cursor for the NEXT page
                NextCursor = fetchedVideos[pageSize - 1].VideoId;
                
                // Remove the 21st item so don't display it yet
                fetchedVideos.RemoveAt(pageSize);
            }

            RecentVideos = fetchedVideos;
        }
    }
}