using System.ComponentModel.DataAnnotations;
using ADHUNIK_BARI.Models;

namespace ADHUNIK_BARI.ViewModels
{
    public class NoticeViewModel
    {
        public int NoticeId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string NoticeType { get; set; } = "General";

        public List<int> TargetFlatIds { get; set; } = new();

        public IEnumerable<Flat> Flats { get; set; } = Enumerable.Empty<Flat>();
    }
}