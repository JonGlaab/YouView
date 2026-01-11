using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YouView.Models
{
    public class LikeDislike
    {
        [Key]
        public int LikeDislikeId { get; set; }
        [ForeignKey("User")]
        public string UserId { get; set; }
        public User User { get; set; }
        [ForeignKey("Video")]
        public int? VideoId { get; set; }
        public Video Video { get; set; }
        [ForeignKey("Comment")]
        public int? CommentId { get; set; }
        public Comment Comment { get; set; }
        public bool IsLike { get; set; } // True= LIke, False= Dislike
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}