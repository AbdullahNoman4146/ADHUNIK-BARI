using System.ComponentModel.DataAnnotations;
using ADHUNIK_BARI.Models;

namespace ADHUNIK_BARI.ViewModels
{
    public class GymSettingsViewModel
    {
        public int GymSettingId { get; set; }

        [Required(ErrorMessage = "Gym name is required.")]
        [MaxLength(150)]
        [Display(Name = "Gym / Facility Name")]
        public string GymName { get; set; } = "Adhunik Bari Fitness & Health Club";

        [Required(ErrorMessage = "Opening hours are required.")]
        [MaxLength(100)]
        [Display(Name = "Opening Hours")]
        public string OpeningHours { get; set; } = "6:00 AM - 10:00 PM (Daily)";

        [Required(ErrorMessage = "Monthly fee is required.")]
        [Range(0, 100000, ErrorMessage = "Fee must be a valid positive amount.")]
        [Display(Name = "Standard Monthly Fee (৳)")]
        public decimal MonthlyFee { get; set; } = 500.00m;

        [Display(Name = "Equipment Information")]
        [MaxLength(2000)]
        public string? EquipmentInformation { get; set; }

        [Display(Name = "Gym Rules & Regulations")]
        [MaxLength(2000)]
        public string? RulesAndGuidelines { get; set; }

        [Display(Name = "Facility Location")]
        [MaxLength(150)]
        public string? Location { get; set; } = "2nd Floor Amenities Area";

        [Display(Name = "Contact / Helpline Phone")]
        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; }

        public string? UpdatedByName { get; set; }
    }

    public class GymApplicationRequest
    {
        [Display(Name = "Member Full Name (Pass Holder)")]
        [MaxLength(150)]
        public string? MemberName { get; set; }

        [Required(ErrorMessage = "Please specify the relationship.")]
        [Display(Name = "Relationship to Resident")]
        public string MemberRelation { get; set; } = "Self"; // Self, Spouse, Son, Daughter, Parent, Other

        [Display(Name = "Member Direct Phone")]
        [MaxLength(50)]
        public string? MemberPhone { get; set; }

        [Display(Name = "Gender")]
        public string? MemberGender { get; set; } // Male, Female, Other

        [Display(Name = "Age")]
        [Range(12, 100, ErrorMessage = "Member age must be 12 or older.")]
        public int? MemberAge { get; set; }

        [Required(ErrorMessage = "A clear portrait photo is required for your official Gym ID card.")]
        [Display(Name = "Member Photo (for ID Card)")]
        public IFormFile? MemberPhoto { get; set; }

        public string? MemberPhotoUrl { get; set; }

        [Display(Name = "Membership Package")]
        [Range(1, 12, ErrorMessage = "Please select a valid package.")]
        public int PackageMonths { get; set; } = 1;

        public static decimal GetPackageFee(int months) => months switch
        {
            3 => 1200m,
            6 => 2500m,
            _ => 500m
        };

        public static string GetPackageLabel(int months) => months switch
        {
            3 => "3 Months (৳1,200)",
            6 => "6 Months (৳2,500)",
            _ => "1 Month (৳500)"
        };

        [Display(Name = "Fitness Goals / Health Notes (Optional)")]
        [MaxLength(1000)]
        public string? Notes { get; set; }

        [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to the Gym Rules & Regulations.")]
        public bool AgreeToRules { get; set; }
    }

    public class GymUpdatePhotoRequest
    {
        [Required]
        public int GymMembershipId { get; set; }

        [Required(ErrorMessage = "Please select an image file.")]
        [Display(Name = "Member Photo")]
        public IFormFile? MemberPhoto { get; set; }
    }

    public class GymApprovalRequest
    {
        [Required]
        public int GymMembershipId { get; set; }

        [Required(ErrorMessage = "Start date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Membership Start Date")]
        public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;

        [Required(ErrorMessage = "Expiry date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Membership Expiry Date")]
        public DateTime ExpiryDate { get; set; } = DateTime.UtcNow.Date.AddMonths(1);

        [Required]
        [Range(0, 100000, ErrorMessage = "Total fee must be 0 or greater.")]
        [Display(Name = "Pay Slip Fee Amount (৳)")]
        public decimal TotalFeeAmount { get; set; } = 500.00m;

        [Display(Name = "Pay Slip Notes / Instructions (Optional)")]
        [MaxLength(500)]
        public string? PaySlipNotes { get; set; }
    }

    public class GymRejectionRequest
    {
        [Required]
        public int GymMembershipId { get; set; }

        [Required(ErrorMessage = "Please provide a reason for rejection.")]
        [MaxLength(500)]
        [Display(Name = "Rejection Reason")]
        public string RejectionReason { get; set; } = string.Empty;
    }

    public class GymCancellationRequest
    {
        [Required]
        public int GymMembershipId { get; set; }

        [MaxLength(500)]
        [Display(Name = "Cancellation Reason (Optional)")]
        public string? CancellationReason { get; set; }
    }

    public class GymPaymentRequest
    {
        [Required]
        public int MembershipId { get; set; }

        [Required]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Online / Card"; // Online, bKash, Cash

        [Display(Name = "Transaction ID / Mobile Banking Reference")]
        [MaxLength(100)]
        public string? TransactionId { get; set; }
    }

    public class GymPaySlipViewModel
    {
        public int MembershipId { get; set; }

        public string PaySlipNumber { get; set; } = string.Empty;

        public string MemberName { get; set; } = string.Empty;

        public string ApplicantName { get; set; } = string.Empty;

        public string FlatNumber { get; set; } = "N/A";

        public string ResidentType { get; set; } = "Resident Member";

        public string MemberRelation { get; set; } = "Self";

        public string? MemberPhone { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        public int DurationDays => Math.Max(1, (int)(ExpiryDate.Date - StartDate.Date).TotalDays);

        public int DurationMonths { get; set; } = 1;

        public string PackageName => DurationMonths switch
        {
            3 => "3 Months Fitness Pass",
            6 => "6 Months Fitness Pass",
            _ => "1 Month Fitness Pass"
        };

        public decimal PaySlipAmount { get; set; }

        public bool IsPaid { get; set; }

        public DateTime? FeePaidAt { get; set; }

        public string? PaymentMethod { get; set; }

        public string? PaymentTransactionId { get; set; }

        public string GymName { get; set; } = "Adhunik Bari Fitness & Health Club";

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        public string? ApprovedByName { get; set; }

        public string? CardNumber { get; set; }

        public string? Notes { get; set; }

        public string? PublishableKey { get; set; }

        public string? ClientSecret { get; set; }

        public string? StripePaymentIntentId { get; set; }
    }

    public class GymMembershipItemViewModel
    {
        public int GymMembershipId { get; set; }

        public string UserId { get; set; } = string.Empty;

        public string ResidentName { get; set; } = string.Empty;

        public string MemberName { get; set; } = string.Empty;

        public string MemberRelation { get; set; } = "Self";

        public string? MemberPhone { get; set; }

        public string? MemberGender { get; set; }

        public int? MemberAge { get; set; }

        public string? MemberPhotoUrl { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string FlatNumber { get; set; } = "N/A";

        public string ResidentType { get; set; } = "Resident";

        public string Status { get; set; } = GymMembershipStatuses.Pending;

        public decimal MonthlyFee { get; set; }

        public int DurationMonths { get; set; } = 1;

        public string PackageName => DurationMonths switch
        {
            3 => "3 Months Pass",
            6 => "6 Months Pass",
            _ => "1 Month Pass"
        };

        public decimal PaySlipAmount { get; set; }

        public string? PaySlipNumber { get; set; }

        public string FormattedPaySlipNumber => !string.IsNullOrWhiteSpace(PaySlipNumber)
            ? PaySlipNumber
            : $"GYM-SLIP-{GymMembershipId:D5}";

        public DateTime RequestedAt { get; set; }

        public DateTime? StartDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public DateTime? ApprovedAt { get; set; }

        public string? ApprovedByName { get; set; }

        public DateTime? RejectedAt { get; set; }

        public string? RejectionReason { get; set; }

        public DateTime? CancellationRequestedAt { get; set; }

        public DateTime? CancellationEffectiveDate { get; set; }

        public string? CancellationReason { get; set; }

        public string RenewalStatus { get; set; } = "None"; // None, RenewalPending, Renewed

        public DateTime? RenewalRequestedAt { get; set; }

        public string? Notes { get; set; }

        public bool IsFeePaid { get; set; }

        public DateTime? FeePaidAt { get; set; }

        public string? PaymentMethod { get; set; }

        public string? CardNumber { get; set; }

        public string FormattedCardNumber => !string.IsNullOrWhiteSpace(CardNumber) ? CardNumber : $"AB-GYM-{GymMembershipId:D5}";

        public bool IsPaid => IsFeePaid;

        public int DaysRemaining
        {
            get
            {
                if (ExpiryDate.HasValue && ExpiryDate.Value.Date >= DateTime.UtcNow.Date)
                {
                    return (int)(ExpiryDate.Value.Date - DateTime.UtcNow.Date).TotalDays;
                }
                return 0;
            }
        }

        public bool IsEffectiveActive => Status == GymMembershipStatuses.Active &&
            (ExpiryDate == null || ExpiryDate.Value.Date >= DateTime.UtcNow.Date) &&
            (CancellationEffectiveDate == null || CancellationEffectiveDate.Value.Date >= DateTime.UtcNow.Date);

        public bool IsPendingPayment => !IsPaid && !string.IsNullOrWhiteSpace(PaySlipNumber);

        public string DisplayStatus
        {
            get
            {
                if (Status == GymMembershipStatuses.Pending)
                {
                    return !string.IsNullOrWhiteSpace(PaySlipNumber) ? "Pending Payment" : "Pending Review";
                }
                return Status;
            }
        }
    }

    public class ResidentGymDashboardViewModel
    {
        public GymSettingsViewModel GymSettings { get; set; } = new();

        public List<GymMembershipItemViewModel> MyMemberships { get; set; } = new();

        public List<GymMembershipItemViewModel> UnpaidPaySlips => MyMemberships
            .Where(m => !m.IsPaid && !string.IsNullOrWhiteSpace(m.PaySlipNumber))
            .ToList();

        public List<GymMembershipItemViewModel> ActivePaidMembers => MyMemberships
            .Where(m => m.Status == GymMembershipStatuses.Active && m.IsPaid)
            .ToList();

        public List<GymMembershipItemViewModel> ActiveMembers => ActivePaidMembers;

        public List<GymMembershipItemViewModel> PendingApplications => MyMemberships
            .Where(m => m.Status == GymMembershipStatuses.Pending)
            .ToList();

        public List<GymMembershipItemViewModel> HistoryMemberships => MyMemberships.Where(m => 
            m.Status == GymMembershipStatuses.Expired || 
            m.Status == GymMembershipStatuses.Cancelled || 
            m.Status == GymMembershipStatuses.Rejected).ToList();

        public string ResidentName { get; set; } = string.Empty;

        public string FlatNumber { get; set; } = "N/A";

        public string ResidentType { get; set; } = "Resident";
    }

    public class GymIdCardViewModel
    {
        public int MembershipId { get; set; }

        public string CardNumber { get; set; } = string.Empty;

        public string MemberName { get; set; } = string.Empty;

        public string ApplicantName { get; set; } = string.Empty;

        public string ResidentName => !string.IsNullOrWhiteSpace(MemberName) ? MemberName : ApplicantName;

        public string MemberRelation { get; set; } = "Self";

        public string? MemberGender { get; set; }

        public int? MemberAge { get; set; }

        public string? MemberPhotoUrl { get; set; }

        public string? Email { get; set; }

        public string? Phone { get; set; }

        public string FlatNumber { get; set; } = "N/A";

        public string ResidentType { get; set; } = "Resident Member";

        public string Status { get; set; } = "Active";

        public DateTime StartDate { get; set; }

        public DateTime ExpiryDate { get; set; }

        public int DaysRemaining => Math.Max(0, (int)(ExpiryDate.Date - DateTime.UtcNow.Date).TotalDays);

        public string GymName { get; set; } = "Adhunik Bari Fitness & Health Club";

        public string Location { get; set; } = "Amenities Area (Floor 2)";

        public string OpeningHours { get; set; } = "6:00 AM - 10:00 PM";

        public string? EmergencyPhone { get; set; }

        public string? ApprovedByName { get; set; }

        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        public string VerificationCode { get; set; } = string.Empty;

        public string VerificationUrl { get; set; } = string.Empty;

        public string QrCodeUrl { get; set; } = string.Empty;
    }

    public class GymVerificationViewModel
    {
        public bool IsValid { get; set; }

        public string StatusBadge { get; set; } = "Active";

        public string Message { get; set; } = string.Empty;

        public string CardNumber { get; set; } = string.Empty;

        public string MemberName { get; set; } = string.Empty;

        public string ApplicantName { get; set; } = string.Empty;

        public string FlatNumber { get; set; } = "N/A";

        public string MemberRelation { get; set; } = "Self";

        public DateTime? StartDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public int DaysRemaining { get; set; }

        public string GymName { get; set; } = "Adhunik Bari Fitness & Health Club";

        public string Location { get; set; } = "Amenities Area (Floor 2)";

        public string OpeningHours { get; set; } = "6:00 AM - 10:00 PM";

        public string? ApprovedByName { get; set; }

        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
    }

    public class ManagerGymDashboardViewModel
    {
        public GymSettingsViewModel GymSettings { get; set; } = new();

        public List<GymMembershipItemViewModel> PendingRequests { get; set; } = new();

        public List<GymMembershipItemViewModel> PendingPaymentSlips { get; set; } = new();

        public List<GymMembershipItemViewModel> ActiveMembers { get; set; } = new();

        public List<GymMembershipItemViewModel> ExpiredOrCancelledMembers { get; set; } = new();

        public int TotalActiveMembers => ActiveMembers.Count;

        public int TotalPendingRequests => PendingRequests.Count + PendingPaymentSlips.Count;

        public int TotalExpiringSoon => ActiveMembers.Count(m => m.DaysRemaining <= 7 && m.DaysRemaining >= 0);

        public decimal EstimatedMonthlyRevenue => ActiveMembers.Sum(m => m.MonthlyFee);
    }
}
