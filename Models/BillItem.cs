using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADHUNIK_BARI.Models
{
    public class BillItem
    {
        [Key]
        public int BillItemId { get; set; }

        [Required]
        public int BillId { get; set; }

        [Required]
        [MaxLength(50)]
        public string ItemType { get; set; } // HouseRent, ServiceCharge, Gas, Water, Electricity, Maintenance

        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }

        [MaxLength(255)]
        public string? Description { get; set; }

        [Required]
        [MaxLength(50)]
        public string PaymentStatus { get; set; } = "Pending"; // Pending, Paid, PartiallyPaid

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("BillId")]
        public Bill? Bill { get; set; }
    }
}
