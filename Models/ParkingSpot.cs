using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADHUNIK_BARI.Models
{
    public class ParkingSpot
    {
        [Key]
        public int ParkingSpotId { get; set; }

        // Associated Floor (e.g. Basement 2, Basement 1, Ground)
        public int? ParkingFloorId { get; set; }

        [ForeignKey("ParkingFloorId")]
        public ParkingFloor? Floor { get; set; }

        [Required]
        [MaxLength(50)]
        public string SpotNumber { get; set; } = string.Empty;

        [Required]
        [Range(0, double.MaxValue)]
        public decimal ParkingFee { get; set; }

        [MaxLength(50)]
        public string? ParkingType { get; set; } = "Car"; // Car, Bike

        // Status: Available, Assigned, ForSale, ToLet
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "Available";

        public bool IsAvailable { get; set; } = true;

        // Marketplace / Listing Extension (without touching PropertyListing)
        [Range(0, double.MaxValue)]
        public decimal? ListingPrice { get; set; }

        [MaxLength(500)]
        public string? ListingNotes { get; set; }

        // Assigned Flat
        public int? FlatId { get; set; }

        [ForeignKey("FlatId")]
        public Flat? Flat { get; set; }

        // Assigned Direct User (for external buyers / outsiders)
        public string? AssignedUserId { get; set; }

        [ForeignKey("AssignedUserId")]
        public ApplicationUser? AssignedUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Activity Logs
        public ICollection<ParkingActivityLog> ActivityLogs { get; set; } = new List<ParkingActivityLog>();
    }
}