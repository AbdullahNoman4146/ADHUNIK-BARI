using System.ComponentModel.DataAnnotations;
using ADHUNIK_BARI.Models;

namespace ADHUNIK_BARI.ViewModels
{
    public class ParkingFloorPlanViewModel
    {
        // Selected & available floors
        public int? SelectedFloorId { get; set; }
        public ParkingFloor? CurrentFloor { get; set; }
        public List<ParkingFloor> Floors { get; set; } = new();

        // Spots for current floor (rendered in full capacity)
        public List<ParkingSpotTileViewModel> Spots { get; set; } = new();

        // Global metrics across all floors
        public int TotalCapacity { get; set; }
        public int AvailableCount { get; set; }
        public int AssignedCount { get; set; }
        public int ForSaleCount { get; set; }
        public int ToLetCount { get; set; }

        // Revenue summary
        public decimal RevenueCollected { get; set; }
        public decimal RevenueDue { get; set; }

        // Filter & Search states
        public string? SearchQuery { get; set; }
        public string? StatusFilter { get; set; }

        // Flats list for typeahead assignment (occupied flats only)
        public List<FlatLookupItem> OccupiedFlats { get; set; } = new();

        // Activity log history
        public List<ParkingActivityLogViewModel> RecentActivities { get; set; } = new();
    }

    public class ParkingSpotTileViewModel
    {
        public int ParkingSpotId { get; set; }
        public int? ParkingFloorId { get; set; }
        public string FloorName { get; set; } = string.Empty;
        public string SpotNumber { get; set; } = string.Empty;
        public string Status { get; set; } = "Available"; // Available, Assigned, ForSale, ToLet
        public string VehicleType { get; set; } = "Car"; // Car, Bike
        public decimal MonthlyFee { get; set; }
        public decimal? ListingPrice { get; set; }
        public string? ListingNotes { get; set; }

        // Assignment Info
        public int? FlatId { get; set; }
        public string? FlatNumber { get; set; }
        public int? FlatFloor { get; set; }
        public string? ResidentName { get; set; }
        public string? ResidentType { get; set; }

        // Payment status for assigned spots: "Paid" or "Due"
        public string PaymentStatus { get; set; } = "Due";
    }

    public class FlatLookupItem
    {
        public int FlatId { get; set; }
        public string FlatNumber { get; set; } = string.Empty;
        public int FloorNumber { get; set; }
        public string ResidentName { get; set; } = string.Empty;
        public string ResidentType { get; set; } = string.Empty;
    }

    public class CreateParkingFloorViewModel
    {
        [Required(ErrorMessage = "Floor name is required (e.g. Basement 2)")]
        [MaxLength(100)]
        [Display(Name = "Floor Name")]
        public string FloorName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Floor code is required (e.g. B2)")]
        [MaxLength(20)]
        [Display(Name = "Floor Code / Prefix")]
        public string FloorCode { get; set; } = string.Empty;

        [Required]
        [Range(1, 500, ErrorMessage = "Capacity must be between 1 and 500")]
        [Display(Name = "Total Parking Capacity")]
        public int Capacity { get; set; } = 20;

        [Required]
        [Display(Name = "Default Vehicle Type")]
        public string DefaultVehicleType { get; set; } = "Car";

        [Required]
        [Range(0, 1000000, ErrorMessage = "Monthly fee must be 0 or higher")]
        [Display(Name = "Default Monthly Fee (BDT)")]
        public decimal DefaultMonthlyFee { get; set; } = 1500;
    }

    public class AssignParkingInputModel
    {
        [Required]
        public int ParkingSpotId { get; set; }

        [Required(ErrorMessage = "Please select a flat to assign")]
        public int FlatId { get; set; }
    }

    public class UpdateSpotStatusInputModel
    {
        [Required]
        public int ParkingSpotId { get; set; }

        [Required]
        public string Status { get; set; } = "Available"; // Available, ForSale, ToLet

        public decimal? ListingPrice { get; set; }

        public string? ListingNotes { get; set; }
    }

    public class ResidentParkingViewModel
    {
        public bool HasAssignedParking => Spots.Count > 0;
        public List<ResidentParkingSpotItem> Spots { get; set; } = new();
    }

    public class ResidentParkingSpotItem
    {
        public int ParkingSpotId { get; set; }
        public string SpotNumber { get; set; } = string.Empty;
        public string FloorName { get; set; } = "Basement";
        public string VehicleType { get; set; } = "Car";
        public decimal MonthlyFee { get; set; }
        public string PaymentStatus { get; set; } = "Due"; // Paid, Due
        public DateTime? AssignedDate { get; set; }
    }

    public class ParkingActivityLogViewModel
    {
        public int ActivityId { get; set; }
        public string SpotNumber { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

