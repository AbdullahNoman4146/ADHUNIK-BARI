using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.ViewModels;

namespace ADHUNIK_BARI.Services
{
    /// <summary>
    /// Service interface for dynamic monthly bill generation and billing overview
    /// </summary>
    public interface IBillingService
    {
        /// <summary>
        /// Generates monthly bills for all active flat assignments or a targeted resident.
        /// Flat Owners are strictly exempt from House Rent.
        /// </summary>
        Task<int> GenerateMonthlyBillsAsync(
            int month,
            int year,
            decimal serviceCharge = 0,
            decimal gasCharge = 0,
            decimal waterCharge = 0,
            decimal electricityCharge = 0,
            decimal maintenanceCharge = 0,
            int? targetAssignmentId = null,
            decimal? monthlyRent = null,
            decimal otherCharge = 0,
            string? otherDescription = null);

        /// <summary>
        /// Appoints or updates an "Other" charge on an existing bill.
        /// Recalculates total bill amount, due amount, and status.
        /// </summary>
        Task<bool> AppointOtherChargeAsync(int billId, decimal amount, string? description);

        /// <summary>
        /// Appoints or updates an "Other" charge to all bills in a specific month and year.
        /// </summary>
        Task<int> AppointOtherChargeForMonthAsync(int month, int year, decimal amount, string? description);

        /// <summary>
        /// Retrieves bill overview grouped by flat and resident type with payment status counts
        /// </summary>
        Task<List<BillingOverviewViewModel>> GetBillingOverviewForManagerAsync();

        /// <summary>
        /// Checks if bills already exist for a specific month/year
        /// </summary>
        Task<bool> BillsExistForMonthAsync(int month, int year);

        /// <summary>
        /// Gets all active flat assignments with related entities
        /// </summary>
        Task<List<FlatAssignment>> GetActiveFlatAssignmentsAsync();
    }
}
