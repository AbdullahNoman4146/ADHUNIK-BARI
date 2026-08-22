using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ADHUNIK_BARI.Models
{
    public class Notice
    {
        [Key]
        public int NoticeId { get; set; }

        public string CreatedByUserId { get; set; }

        public ApplicationUser CreatedBy { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        // General, FlatSpecific, MultipleFlats
        public string NoticeType { get; set; }

        public DateTime CreatedAt { get; set; }

        public ICollection<NoticeTarget> Targets { get; set; }
    }
}
