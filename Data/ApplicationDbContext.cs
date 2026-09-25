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
        public DbSet<ParkingSpot> ParkingSpots { get; set; }
        public DbSet<ParkingFloor> ParkingFloors { get; set; }
        public DbSet<ParkingActivityLog> ParkingActivityLogs { get; set; }
        public DbSet<Notice> Notices { get; set; }
        public DbSet<NoticeTarget> NoticeTargets { get; set; }
        public DbSet<Complaint> Complaints { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillItem> BillItems { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<PropertyListing> PropertyListings { get; set; }
        public DbSet<PropertyApplication> PropertyApplications { get; set; }
        public DbSet<ParkingApplication> ParkingApplications { get; set; }
        public DbSet<CctvCamera> CctvCameras { get; set; }
        public DbSet<CctvCameraFlatAccess> CctvCameraFlatAccesses { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Flats and assignments
            builder.Entity<Flat>(b =>
            {
                b.HasKey(f => f.FlatId);
                b.Property(f => f.FlatNumber).IsRequired();
                b.Property(f => f.MonthlyRent).HasPrecision(18, 2);
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

            // Parking Floor & Spots
            builder.Entity<ParkingFloor>(b =>
            {
                b.HasKey(pf => pf.ParkingFloorId);
                b.Property(pf => pf.FloorName).IsRequired().HasMaxLength(100);
                b.Property(pf => pf.FloorCode).IsRequired().HasMaxLength(20);
                b.HasMany(pf => pf.ParkingSpots)
                    .WithOne(ps => ps.Floor)
                    .HasForeignKey(ps => ps.ParkingFloorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<ParkingSpot>(b =>
            {
                b.HasKey(p => p.ParkingSpotId);
                b.Property(p => p.SpotNumber).IsRequired().HasMaxLength(50);
                b.Property(p => p.Status).IsRequired().HasMaxLength(50);
                b.Property(p => p.ParkingFee).HasPrecision(18, 2);
                b.Property(p => p.ListingPrice).HasPrecision(18, 2);
                b.HasIndex(p => p.SpotNumber);
                b.HasMany(p => p.ActivityLogs)
                    .WithOne(al => al.ParkingSpot)
                    .HasForeignKey(al => al.ParkingSpotId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(p => p.AssignedUser)
                    .WithMany()
                    .HasForeignKey(p => p.AssignedUserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            builder.Entity<ParkingActivityLog>(b =>
            {
                b.HasKey(al => al.ActivityId);
                b.Property(al => al.Action).IsRequired().HasMaxLength(100);
                b.Property(al => al.Details).IsRequired().HasMaxLength(500);
                b.Property(al => al.CreatedBy).HasMaxLength(150);
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
                b.HasMany(bill => bill.BillItems)
                    .WithOne(item => item.Bill)
                    .HasForeignKey(item => item.BillId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<BillItem>(b =>
            {
                b.HasKey(item => item.BillItemId);
                b.Property(item => item.ItemType).IsRequired().HasMaxLength(50);
                b.Property(item => item.Amount).HasPrecision(18, 2);
                b.Property(item => item.Description).HasMaxLength(255);
                b.Property(item => item.PaymentStatus).IsRequired().HasMaxLength(50);
            });

            builder.Entity<Payment>(b =>
            {
                b.HasKey(payment => payment.PaymentId);
                b.Property(payment => payment.Amount).HasPrecision(18, 2);
                b.Property(payment => payment.AmountPaid).HasPrecision(18, 2);
                b.Property(payment => payment.PaymentStatus).IsRequired().HasMaxLength(50);
                b.Property(payment => payment.StripePaymentIntentId).HasMaxLength(255);
                b.Property(payment => payment.StripeReceiptUrl).HasMaxLength(500);
                b.Property(payment => payment.PaidItemsJson).HasMaxLength(1000);
                b.HasOne(payment => payment.User)
                    .WithMany()
                    .HasForeignKey(payment => payment.UserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<PropertyListing>(b =>
            {
                b.HasKey(listing => listing.PropertyListingId);
                b.Property(listing => listing.ListingType).HasMaxLength(20).IsRequired();
                b.Property(listing => listing.Title).HasMaxLength(200).IsRequired();
                b.Property(listing => listing.ShortDescription).HasMaxLength(500).IsRequired();
                b.Property(listing => listing.Description).IsRequired();
                b.Property(listing => listing.Price).HasPrecision(18, 2);
                b.Property(listing => listing.AdvanceAmount).HasPrecision(18, 2);
                b.Property(listing => listing.AreaSqFt).HasPrecision(18, 2);
                b.Property(listing => listing.FurnishingStatus).HasMaxLength(50);
                b.Property(listing => listing.Facing).HasMaxLength(50);
                b.Property(listing => listing.Features).HasMaxLength(2000);
                b.Property(listing => listing.CoverImagePath).HasMaxLength(500);
                b.Property(listing => listing.RoomLayoutImagePath).HasMaxLength(500).IsRequired();
                b.Property(listing => listing.ListingStatus).HasMaxLength(30).IsRequired();
                b.Property(listing => listing.CreatedByUserId).IsRequired();

                b.HasOne(listing => listing.Flat)
                    .WithMany()
                    .HasForeignKey(listing => listing.FlatId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(listing => listing.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(listing => listing.CreatedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasMany(listing => listing.Applications)
                    .WithOne(application => application.PropertyListing)
                    .HasForeignKey(application => application.PropertyListingId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasIndex(listing => listing.FlatId);
                b.HasIndex(listing => listing.CreatedByUserId);
                b.HasIndex(listing => new { listing.ListingStatus, listing.ListingType });
                b.HasIndex(listing => listing.FlatId)
                    .IsUnique()
                    .HasFilter("[ListingStatus] <> 'Draft' AND [ListingStatus] <> 'Closed' AND [ListingStatus] <> 'Archived'");
            });

            builder.Entity<PropertyApplication>(b =>
            {
                b.HasKey(application => application.PropertyApplicationId);
                b.Property(application => application.FullName).HasMaxLength(200).IsRequired();
                b.Property(application => application.Email).HasMaxLength(256).IsRequired();
                b.Property(application => application.Phone).HasMaxLength(50).IsRequired();
                b.Property(application => application.CurrentAddress).HasMaxLength(1000).IsRequired();
                b.Property(application => application.Profession).HasMaxLength(150);
                b.Property(application => application.Message).HasMaxLength(2000);
                b.Property(application => application.ApplicationType).HasMaxLength(20).IsRequired();
                b.Property(application => application.Status).HasMaxLength(40).IsRequired();
                b.Property(application => application.AdvanceAmount).HasPrecision(18, 2);
                b.Property(application => application.StripePaymentIntentId).HasMaxLength(255);
                b.Property(application => application.PaymentStatus).HasMaxLength(30).IsRequired();
                b.Property(application => application.CreatedResidentUserId).HasMaxLength(450);
                b.Property(application => application.FailureReason).HasMaxLength(2000);

                b.HasOne(application => application.PropertyListing)
                    .WithMany(listing => listing.Applications)
                    .HasForeignKey(application => application.PropertyListingId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(application => application.CreatedResidentUser)
                    .WithMany()
                    .HasForeignKey(application => application.CreatedResidentUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasIndex(application => application.PropertyListingId);
                b.HasIndex(application => new { application.Status, application.PaymentStatus });
                b.HasIndex(application => application.CreatedResidentUserId);
                b.HasIndex(application => application.StripePaymentIntentId)
                    .IsUnique()
                    .HasFilter("[StripePaymentIntentId] IS NOT NULL");
            });

            builder.Entity<CctvCamera>(b =>
            {
                b.ToTable("CctvCameras");
                b.HasKey(c => c.CameraId);
                b.Property(c => c.CameraName).IsRequired().HasMaxLength(100);
                b.Property(c => c.Location).IsRequired().HasMaxLength(100);
                b.Property(c => c.StreamUrl).IsRequired().HasMaxLength(1000);
                b.Property(c => c.Status).HasMaxLength(50);
                b.Property(c => c.AccessType).HasMaxLength(50).HasDefaultValue("All");
            });

            builder.Entity<CctvCameraFlatAccess>(b =>
            {
                b.ToTable("CctvCameraFlatAccesses");
                b.HasKey(a => a.Id);
                b.HasOne(a => a.Camera)
                    .WithMany(c => c.FlatAccesses)
                    .HasForeignKey(a => a.CameraId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasOne(a => a.Flat)
                    .WithMany()
                    .HasForeignKey(a => a.FlatId)
                    .OnDelete(DeleteBehavior.Cascade);
                b.HasIndex(a => new { a.CameraId, a.FlatId }).IsUnique();
            });

            builder.Entity<ParkingApplication>(b =>
            {
                b.HasKey(a => a.ParkingApplicationId);
                b.Property(a => a.FullName).IsRequired().HasMaxLength(200);
                b.Property(a => a.Email).IsRequired().HasMaxLength(256);
                b.Property(a => a.Phone).IsRequired().HasMaxLength(50);
                b.Property(a => a.VehicleType).HasMaxLength(50);
                b.Property(a => a.VehicleRegNumber).HasMaxLength(50);
                b.Property(a => a.Notes).HasMaxLength(1000);
                b.Property(a => a.ApplicationType).IsRequired().HasMaxLength(20);
                b.Property(a => a.Status).IsRequired().HasMaxLength(40);
                b.Property(a => a.AdvanceAmount).HasPrecision(18, 2);
                b.Property(a => a.StripePaymentIntentId).HasMaxLength(255);
                b.Property(a => a.PaymentStatus).IsRequired().HasMaxLength(30);
                b.Property(a => a.FailureReason).HasMaxLength(2000);

                b.HasOne(a => a.ParkingSpot)
                    .WithMany()
                    .HasForeignKey(a => a.ParkingSpotId)
                    .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(a => a.Flat)
                    .WithMany()
                    .HasForeignKey(a => a.FlatId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasOne(a => a.CreatedUser)
                    .WithMany()
                    .HasForeignKey(a => a.CreatedUserId)
                    .OnDelete(DeleteBehavior.SetNull);

                b.HasIndex(a => a.ParkingSpotId);
                b.HasIndex(a => new { a.Status, a.PaymentStatus });
                b.HasIndex(a => a.CreatedUserId);
                b.HasIndex(a => a.StripePaymentIntentId)
                    .IsUnique()
                    .HasFilter("[StripePaymentIntentId] IS NOT NULL");
            });

        }

    }

}
