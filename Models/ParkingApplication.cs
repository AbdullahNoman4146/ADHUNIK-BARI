using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADHUNIK_BARI.Models
{
    public class ParkingApplication
    {
        [Key]
        public int ParkingApplicationId { get; set; }

        [Required]
        public int ParkingSpotId { get; set; }

        public int? FlatId { get; set; }

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? VehicleType { get; set; } = "Car";

        [MaxLength(50)]
        public string? VehicleRegNumber { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        [Required]
        [MaxLength(20)]
        public string ApplicationType { get; set; } = PropertyListingTypes.ToLet;

        [Required]
        [MaxLength(40)]
        public string Status { get; set; } = PropertyApplicationStatuses.PendingPayment;

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal AdvanceAmount { get; set; }

        [MaxLength(255)]
        public string? StripePaymentIntentId { get; set; }

        [Required]
        [MaxLength(30)]
        public string PaymentStatus { get; set; } = PropertyPaymentStatuses.Pending;

        public DateTime? PaidAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ReservationExpiresAt { get; set; }

        public string? CreatedUserId { get; set; }

        public bool IsOutsider { get; set; }

        public bool EmailSent { get; set; }

        public DateTime? EmailSentAt { get; set; }

        [MaxLength(2000)]
        public string? FailureReason { get; set; }

        [ForeignKey("ParkingSpotId")]
        public ParkingSpot? ParkingSpot { get; set; }

        [ForeignKey("FlatId")]
        public Flat? Flat { get; set; }

        [ForeignKey("CreatedUserId")]
        public ApplicationUser? CreatedUser { get; set; }
    }
}

