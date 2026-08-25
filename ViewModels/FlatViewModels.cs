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