using System.Collections.Generic;
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
    }
}
