using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Identity;

namespace YouView.Models;

public class User : IdentityUser
{
	public string FirstName { get;set;}
	public string LastName {get;set;}
    public string ProfilePicUrl { get; set; }
    public string Bio { get; set;}
    public bool IsPremium { get; set;}
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Video> Videos { get; set; }
    public ICollection<Playlist> Playlists { get; set; }
    public ICollection<Comment> Comments { get; set; }
    public ICollection<WatchHistory> WatchHistories { get; set; }
    [InverseProperty("Creator")]
    public ICollection<Subscription> Followers { get; set; }
    [InverseProperty("Follower")]
    public ICollection<Subscription> Following { get; set; }
}