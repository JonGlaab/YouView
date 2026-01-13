using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YouView.Models;

public enum PrivacyStatus
{
    Public=0,
    Private=1,
    Unlisted=2
}
public class Video
{
    [Key]
    public int VideoId { get; set; }
    [ForeignKey("User")]
    public string UserId { get; set; } 
    public User User { get; set; }
    [Required]
    [StringLength(255)]
    public string Title { get; set; }
    public string Description { get; set; }
    [Required]
    public string VideoUrl { get; set; }
	public string Duration { get;set}
    public string ThumbnailUrl { get; set; }
    public PrivacyStatus PrivacyStatus { get; set; } = PrivacyStatus.Public;
    public string SubtitlesUrl { get; set; }
    public string AiSummary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Comment> Comments { get; set; }
    public ICollection<PlaylistVideo> PlaylistVideos { get; set; }
    public ICollection<WatchHistory> WatchHistories { get; set; }
    
}