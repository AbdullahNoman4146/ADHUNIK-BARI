using System.ComponentModel.DataAnnotations;
using ADHUNIK_BARI.Models;

namespace ADHUNIK_BARI.ViewModels
{
    public class CreateFlatViewModel
    {
        [Required]
        public string FlatNumber { get; set; }

        [Range(0, int.MaxValue)]
        public int FloorNumber { get; set; }
    }

    public class AssignFlatViewModel
    {
        [Required]
        public int FlatId { get; set; }

        [Required]
        public string UserId { get; set; }

        [Required]
        public string ResidentType { get; set; }

        public IEnumerable<Flat> AvailableFlats { get; set; } = Enumerable.Empty<Flat>();

        public IEnumerable<ApplicationUser> Residents { get; set; } = Enumerable.Empty<ApplicationUser>();
    }

    public class ResidentFlatViewModel
    {
        public FlatAssignment Assignment { get; set; }
    }
}