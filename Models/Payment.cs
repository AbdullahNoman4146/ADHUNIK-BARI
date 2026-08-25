using System.ComponentModel.DataAnnotations;

namespace ADHUNIK_BARI.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int BillId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal AmountPaid { get; set; }

        public DateTime PaymentDate { get; set; }

        [Required]
        public string PaymentStatus { get; set; } = "Pending";

        public string? Reference { get; set; }

        [MaxLength(255)]
        public string? StripePaymentIntentId { get; set; }

        [MaxLength(500)]
        public string? StripeReceiptUrl { get; set; }

        [MaxLength(1000)]
        public string? PaidItemsJson { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Bill? Bill { get; set; }

        public ApplicationUser? User { get; set; }
    }
}
