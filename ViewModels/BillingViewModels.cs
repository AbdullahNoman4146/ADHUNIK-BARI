using ADHUNIK_BARI.Models;

namespace ADHUNIK_BARI.ViewModels
{
    /// <summary>
    /// ViewModel for generating monthly bills - accepts billing parameters
    /// </summary>
    public class GenerateMonthlyBillsRequest
    {
        public int Month { get; set; } = DateTime.Now.Month; // 1-12

        public int Year { get; set; } = DateTime.Now.Year;

        /// <summary>
        /// Optional: Specific Flat Assignment ID. If null or 0, generates for all active residents.
        /// </summary>
        public int? TargetAssignmentId { get; set; }

        /// <summary>
        /// House Rent for Tenant(s) (৳).
        /// Flat Owners are automatically exempt from rent.
        /// </summary>
        public decimal? MonthlyRent { get; set; } = 15000;

        public decimal ServiceCharge { get; set; } = 2000;

        public decimal GasCharge { get; set; } = 1080;

        public decimal WaterCharge { get; set; } = 800;

        public decimal ElectricityCharge { get; set; } = 1500;

        public decimal MaintenanceCharge { get; set; } = 500;
    }

    /// <summary>
    /// ViewModel for response after generating bills
    /// </summary>
    public class GenerateMonthlyBillsResponse
    {
        public bool Success { get; set; }

        public string Message { get; set; }

        public int BillsGenerated { get; set; }

        public DateTime GeneratedAt { get; set; }

        public string Month { get; set; }

        public string Year { get; set; }
    }

    /// <summary>
    /// ViewModel for billing overview grouped by flat and resident type
    /// </summary>
    public class BillingOverviewViewModel
    {
        public string FlatNumber { get; set; }

        public string ResidentType { get; set; } // "Tenant" or "FlatOwner"

        public int TotalBills { get; set; }

        public int PaidCount { get; set; }

        public int UnpaidCount { get; set; }

        public int PartiallyPaidCount { get; set; }

        public decimal TotalAmountDue { get; set; }

        public decimal TotalAmountPaid { get; set; }

        public decimal OutstandingAmount => TotalAmountDue - TotalAmountPaid;
    }

    /// <summary>
    /// ViewModel for Manager Bills & Invoices Dashboard Page
    /// </summary>
    public class ManagerBillsPageViewModel
    {
        public GenerateMonthlyBillsRequest GenerateRequest { get; set; } = new();

        public List<FlatAssignment> ActiveAssignments { get; set; } = new();

        public List<BillingOverviewViewModel> OverviewList { get; set; } = new();

        public List<Bill> RecentBills { get; set; } = new();

        public List<Payment> RecentPayments { get; set; } = new();

        public decimal TotalBilledAmount { get; set; }

        public decimal TotalCollectedAmount { get; set; }

        public decimal TotalDueAmount { get; set; }

        public int TotalUnpaidBills { get; set; }

        public int TotalActiveAssignments { get; set; }
    }
}
