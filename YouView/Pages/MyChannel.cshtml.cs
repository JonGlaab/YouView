using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YouView.Data;
using YouView.Models;

namespace YouView.Pages
{
    [Authorize]
    public class MyVideosModel : PageModel
    {
        private readonly YouViewDbContext _context;
        private readonly UserManager<User> _userManager;

        public MyVideosModel(YouViewDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<Video> Videos { get; set; } = new();
        public List<Playlist> Playlists { get; set; } = new();
        
        // Pagination
        public int? NextCursor { get; set; }
        public bool HasMoreVideos { get; set; }

        public async Task OnGetAsync(int? cursor)
        {
            var userId = _userManager.GetUserId(User);
            int pageSize = 12; // Show 12 videos per page (3 rows of 4)

            // Load Videos with Pagination
            var query = _context.Videos
                .Where(v => v.UserId == userId)
                .OrderByDescending(v => v.VideoId); // Use ID for cursor pagination

            if (cursor.HasValue)
            {
                query = (IOrderedQueryable<Video>)query.Where(v => v.VideoId < cursor.Value);
            }

            var fetchedVideos = await query.Take(pageSize + 1).ToListAsync();

            if (fetchedVideos.Count > pageSize)
            {
                NextCursor = fetchedVideos[pageSize - 1].VideoId;
                HasMoreVideos = true;
                fetchedVideos.RemoveAt(pageSize);
            }
            else
            {
                HasMoreVideos = false;
            }

            Videos = fetchedVideos;

            // Load Playlists (No pagination for now, usually fewer playlists than videos)
            Playlists = await _context.Playlists
                .Where(p => p.UserId == userId)
                .Include(p => p.PlaylistVideos)
                .ThenInclude(pv => pv.Video)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        public async Task<IActionResult> OnGetUserPlaylistsAsync(int videoId)
        {
            var userId = _userManager.GetUserId(User);
            
            var playlists = await _context.Playlists
                .Where(p => p.UserId == userId)
                .Select(p => new 
                {
                    p.PlaylistId,
                    Title = p.Name, 
                    ContainsVideo = p.PlaylistVideos.Any(pv => pv.VideoId == videoId)
                })
                .ToListAsync();

            return new JsonResult(playlists);
        }
        
        public async Task<IActionResult> OnGetPlaylistDetailsAsync(int playlistId)
        {
            var userId = _userManager.GetUserId(User);
            
            var playlist = await _context.Playlists
                .Include(p => p.PlaylistVideos)
                .ThenInclude(pv => pv.Video)
                .ThenInclude(v => v.User)
                .FirstOrDefaultAsync(p => p.PlaylistId == playlistId && p.UserId == userId);

            if (playlist == null) return NotFound();

            var videos = playlist.PlaylistVideos
                .Select(pv => new 
                {
                    pv.Video.VideoId,
                    pv.Video.Title,
                    pv.Video.ThumbnailUrl,
                    pv.Video.Duration,
                    Author = pv.Video.User.UserName,
                    AddedAt = pv.AddedAt.ToString("MMM dd, yyyy")
                })
                .ToList();

            return new JsonResult(new { title = playlist.Name, videos = videos });
        }

        public async Task<IActionResult> OnPostCreatePlaylistAsync([FromBody] CreatePlaylistRequest request)
        {
            var userId = _userManager.GetUserId(User);
            
            var playlist = new Playlist
            {
                UserId = userId,
                Name = request.Title, 
                Description = "", 
                CreatedAt = DateTime.UtcNow
            };

            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true, playlistId = playlist.PlaylistId });
        }
        
        // For the "Create Playlist" button on the Playlists tab
        public async Task<IActionResult> OnPostCreateEmptyPlaylistAsync([FromBody] CreatePlaylistRequest request)
        {
            var userId = _userManager.GetUserId(User);
            
            var playlist = new Playlist
            {
                UserId = userId,
                Name = request.Title, 
                Description = "", 
                CreatedAt = DateTime.UtcNow
            };

            _context.Playlists.Add(playlist);
            await _context.SaveChangesAsync();

            // Return success so frontend can reload
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostTogglePlaylistVideoAsync([FromBody] TogglePlaylistRequest request)
        {
            var userId = _userManager.GetUserId(User);
            
            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.PlaylistId == request.PlaylistId && p.UserId == userId);

            if (playlist == null) return NotFound();

            var existingLink = await _context.PlaylistVideos
                .FirstOrDefaultAsync(pv => pv.PlaylistId == request.PlaylistId && pv.VideoId == request.VideoId);

            if (existingLink != null)
            {
                _context.PlaylistVideos.Remove(existingLink);
            }
            else
            {
                _context.PlaylistVideos.Add(new PlaylistVideo 
                { 
                    PlaylistId = request.PlaylistId, 
                    VideoId = request.VideoId,
                    AddedAt = DateTime.UtcNow
                });
            }

            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }
        
        // New: Rename Playlist
        public async Task<IActionResult> OnPostRenamePlaylistAsync([FromBody] RenamePlaylistRequest request)
        {
            var userId = _userManager.GetUserId(User);
            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.PlaylistId == request.PlaylistId && p.UserId == userId);

            if (playlist == null) return NotFound();

            playlist.Name = request.NewName;
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        // New: Delete Playlist
        public async Task<IActionResult> OnPostDeletePlaylistAsync([FromBody] DeletePlaylistRequest request)
        {
            var userId = _userManager.GetUserId(User);
            var playlist = await _context.Playlists
                .Include(p => p.PlaylistVideos) // Include relations to delete them first
                .FirstOrDefaultAsync(p => p.PlaylistId == request.PlaylistId && p.UserId == userId);

            if (playlist == null) return NotFound();

            // Remove videos from playlist first 
            _context.PlaylistVideos.RemoveRange(playlist.PlaylistVideos);
            
            // Remove playlist
            _context.Playlists.Remove(playlist);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        // Remove Video from Playlist (from the View Modal)
        public async Task<IActionResult> OnPostRemoveVideoFromPlaylistAsync([FromBody] RemoveVideoRequest request)
        {
            var userId = _userManager.GetUserId(User);
            
            // Ensure user owns the playlist
            var playlist = await _context.Playlists
                .FirstOrDefaultAsync(p => p.PlaylistId == request.PlaylistId && p.UserId == userId);

            if (playlist == null) return NotFound();

            var link = await _context.PlaylistVideos
                .FirstOrDefaultAsync(pv => pv.PlaylistId == request.PlaylistId && pv.VideoId == request.VideoId);

            if (link != null)
            {
                _context.PlaylistVideos.Remove(link);
                await _context.SaveChangesAsync();
            }

            return new JsonResult(new { success = true });
        }

        public class CreatePlaylistRequest
        {
            public string Title { get; set; }
        }

        public class TogglePlaylistRequest
        {
            public int PlaylistId { get; set; }
            public int VideoId { get; set; }
        }
        
        public class RenamePlaylistRequest
        {
            public int PlaylistId { get; set; }
            public string NewName { get; set; }
        }

        public class DeletePlaylistRequest
        {
            public int PlaylistId { get; set; }
        }

        public class RemoveVideoRequest
        {
            public int PlaylistId { get; set; }
            public int VideoId { get; set; }
        }
    }
}