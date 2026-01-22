using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity; 
using Microsoft.Extensions.Caching.Memory; 
using YouView.Data;
using YouView.Models;

namespace YouView.Pages
{
    public class IndexModel : PageModel
    {
        private readonly YouViewDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IMemoryCache _cache; 
        
        public IndexModel(YouViewDbContext context, UserManager<User> userManager, IMemoryCache cache)
        {
            _context = context;
            _userManager = userManager;
            _cache = cache;
        }

        public IList<Video> RecentVideos { get; set; } = default!;
        public int? NextCursor { get; set; }
        
        public List<Video> ShelfVideos { get; set; } = new List<Video>();
        public string ShelfTitle { get; set; } = "";
        public bool ShowShelf { get; set; } = false;


        public async Task OnGetAsync(int? cursor)
        {
            if (cursor == null)
            {
                bool hasSubscriptionContent = false;

                // A. Personalized "Fresh" Shelf (Do NOT Cache - unique per user)
                if (User.Identity.IsAuthenticated)
                {
                    var userId = _userManager.GetUserId(User);

                    // Get Subscribed Creator IDs
                    var subCreatorIds = await _context.Subscriptions
                        .Where(s => s.FollowerId == userId)
                        .Select(s => s.CreatorId)
                        .ToListAsync();

                    if (subCreatorIds.Any())
                    {
                        // Get IDs of videos ALREADY watched
                        var watchedVideoIds = await _context.WatchHistories
                            .Where(wh => wh.UserId == userId)
                            .Select(wh => wh.VideoId)
                            .ToListAsync();

                        // Fetch candidate videos
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
                            // "One Video Per Creator" Logic
                            var finalSelection = new List<Video>();
                            var usedCreators = new HashSet<string>();

                            foreach (var vid in candidateVideos)
                            {
                                if (finalSelection.Count >= 10) break;
                                if (!usedCreators.Contains(vid.UserId))
                                {
                                    finalSelection.Add(vid);
                                    usedCreators.Add(vid.UserId);
                                }
                            }
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

                // Fallback: "Trending Now" 
                if (!hasSubscriptionContent)
                {
                    // Check Cache for "trending_shelf"
                    if (!_cache.TryGetValue("trending_shelf", out List<Video> cachedTrending))
                    {
                        // Not in cache? Fetch from DB
                        cachedTrending = await _context.Videos
                            .Include(v => v.User)
                            .Include(v => v.Comments)
                            .Where(v => v.PrivacyStatus == PrivacyStatus.Public)
                            .OrderByDescending(v => v.Comments.Count) // Trending by comments
                            .Take(10)
                            .ToListAsync();

                        // Save to cache for 15 minutes
                        var cacheOptions = new MemoryCacheEntryOptions()
                            .SetAbsoluteExpiration(TimeSpan.FromMinutes(15));

                        _cache.Set("trending_shelf", cachedTrending, cacheOptions);
                    }

                    ShelfVideos = cachedTrending;
                    ShelfTitle = "Trending Now";
                    ShowShelf = true;
                }
            }
            
            // Create a unique cache key based on the page cursor
            string cacheKey = $"recent_videos_{cursor ?? 0}";

            if (!_cache.TryGetValue(cacheKey, out List<Video> cachedRecent))
            {
                int pageSize = 12; 

                var query = _context.Videos
                    .Include(v => v.User)
                    .Where(v => v.PrivacyStatus == PrivacyStatus.Public)
                    .OrderByDescending(v => v.VideoId);

                if (cursor.HasValue)
                {
                    query = (IOrderedQueryable<Video>)query.Where(v => v.VideoId < cursor.Value);
                }

                // Fetch 13 items (12 + 1 to detect next page)
                cachedRecent = await query.Take(pageSize + 1).ToListAsync();

                // Save to cache for 1 minute
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetAbsoluteExpiration(TimeSpan.FromMinutes(1));

                _cache.Set(cacheKey, cachedRecent, cacheOptions);
            }

            // Process the list 
            var videoList = new List<Video>(cachedRecent);
            int displaySize = 12;

            if (videoList.Count > displaySize)
            {
                NextCursor = videoList[displaySize - 1].VideoId;
                videoList.RemoveAt(displaySize);
            }

            RecentVideos = videoList;
        }
    }
}