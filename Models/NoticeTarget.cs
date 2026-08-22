using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADHUNIK_BARI.Models
{
    public class NoticeTarget
    {
        [Key]
        public int NoticeTargetId { get; set; }

        [Required]
        public int NoticeId { get; set; }

        [ForeignKey("NoticeId")]
        public Notice Notice { get; set; }

        // If null, means not specific to a flat (but for our app we'll not create targets for general notices)
        public int? FlatId { get; set; }

        [ForeignKey("FlatId")]
        public Flat Flat { get; set; }
    }
}
