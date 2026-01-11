using System.ComponentModel.DataAnnotations.Schema;
namespace YouView.Models;

public class PlaylistVideo
{
    [ForeignKey("Playlist")]
    public int PlaylistId { get; set; }
    public Playlist Playlist { get; set; }

    [ForeignKey("Video")]
    public int VideoId { get; set; }
    public Video Video { get; set; }

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}