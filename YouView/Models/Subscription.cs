using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace YouView.Models
{
    public class Subscription
    {
        public int Id { get; set; }

        [Required]
        public string FollowerId { get; set; } 
        public User Follower { get; set; }

        [Required]
        public string CreatorId { get; set; } 
        public User Creator { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}