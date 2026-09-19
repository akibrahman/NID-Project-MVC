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
        }
    }
}