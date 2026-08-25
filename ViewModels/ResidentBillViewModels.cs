namespace ADHUNIK_BARI.ViewModels
{
    /// <summary>
    /// Represents a single bill item for resident display
    /// READ-ONLY ViewModel - no database modifications
    /// </summary>
    public class ResidentBillItemViewModel
    {
        public int BillItemId { get; set; }

        public string ItemType { get; set; } = string.Empty; // HouseRent, ServiceCharge, Gas, Water, Electricity, Maintenance

        public decimal Amount { get; set; }

        public string? Description { get; set; }

        public string PaymentStatus { get; set; } = "Unpaid"; // Pending, Unpaid, Paid, PartiallyPaid

        public bool IsCheckable => PaymentStatus != "Paid"; // Only allow checkbox for unpaid/pending items
    }

    /// <summary>
    /// Represents a bill header for resident display
    /// </summary>
    public class ResidentBillViewModel
    {
        public int BillId { get; set; }

        public int BillMonth { get; set; }

        public int BillYear { get; set; }

        public string FlatNumber { get; set; } = string.Empty;

        public string ResidentType { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal DueAmount { get; set; }

        public string BillStatus { get; set; } = "Unpaid"; // Paid, Unpaid, PartiallyPaid

        public DateTime Deadline { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<ResidentBillItemViewModel> BillItems { get; set; } = new();
    }

    /// <summary>
    /// Represents a past payment for history display
    /// </summary>
    public class ResidentPaymentHistoryViewModel
    {
        public int PaymentId { get; set; }

        public decimal AmountPaid { get; set; }

        public DateTime PaymentDate { get; set; }

        public string PaymentStatus { get; set; } = "Completed";

        public string? StripeReceiptUrl { get; set; }

        public string? Reference { get; set; }

        public string? ItemsDescription { get; set; } // Comma-separated list of items paid
    }

    /// <summary>
    /// Container ViewModel for MyBills page
    /// Aggregates current bills and payment history
    /// </summary>
    public class MyBillsViewModel
    {
        public string FlatNumber { get; set; } = string.Empty;

        public string ResidentType { get; set; } = string.Empty;

        public string ResidentName { get; set; } = string.Empty;

        public List<ResidentBillViewModel> CurrentBills { get; set; } = new();

        public List<ResidentPaymentHistoryViewModel> PaymentHistory { get; set; } = new();

        public decimal TotalOutstanding => CurrentBills.Sum(b => b.DueAmount);

        public int TotalUnpaidBills => CurrentBills.Count(b => b.BillStatus != "Paid");

        public bool HasBills => CurrentBills.Count > 0;

        public bool HasPaymentHistory => PaymentHistory.Count > 0;
    }
}
