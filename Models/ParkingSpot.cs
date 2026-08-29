using System.ComponentModel.DataAnnotations;

namespace ADHUNIK_BARI.Models
{
    public class ParkingSpot
    {

        public int ParkingSpotId { get; set; }


        [Required]
        public string SpotNumber { get; set; }


        [Required]
        public decimal ParkingFee { get; set; }


        public string? ParkingType { get; set; }


        public bool IsAvailable { get; set; } = true;



        // Assigned Flat

        public int? FlatId { get; set; }

        public Flat? Flat { get; set; }


        public DateTime CreatedAt { get; set; } = DateTime.Now;

    }
}