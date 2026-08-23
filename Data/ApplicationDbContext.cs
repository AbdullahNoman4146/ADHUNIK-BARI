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
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<Payment> Payments { get; set; }

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

            builder.Entity<Complaint>(b =>
            {
                b.HasKey(c => c.ComplaintId);
                b.Property(c => c.Category).IsRequired().HasMaxLength(100);
                b.Property(c => c.ComplaintStatus).IsRequired().HasMaxLength(50);
                b.Property(c => c.Description).IsRequired();
                b.HasOne(c => c.Flat)
                    .WithMany()
                    .HasForeignKey(c => c.FlatId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(c => c.User)
                    .WithMany()
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasOne(c => c.ResolvedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.ResolvedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Bill>(b =>
            {
                b.HasKey(bill => bill.BillId);
                b.Property(bill => bill.TotalAmount).HasPrecision(18, 2);
                b.Property(bill => bill.PaidAmount).HasPrecision(18, 2);
                b.Property(bill => bill.DueAmount).HasPrecision(18, 2);
                b.Property(bill => bill.BillStatus).IsRequired().HasMaxLength(50);
                b.HasOne(bill => bill.Assignment)
                    .WithMany()
                    .HasForeignKey(bill => bill.AssignmentId)
                    .OnDelete(DeleteBehavior.Restrict);
                b.HasMany(bill => bill.Payments)
                    .WithOne(payment => payment.Bill)
                    .HasForeignKey(payment => payment.BillId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Payment>(b =>
            {
                b.HasKey(payment => payment.PaymentId);
                b.Property(payment => payment.Amount).HasPrecision(18, 2);
                b.Property(payment => payment.PaymentStatus).IsRequired().HasMaxLength(50);
                b.HasOne(payment => payment.User)
                    .WithMany()
                    .HasForeignKey(payment => payment.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

        }

    }

}