using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADHUNIK_BARI.Models
{
    public class ParkingActivityLog
    {
        [Key]
        public int ActivityId { get; set; }

        public int? ParkingSpotId { get; set; }

        [ForeignKey("ParkingSpotId")]
        public ParkingSpot? ParkingSpot { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string Details { get; set; } = string.Empty;

        [MaxLength(150)]
        public string CreatedBy { get; set; } = "Manager";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

