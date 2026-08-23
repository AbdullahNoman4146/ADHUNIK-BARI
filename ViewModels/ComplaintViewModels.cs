using System.ComponentModel.DataAnnotations;
using ADHUNIK_BARI.Models;

namespace ADHUNIK_BARI.ViewModels
{
    public class SubmitComplaintViewModel
    {
        [Required]
        [StringLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(2000, MinimumLength = 10)]
        public string Description { get; set; } = string.Empty;

        public IFormFile? Image { get; set; }
    }

    public class MyComplaintsViewModel
    {
        public string? FlatNumber { get; set; }

        public IEnumerable<Complaint> Complaints { get; set; } = Enumerable.Empty<Complaint>();
    }
}
