using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace ADHUNIK_BARI.Models
{
    public class Flat
    {
        [Key]
        public int FlatId { get; set; }

        [Required]
        public string FlatNumber { get; set; }

        public int FloorNumber { get; set; }

        // Available or Occupied
        public string FlatStatus { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation
        public ICollection<FlatAssignment> Assignments { get; set; }
    }
}
