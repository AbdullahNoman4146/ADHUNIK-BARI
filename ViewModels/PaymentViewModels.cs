namespace ADHUNIK_BARI.ViewModels
{
    /// <summary>
    /// Request to create a Stripe PaymentIntent for selected bill items
    /// </summary>
    public class CreatePaymentIntentRequest
    {
        public int BillId { get; set; }
        public List<int> SelectedItemIds { get; set; } = new();
    }

    /// <summary>
    /// Request to confirm a successful Stripe payment on the server
    /// </summary>
    public class ConfirmPaymentRequest
    {
        public string PaymentIntentId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Result after confirming and processing payment
    /// </summary>
    public class ConfirmPaymentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int PaymentId { get; set; }
        public int BillId { get; set; }
    }

    /// <summary>
    /// Response containing Stripe client secret for payment confirmation
    /// </summary>
    public class PaymentIntentResponse
    {
        public string ClientSecret { get; set; }
        public long AmountInSmallestUnit { get; set; } // Amount in fils (1 BDT = 100 fils)
        public string Currency { get; set; } = "bdt";
        public string PaymentIntentId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// ViewModel for displaying payment receipt details
    /// </summary>
    public class ReceiptViewModel
    {
        public string ReceiptNumber { get; set; }
        public DateTime PaymentDateTime { get; set; }
        public string TransactionId { get; set; }
        public string StripeReceiptUrl { get; set; }

        // Resident details
        public string ResidentName { get; set; }
        public string ResidentEmail { get; set; }
        public string ResidentPhone { get; set; }
        public string ResidentRole { get; set; } // "Tenant" or "Flat Owner"
        public string FlatNumber { get; set; }
        public int FloorNumber { get; set; }

        // Payment details
        public decimal TotalAmountPaid { get; set; }
        public string PaymentStatus { get; set; } = "Completed";

        // Billing period
        public int BillingMonth { get; set; }
        public int BillingYear { get; set; }

        // Itemized charges paid in this transaction
        public List<PaidItemDetail> PaidItems { get; set; } = new();

        // Organization header
        public string OrganizationName { get; set; } = "ADHUNIK BARI";
        public string OrganizationSubtitle { get; set; } = "Money Receipt";
    }

    /// <summary>
    /// Details of a single bill item paid in a transaction
    /// </summary>
    public class PaidItemDetail
    {
        public int BillItemId { get; set; }
        public string ItemType { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
    }

    /// <summary>
    /// Webhook payload from Stripe for payment_intent.succeeded events
    /// </summary>
    public class StripeWebhookPayload
    {
        public string Type { get; set; }
        public StripeEventData Data { get; set; }
    }

    /// <summary>
    /// Event data from Stripe webhook
    /// </summary>
    public class StripeEventData
    {
        public StripePaymentIntentObject Object { get; set; }
    }

    /// <summary>
    /// PaymentIntent object from Stripe webhook
    /// </summary>
    public class StripePaymentIntentObject
    {
        public string Id { get; set; }
        public long Amount { get; set; }
        public string Currency { get; set; }
        public string Status { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
        public string ReceiptEmail { get; set; }
        public string ClientSecret { get; set; }
        public long Created { get; set; }
    }
}
