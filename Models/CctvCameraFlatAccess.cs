using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADHUNIK_BARI.Models
{
    public class CctvCameraFlatAccess
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CameraId { get; set; }

        [ForeignKey("CameraId")]
        public CctvCamera Camera { get; set; }

        [Required]
        public int FlatId { get; set; }

        [ForeignKey("FlatId")]
        public Flat Flat { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    }
}

