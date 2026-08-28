using System.ComponentModel.DataAnnotations;

namespace ADHUNIK_BARI.Models
{
    public class PropertyApplication
    {
        [Key]
        public int PropertyApplicationId { get; set; }

        [Required]
        public int PropertyListingId { get; set; }

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

        [Required]
        [MaxLength(1000)]
        public string CurrentAddress { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Profession { get; set; }

        [Range(1, int.MaxValue)]
        public int? NumberOfOccupants { get; set; }

        public DateTime? ExpectedMoveInDate { get; set; }

        [MaxLength(2000)]
        public string? Message { get; set; }

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

        public string? CreatedResidentUserId { get; set; }

        public bool EmailSent { get; set; }

        public DateTime? EmailSentAt { get; set; }

        [MaxLength(2000)]
        public string? FailureReason { get; set; }

        public PropertyListing? PropertyListing { get; set; }

        public ApplicationUser? CreatedResidentUser { get; set; }
    }
}