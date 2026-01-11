using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YouView.Models
{
    public class Playlist
    {
        [Key]
        public int PlaylistId { get; set; }
        [ForeignKey("User")]
        public string UserId { get; set; }
        public User User { get; set; }
        [Required]
        [StringLength(255)]
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<PlaylistVideo> PlaylistVideos { get; set; }
    }
}