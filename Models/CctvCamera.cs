using System;
using System.ComponentModel.DataAnnotations;

namespace ADHUNIK_BARI.Models
{
    public class CctvCamera
    {
        [Key]
        public int CameraId { get; set; }

        [Required(ErrorMessage = "Camera Name is required")]
        [StringLength(100)]
        [Display(Name = "Camera Name")]
        public string CameraName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required")]
        [StringLength(100)]
        [Display(Name = "Location")]
        public string Location { get; set; } = "Main Gate";

        [Required(ErrorMessage = "Stream URL is required")]
        [StringLength(1000)]
        [Display(Name = "Stream URL")]
        public string StreamUrl { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Online";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
