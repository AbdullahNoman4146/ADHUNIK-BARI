using ADHUNIK_BARI.Models;

namespace ADHUNIK_BARI.ViewModels
{
    public class ComplaintAISummaryViewModel
    {

        public int Months { get; set; }


        public int TotalComplaints { get; set; }


        public AIComplaintReport Report { get; set; }
    = new();

    }
}