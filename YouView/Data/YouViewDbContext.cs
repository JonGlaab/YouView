using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using YouView.Models;

namespace YouView.Data
{
    
    public class YouViewDbContext : IdentityDbContext<User>
    {
        public YouViewDbContext(DbContextOptions<YouViewDbContext> options)
            : base(options)
        {
        }

        public DbSet<Video> Videos { get; set; }
        public DbSet<Playlist> Playlists { get; set; }
        public DbSet<PlaylistVideo> PlaylistVideos { get; set; }
        public DbSet<Comment> Comments { get; set; }
        public DbSet<WatchHistory> WatchHistories { get; set; }
        public DbSet<LikeDislike> LikeDislikes { get; set; }
        
        public DbSet<Subscription> Subscriptions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // config for subscription ( no duplicate follows)
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Follower)
                .WithMany(u => u.Following)
                .HasForeignKey(s => s.FollowerId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Subscription>()
                .HasOne(s => s.Creator)
                .WithMany(u => u.Followers)
                .HasForeignKey(s => s.CreatorId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PlaylistVideo>()
                .HasKey(pv => new { pv.PlaylistId, pv.VideoId });
            //remove playlist
            modelBuilder.Entity<PlaylistVideo>()
                .HasOne(pv => pv.Video)
                .WithMany(pv => pv.PlaylistVideos)
                .HasForeignKey(pv => pv.VideoId)
                .OnDelete(DeleteBehavior.Restrict);
            //User deletion
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.User)
                .WithMany(c => c.Comments)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            //Comment threads deletion
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(c => c.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);
            //Video deletion
            modelBuilder.Entity<Comment>()
                .HasOne(c => c.Video)
                .WithMany(c => c.Comments)
                .HasForeignKey(c => c.VideoId)
                .OnDelete(DeleteBehavior.Restrict);
            //Watch history
            modelBuilder.Entity<WatchHistory>()
                .HasOne(wh => wh.Video)
                .WithMany(wh => wh.WatchHistories)
                .HasForeignKey(wh => wh.VideoId)
                .OnDelete(DeleteBehavior.Restrict);
            //Likes/Dislikes
            modelBuilder.Entity<LikeDislike>()
                .HasOne(ld => ld.Video)
                .WithMany() 
                .HasForeignKey(ld => ld.VideoId)
                .OnDelete(DeleteBehavior.Restrict);
            
        }
    }
}