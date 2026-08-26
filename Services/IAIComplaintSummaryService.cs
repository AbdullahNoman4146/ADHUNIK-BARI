using ADHUNIK_BARI.Models;

namespace ADHUNIK_BARI.Services
{
    public interface IAIComplaintSummaryService
    {
        Task<AIComplaintReport> GenerateComplaintSummary(
            string complaintText
        );
    }
}