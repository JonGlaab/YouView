using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YouView.Models
{
    public class WatchHistory
    {
        [Key]
        public int HistoryId { get; set; } 
        [ForeignKey("User")]
        public string UserId { get; set; }
        public User User { get; set; }

        [ForeignKey("Video")]
        public int VideoId { get; set; }
        public Video Video { get; set; }

        public DateTime WatchedAt { get; set; } = DateTime.UtcNow;
    }
}