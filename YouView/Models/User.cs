using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Identity;

namespace YouView.Models;

public class User : IdentityUser
{
    public string ProfilePicUrl { get; set; }
    public string Bio { get; set;}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Video> Videos { get; set; }
    public ICollection<Playlist> Playlists { get; set; }
    public ICollection<Comment> Comments { get; set; }
    public ICollection<WatchHistory> WatchHistories { get; set; }
}