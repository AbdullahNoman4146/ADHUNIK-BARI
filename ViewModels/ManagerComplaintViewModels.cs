using System.ComponentModel.DataAnnotations;
using ADHUNIK_BARI.Models;

namespace ADHUNIK_BARI.ViewModels
{
    public class ManagerComplaintListViewModel
    {
        public string? FlatNumber { get; set; }

        public string? Status { get; set; }

        public IEnumerable<Complaint> Complaints { get; set; } = Enumerable.Empty<Complaint>();
    }

    public class UpdateComplaintViewModel
    {
        public int ComplaintId { get; set; }

        [Required]
        public string ComplaintStatus { get; set; } = string.Empty;

        [StringLength(2000)]
        public string? ManagerNote { get; set; }
    }
}
