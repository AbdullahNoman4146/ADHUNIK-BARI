using System.ComponentModel.DataAnnotations;

namespace ADHUNIK_BARI.Models
{
    public class Bill
    {
        [Key]
        public int BillId { get; set; }

        [Required]
        public int AssignmentId { get; set; }

        public int BillMonth { get; set; }

        public int BillYear { get; set; }

        [Range(0, double.MaxValue)]
        public decimal TotalAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal PaidAmount { get; set; }

        [Range(0, double.MaxValue)]
        public decimal DueAmount { get; set; }

        public DateTime Deadline { get; set; }

        [Required]
        public string BillStatus { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; }

        public FlatAssignment? Assignment { get; set; }

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();

        public ICollection<BillItem> BillItems { get; set; } = new List<BillItem>();
    }
}
