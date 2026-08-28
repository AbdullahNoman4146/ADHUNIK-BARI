using ADHUNIK_BARI.ViewModels;

namespace ADHUNIK_BARI.Services
{
    public interface IPropertyPaymentService
    {
        Task<PropertyCheckoutStartResult> StartCheckoutAsync(PropertyApplicationViewModel model);
        Task<PropertyCheckoutViewModel?> GetCheckoutAsync(int applicationId);
        Task<PropertyPaymentFinalizeResult> ConfirmPaymentAsync(int applicationId, string paymentIntentId);
        Task<bool> ProcessPaymentSuccessAsync(string paymentIntentId, long amount, Dictionary<string, string> metadata);
        Task ReleaseExpiredReservationsAsync();
    }

    public class PropertyCheckoutStartResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ApplicationId { get; set; }
    }

    public class PropertyPaymentFinalizeResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? ApplicationId { get; set; }
    }
}
