namespace ADHUNIK_BARI.Models
{
    public static class PropertyListingTypes
    {
        public const string ToLet = "ToLet";
        public const string ForSale = "ForSale";
    }

    public static class PropertyListingStatuses
    {
        public const string Draft = "Draft";
        public const string Published = "Published";
        public const string CheckoutReserved = "CheckoutReserved";
        public const string Rented = "Rented";
        public const string SaleReserved = "SaleReserved";
        public const string Closed = "Closed";
        public const string Archived = "Archived";
    }

    public static class PropertyApplicationStatuses
    {
        public const string PendingPayment = "PendingPayment";
        public const string PaymentProcessing = "PaymentProcessing";
        public const string AdvancePaid = "AdvancePaid";
        public const string TenantCreated = "TenantCreated";
        public const string SaleAwaitingOfflineCompletion = "SaleAwaitingOfflineCompletion";
        public const string Failed = "Failed";
        public const string Cancelled = "Cancelled";
        public const string Completed = "Completed";
        public const string NeedsManualReview = "NeedsManualReview";
    }

    public static class PropertyPaymentStatuses
    {
        public const string Pending = "Pending";
        public const string Succeeded = "Succeeded";
        public const string Failed = "Failed";
        public const string Cancelled = "Cancelled";
    }
}