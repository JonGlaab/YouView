using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YouView.Models
{
    public class Comment
    {
        [Key]
        public int CommentId { get; set; }
        [ForeignKey("Video")]
        public int VideoId { get; set; }
        public Video Video { get; set; }
        [ForeignKey("User")]
        public string UserId { get; set; }
        public User User { get; set; }
        public int? ParentCommentId { get; set; }
        public Comment ParentComment { get; set; }
        public ICollection<Comment> Replies { get; set; }
        [Required]
        public string Content { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}