using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADHUNIK_BARI.Models
{
    public class FlatAssignment
    {
        [Key]
        public int AssignmentId { get; set; }

        [Required]
        public int FlatId { get; set; }

        [ForeignKey("FlatId")]
        public Flat Flat { get; set; }

        [Required]
        public string UserId { get; set; }

        [ForeignKey("UserId")]
        public ApplicationUser User { get; set; }

        // Tenant or Flat Owner
        public string ResidentType { get; set; }

        public DateTime AssignmentDate { get; set; }

        public bool IsActive { get; set; }
    }
}
