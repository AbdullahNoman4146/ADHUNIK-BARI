using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ADHUNIK_BARI.Models;

namespace ADHUNIK_BARI.ViewModels
{
    public class CctvDashboardViewModel
    {
        public List<CctvCamera> Cameras { get; set; } = new();
        public string? SelectedZone { get; set; }
        public List<string> AvailableZones { get; set; } = new();
        public int TotalCameras { get; set; }
        public int OnlineCount { get; set; }
        public int OfflineCount { get; set; }
        public List<Flat> AvailableFlats { get; set; } = new();
    }

    public class CctvCameraViewModel
    {
        public int CameraId { get; set; }

        [Required(ErrorMessage = "Camera Name is required.")]
        [StringLength(100)]
        [Display(Name = "Camera Name")]
        public string CameraName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required.")]
        [StringLength(100)]
        [Display(Name = "Location / Zone")]
        public string Location { get; set; } = "Main Gate";

        [Required(ErrorMessage = "Stream URL is required.")]
        [StringLength(1000)]
        [Display(Name = "Stream URL")]
        public string StreamUrl { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Online";

        [StringLength(50)]
        [Display(Name = "Access Permission")]
        public string AccessType { get; set; } = "All"; // "All" or "SpecificFlats"

        [Display(Name = "Accessible Flats")]
        public List<int> TargetFlatIds { get; set; } = new();

        public List<Flat> AvailableFlats { get; set; } = new();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}

