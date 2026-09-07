using System.ComponentModel.DataAnnotations;

namespace ADHUNIK_BARI.Models
{
    public class ParkingFloor
    {
        [Key]
        public int ParkingFloorId { get; set; }

        [Required]
        [MaxLength(100)]
        public string FloorName { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string FloorCode { get; set; } = string.Empty;

        [Range(1, 500)]
        public int Capacity { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ParkingSpot> ParkingSpots { get; set; } = new List<ParkingSpot>();
    }
}

