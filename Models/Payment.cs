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

        public DateTime PaymentDate { get; set; }

        [Required]
        public string PaymentStatus { get; set; } = "Pending";

        public string? Reference { get; set; }

        public Bill? Bill { get; set; }

        public ApplicationUser? User { get; set; }
    }
}
