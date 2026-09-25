using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADHUNIK_BARI.Models
{
    public static class GymMembershipStatuses
    {
        public const string Pending = "Pending";
        public const string Active = "Active";
        public const string Rejected = "Rejected";
        public const string Expired = "Expired";
        public const string Cancelled = "Cancelled";
    }

    public class GymMembership
    {
        [Key]
        public int GymMembershipId { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }

        public int? FlatId { get; set; }

        [ForeignKey("FlatId")]
        public Flat? Flat { get; set; }

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = GymMembershipStatuses.Pending; // Pending, Active, Rejected, Expired, Cancelled

        [Range(0, 100000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyFee { get; set; }

        public int DurationMonths { get; set; } = 1;

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public DateTime? ApprovedAt { get; set; }

        [MaxLength(450)]
        public string? ApprovedByUserId { get; set; }

        [ForeignKey("ApprovedByUserId")]
        public ApplicationUser? ApprovedByUser { get; set; }

        public DateTime? RejectedAt { get; set; }

        [MaxLength(500)]
        public string? RejectionReason { get; set; }

        public DateTime? CancellationRequestedAt { get; set; }

        public DateTime? CancellationEffectiveDate { get; set; }

        [MaxLength(500)]
        public string? CancellationReason { get; set; }

        [MaxLength(50)]
        public string RenewalStatus { get; set; } = "None"; // None, RenewalPending, Renewed

        public DateTime? RenewalRequestedAt { get; set; }

        [MaxLength(1000)]
        public string? Notes { get; set; }

        public bool IsFeePaid { get; set; } = false;

        public DateTime? FeePaidAt { get; set; }

        [MaxLength(50)]
        public string? CardNumber { get; set; }

        [MaxLength(150)]
        public string? MemberName { get; set; }

        [MaxLength(50)]
        public string? MemberPhone { get; set; }

        [MaxLength(50)]
        public string MemberRelation { get; set; } = "Self"; // Self, Spouse, Child, Parent, Other

        [MaxLength(20)]
        public string? MemberGender { get; set; } // Male, Female, Other

        public int? MemberAge { get; set; }

        [MaxLength(500)]
        public string? MemberPhotoUrl { get; set; }

        [MaxLength(50)]
        public string? PaySlipNumber { get; set; }

        public DateTime? PaySlipIssuedAt { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PaySlipAmount { get; set; }

        [MaxLength(50)]
        public string? PaymentMethod { get; set; } // Online, bKash, Cash

        [MaxLength(100)]
        public string? PaymentTransactionId { get; set; }

        [MaxLength(50)]
        public string? VerificationCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [NotMapped]
        public string FormattedCardNumber => !string.IsNullOrWhiteSpace(CardNumber)
            ? CardNumber
            : $"AB-GYM-{GymMembershipId:D5}";

        [NotMapped]
        public string DisplayMemberName => !string.IsNullOrWhiteSpace(MemberName)
            ? MemberName
            : (User?.FullName ?? "Resident Member");

        [NotMapped]
        public string DisplayMemberPhone => !string.IsNullOrWhiteSpace(MemberPhone)
            ? MemberPhone
            : (User?.PhoneNumber ?? "N/A");

        [NotMapped]
        public string FormattedPaySlipNumber => !string.IsNullOrWhiteSpace(PaySlipNumber)
            ? PaySlipNumber
            : $"GYM-SLIP-{GymMembershipId:D5}";

        [NotMapped]
        public string FormattedVerificationCode => !string.IsNullOrWhiteSpace(VerificationCode)
            ? VerificationCode
            : FormattedCardNumber;

        [NotMapped]
        public bool IsEffectiveActive => Status == GymMembershipStatuses.Active &&
            (ExpiryDate == null || ExpiryDate.Value.Date >= DateTime.UtcNow.Date) &&
            (CancellationEffectiveDate == null || CancellationEffectiveDate.Value.Date >= DateTime.UtcNow.Date);
    }
}
