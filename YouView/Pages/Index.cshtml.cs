using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; // Needed for User check
using YouView.Data;
using YouView.Models;

namespace YouView.Pages
{
    public class IndexModel : PageModel
    {
        private readonly YouViewDbContext _context;
        private readonly UserManager<User> _userManager;

        public IndexModel(YouViewDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public IList<Video> RecentVideos { get; set; } = default!;
        public int? NextCursor { get; set; }
        
        public List<Video> ShelfVideos { get; set; } = new List<Video>();
        public string ShelfTitle { get; set; } = "";
        public bool ShowShelf { get; set; } = false;


        public async Task OnGetAsync(int? cursor)
        {
           
            //THE SHELF (Only run on the first page: cursor == null)
           
            if (cursor == null)
            {
                bool hasSubscriptionContent = false;

                if (User.Identity.IsAuthenticated)
                {
                    var userId = _userManager.GetUserId(User);

                    // A. Get Subscribed Creator IDs
                    var subCreatorIds = await _context.Subscriptions
                        .Where(s => s.FollowerId == userId)
                        .Select(s => s.CreatorId)
                        .ToListAsync();

                    if (subCreatorIds.Any())
                    {
                        // B. Get IDs of videos ALREADY watched
                        var watchedVideoIds = await _context.WatchHistories
                            .Where(wh => wh.UserId == userId)
                            .Select(wh => wh.VideoId)
                            .ToListAsync();

                        // C. Fetch candidate videos (from subs, unwatched)
                        var candidateVideos = await _context.Videos
                            .Include(v => v.User)
                            .Where(v => subCreatorIds.Contains(v.UserId))
                            .Where(v => !watchedVideoIds.Contains(v.VideoId)) 
                            .Where(v => v.PrivacyStatus == PrivacyStatus.Public) 
                            .OrderByDescending(v => v.VideoId)
                            .Take(30)
                            .ToListAsync();

                        if (candidateVideos.Any())
                        {
                            // D. "One Video Per Creator" Logic
                            var finalSelection = new List<Video>();
                            var usedCreators = new HashSet<string>();

                            // Pass 1: One per creator
                            foreach (var vid in candidateVideos)
                            {
                                if (finalSelection.Count >= 10) break;
                                if (!usedCreators.Contains(vid.UserId))
                                {
                                    finalSelection.Add(vid);
                                    usedCreators.Add(vid.UserId);
                                }
                            }
                            // Pass 2: Fill rest
                            if (finalSelection.Count < 10)
                            {
                                foreach (var vid in candidateVideos)
                                {
                                    if (finalSelection.Count >= 10) break;
                                    if (!finalSelection.Contains(vid)) finalSelection.Add(vid);
                                }
                            }

                            ShelfVideos = finalSelection;
                            ShelfTitle = "Fresh from your Creators";
                            ShowShelf = true;
                            hasSubscriptionContent = true;
                        }
                    }
                }

                
                if (!hasSubscriptionContent)
                {
                    ShelfVideos = await _context.Videos
                        .Include(v => v.User)
                        .Include(v => v.Comments)
                        .Where(v => v.PrivacyStatus == PrivacyStatus.Public)
                        .OrderByDescending(v => v.Comments.Count) // Trending by comments
                        .Take(10)
                        .ToListAsync();

                    ShelfTitle = "Trending Now";
                    ShowShelf = true;
                }
            }

            
            int pageSize = 20;

            var query = _context.Videos
                .Include(v => v.User)
                .Where(v => v.PrivacyStatus == PrivacyStatus.Public)
                .OrderByDescending(v => v.VideoId);

            if (cursor.HasValue)
            {
                query = (IOrderedQueryable<Video>)query.Where(v => v.VideoId < cursor.Value);
            }

            // Fetch 21 items to detect if there is a next page
            var fetchedVideos = await query.Take(pageSize + 1).ToListAsync();

            if (fetchedVideos.Count > pageSize)
            {
                NextCursor = fetchedVideos[pageSize - 1].VideoId;
                fetchedVideos.RemoveAt(pageSize);
            }

            RecentVideos = fetchedVideos;
        }
    }
}