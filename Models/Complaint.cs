using System.ComponentModel.DataAnnotations;

namespace ADHUNIK_BARI.Models
{
    public class Complaint
    {
        [Key]
        public int ComplaintId { get; set; }

        [Required]
        public int FlatId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string Category { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? ImagePath { get; set; }

        // Pending, In Progress, Solved
        [Required]
        public string ComplaintStatus { get; set; } = "Pending";

        public string? ManagerNote { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public string? ResolvedByUserId { get; set; }

        public Flat? Flat { get; set; }

        public ApplicationUser? User { get; set; }

        public ApplicationUser? ResolvedByUser { get; set; }
    }
}
