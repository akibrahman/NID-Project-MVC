using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NID_Project.Models;

namespace NID_Project.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<EditApplication> EditApplications { get; set; }
        public DbSet<Voting> Votings { get; set; }
        public DbSet<Nominee> Nominees { get; set; }
        public DbSet<UserBallot> UserBallots { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<EditApplication>()
                .HasOne(e => e.User).WithMany()
                .HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<EditApplication>()
                .HasOne(e => e.ReviewedByModerator).WithMany()
                .HasForeignKey(e => e.ReviewedByModeratorId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<EditApplication>()
                .HasOne(e => e.ChangeReviewedByModerator).WithMany()
                .HasForeignKey(e => e.ChangeReviewedByModeratorId).OnDelete(DeleteBehavior.Restrict);

            // Voting relationships
            builder.Entity<Nominee>()
                .HasOne(n => n.Voting).WithMany(v => v.Nominees)
                .HasForeignKey(n => n.VotingId).OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserBallot>()
                .HasOne(b => b.Voting).WithMany(v => v.Ballots)
                .HasForeignKey(b => b.VotingId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserBallot>()
                .HasOne(b => b.Nominee).WithMany(n => n.Ballots)
                .HasForeignKey(b => b.NomineeId).OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserBallot>()
                .HasOne(b => b.User).WithMany()
                .HasForeignKey(b => b.UserId).OnDelete(DeleteBehavior.Restrict);

            // One vote per user per voting
            builder.Entity<UserBallot>()
                .HasIndex(b => new { b.VotingId, b.UserId }).IsUnique();
        }
    }
}