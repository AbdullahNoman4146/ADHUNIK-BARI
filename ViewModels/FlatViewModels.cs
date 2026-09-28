using System.ComponentModel.DataAnnotations;
using ADHUNIK_BARI.Models;

namespace ADHUNIK_BARI.ViewModels
{
    public class CreateFlatViewModel
    {
        [Required]
        public string FlatNumber { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int FloorNumber { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Monthly Rent (৳)")]
        public decimal MonthlyRent { get; set; } = 15000;
    }

    public class EditFlatViewModel
    {
        public int FlatId { get; set; }

        [Required]
        public string FlatNumber { get; set; } = string.Empty;

        [Range(0, int.MaxValue)]
        public int FloorNumber { get; set; }

        [Range(0, double.MaxValue)]
        [Display(Name = "Monthly Rent (৳)")]
        public decimal MonthlyRent { get; set; }

        public string FlatStatus { get; set; } = "Available";

        // Occupant information when flat is occupied
        public bool IsOccupied { get; set; }
        public string? OccupantName { get; set; }
        public string? OccupantType { get; set; }
        public string? OccupantEmail { get; set; }
        public string? OccupantPhone { get; set; }
        public string? OccupantUserId { get; set; }
        public DateTime? AssignedDate { get; set; }
    }

    public class ResidentAccountViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string ResidentType { get; set; } = string.Empty;
        public int? FlatId { get; set; }
        public string? FlatNumber { get; set; }
        public int? FloorNumber { get; set; }
        public string? ParkingSpotNumber { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasActiveAssignment { get; set; }
        public string AccountStatus { get; set; } = "Active";
    }

    public class AssignFlatViewModel
    {
        [Required]
        public int FlatId { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string ResidentType { get; set; } = "Tenant";

        public IEnumerable<Flat> AvailableFlats { get; set; } = Enumerable.Empty<Flat>();

        public IEnumerable<ApplicationUser> Residents { get; set; } = Enumerable.Empty<ApplicationUser>();
    }

    public class ResidentFlatViewModel
    {
        public FlatAssignment? Assignment { get; set; }
    }
}