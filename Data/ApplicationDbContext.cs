using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ADHUNIK_BARI.Models;


namespace ADHUNIK_BARI.Data
{

    public class ApplicationDbContext
        : IdentityDbContext<ApplicationUser>
    {

        public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {

        }


        // DbSets for application entities
        public DbSet<Flat> Flats { get; set; }
        public DbSet<FlatAssignment> FlatAssignments { get; set; }
        public DbSet<Notice> Notices { get; set; }
        public DbSet<NoticeTarget> NoticeTargets { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Flats and assignments
            builder.Entity<Flat>(b =>
            {
                b.HasKey(f => f.FlatId);
                b.Property(f => f.FlatNumber).IsRequired();
                b.Property(f => f.FlatStatus).HasMaxLength(50);
                b.HasMany(f => f.Assignments)
                    .WithOne(a => a.Flat)
                    .HasForeignKey(a => a.FlatId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<FlatAssignment>(b =>
            {
                b.HasKey(a => a.AssignmentId);
                b.HasOne(a => a.User)
                    .WithMany()
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.Property(a => a.ResidentType).HasMaxLength(50);
            });

            builder.Entity<Notice>(b =>
            {
                b.HasKey(n => n.NoticeId);
                b.Property(n => n.Title).IsRequired();
                b.Property(n => n.NoticeType).HasMaxLength(50).IsRequired();
                b.HasOne(n => n.CreatedBy)
                    .WithMany()
                    .HasForeignKey(n => n.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasMany(n => n.Targets)
                    .WithOne(t => t.Notice)
                    .HasForeignKey(t => t.NoticeId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<NoticeTarget>(b =>
            {
                b.HasKey(t => t.NoticeTargetId);
                b.HasOne(t => t.Flat)
                    .WithMany()
                    .HasForeignKey(t => t.FlatId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }

    }

}