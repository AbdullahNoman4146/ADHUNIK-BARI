using ADHUNIK_BARI.ViewModels;

namespace ADHUNIK_BARI.Services
{
    public interface IPaymentService
    {
        /// <summary>
        /// Creates a Stripe PaymentIntent for dynamically selected bill items.
        /// Server-side validates bill and item amounts.
        /// </summary>
        /// <param name="userId">Current user ID (resident)</param>
        /// <param name="billId">Bill ID to pay</param>
        /// <param name="selectedItemIds">IDs of selected BillItems to pay</param>
        /// <returns>PaymentIntentResponse with clientSecret for frontend confirmation</returns>
        Task<PaymentIntentResponse> CreatePaymentIntentAsync(string userId, int billId, List<int> selectedItemIds);

        /// <summary>
        /// Confirms a successful client-side Stripe payment and records it in the database
        /// </summary>
        /// <param name="userId">Current user ID (resident)</param>
        /// <param name="paymentIntentId">Stripe PaymentIntent ID</param>
        /// <returns>ConfirmPaymentResult</returns>
        Task<ConfirmPaymentResult> ConfirmPaymentAsync(string userId, string paymentIntentId);

        /// <summary>
        /// Processes a successful Stripe event or verified PaymentIntent.
        /// Updates BillItems status, Bill status, and creates Payment record.
        /// </summary>
        /// <param name="paymentIntentId">Stripe PaymentIntent ID</param>
        /// <param name="amount">Amount paid in smallest currency unit (fils for BDT)</param>
        /// <param name="metadata">Metadata from PaymentIntent with BillId, ResidentId, SelectedItemIds</param>
        /// <returns>True if processing succeeded</returns>
        Task<bool> ProcessPaymentSuccessAsync(string paymentIntentId, long amount, Dictionary<string, string> metadata);

        /// <summary>
        /// Retrieves payment and receipt details for display.
        /// </summary>
        /// <param name="paymentId">Database PaymentId (not Stripe ID)</param>
        /// <param name="userId">Current user ID for access validation</param>
        /// <param name="isManagerOrAdmin">Whether requesting user is Manager/Admin</param>
        /// <returns>ReceiptViewModel with all payment and billing details</returns>
        Task<ReceiptViewModel?> GetReceiptDetailsAsync(int paymentId, string userId, bool isManagerOrAdmin = false);
    }
}
