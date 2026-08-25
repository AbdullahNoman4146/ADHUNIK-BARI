using Microsoft.EntityFrameworkCore;
using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.ViewModels;

namespace ADHUNIK_BARI.Services
{
    /// <summary>
    /// Service implementation for monthly bill generation and management overview
    /// </summary>
    public class BillingService : IBillingService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<BillingService> _logger;

        public BillingService(
            ApplicationDbContext dbContext,
            ILogger<BillingService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        /// <summary>
        /// Generates monthly bills for active flat assignments or a specific resident.
        /// STRICT BILLING RULE:
        /// - Tenants: Charged House Rent (custom or flat default) + Utilities + Service Charge.
        /// - Flat Owners: NEVER charged House Rent (exempt) - only Utilities + Service Charge.
        /// </summary>
        public async Task<int> GenerateMonthlyBillsAsync(
            int month,
            int year,
            decimal serviceCharge = 0,
            decimal gasCharge = 0,
            decimal waterCharge = 0,
            decimal electricityCharge = 0,
            decimal maintenanceCharge = 0,
            int? targetAssignmentId = null,
            decimal? monthlyRent = null)
        {
            try
            {
                var query = _dbContext.FlatAssignments
                    .Include(a => a.Flat)
                    .Include(a => a.User)
                    .Where(a => a.IsActive);

                if (targetAssignmentId.HasValue && targetAssignmentId.Value > 0)
                {
                    query = query.Where(a => a.AssignmentId == targetAssignmentId.Value);
                }

                var activeAssignments = await query.ToListAsync();

                if (activeAssignments.Count == 0)
                {
                    _logger.LogInformation("No matching active flat assignments found.");
                    return 0;
                }

                int billsGenerated = 0;

                foreach (var assignment in activeAssignments)
                {
                    // Check if bill already exists for this assignment for this month/year
                    bool billExists = await _dbContext.Bills
                        .AnyAsync(b => b.AssignmentId == assignment.AssignmentId && b.BillMonth == month && b.BillYear == year);

                    if (billExists)
                    {
                        continue;
                    }

                    var bill = new Bill
                    {
                        AssignmentId = assignment.AssignmentId,
                        BillMonth = month,
                        BillYear = year,
                        BillStatus = "Unpaid",
                        CreatedAt = DateTime.UtcNow,
                        Deadline = new DateTime(year, month, DateTime.DaysInMonth(year, month)).AddDays(7)
                    };

                    var billItems = new List<BillItem>();
                    var createdAt = DateTime.UtcNow;

                    // 1. HOUSE RENT: ONLY FOR TENANTS. FLAT OWNERS NEVER PAY RENT!
                    if (assignment.ResidentType == "Tenant")
                    {
                        decimal rentToCharge = 0;

                        if (targetAssignmentId.HasValue && targetAssignmentId.Value > 0 && monthlyRent.HasValue && monthlyRent.Value > 0)
                        {
                            rentToCharge = monthlyRent.Value;
                        }
                        else if (assignment.Flat != null && assignment.Flat.MonthlyRent > 0)
                        {
                            rentToCharge = assignment.Flat.MonthlyRent;
                        }
                        else if (monthlyRent.HasValue && monthlyRent.Value > 0)
                        {
                            rentToCharge = monthlyRent.Value;
                        }

                        if (rentToCharge > 0)
                        {
                            billItems.Add(new BillItem
                            {
                                ItemType = BillItemTypes.HouseRent,
                                Amount = rentToCharge,
                                Description = $"House Rent for Flat {assignment.Flat?.FlatNumber}",
                                PaymentStatus = "Unpaid",
                                CreatedAt = createdAt
                            });
                        }
                    }
                    // FlatOwner: Strictly No House Rent added.

                    // 2. SERVICE CHARGE
                    if (serviceCharge > 0)
                    {
                        billItems.Add(new BillItem
                        {
                            ItemType = BillItemTypes.ServiceCharge,
                            Amount = serviceCharge,
                            Description = $"Service Charge for Flat {assignment.Flat?.FlatNumber}",
                            PaymentStatus = "Unpaid",
                            CreatedAt = createdAt
                        });
                    }

                    // 3. UTILITIES & FEES
                    if (gasCharge > 0)
                    {
                        billItems.Add(new BillItem
                        {
                            ItemType = BillItemTypes.Gas,
                            Amount = gasCharge,
                            Description = $"Gas Bill for Flat {assignment.Flat?.FlatNumber}",
                            PaymentStatus = "Unpaid",
                            CreatedAt = createdAt
                        });
                    }

                    if (waterCharge > 0)
                    {
                        billItems.Add(new BillItem
                        {
                            ItemType = BillItemTypes.Water,
                            Amount = waterCharge,
                            Description = $"Water Bill for Flat {assignment.Flat?.FlatNumber}",
                            PaymentStatus = "Unpaid",
                            CreatedAt = createdAt
                        });
                    }

                    if (electricityCharge > 0)
                    {
                        billItems.Add(new BillItem
                        {
                            ItemType = BillItemTypes.Electricity,
                            Amount = electricityCharge,
                            Description = $"Electricity Bill for Flat {assignment.Flat?.FlatNumber}",
                            PaymentStatus = "Unpaid",
                            CreatedAt = createdAt
                        });
                    }

                    if (maintenanceCharge > 0)
                    {
                        billItems.Add(new BillItem
                        {
                            ItemType = BillItemTypes.Maintenance,
                            Amount = maintenanceCharge,
                            Description = $"Maintenance Fee for Flat {assignment.Flat?.FlatNumber}",
                            PaymentStatus = "Unpaid",
                            CreatedAt = createdAt
                        });
                    }

                    bill.TotalAmount = billItems.Sum(item => item.Amount);
                    bill.DueAmount = bill.TotalAmount;
                    bill.PaidAmount = 0;
                    bill.BillItems = billItems;

                    _dbContext.Bills.Add(bill);
                    billsGenerated++;
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Generated {billsGenerated} bills for {month}/{year}");
                return billsGenerated;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GenerateMonthlyBillsAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves billing overview grouped by flat and resident type with payment status distribution
        /// </summary>
        public async Task<List<BillingOverviewViewModel>> GetBillingOverviewForManagerAsync()
        {
            try
            {
                var overview = await _dbContext.Bills
                    .Include(b => b.Assignment)
                        .ThenInclude(a => a.Flat)
                    .Include(b => b.Assignment)
                        .ThenInclude(a => a.User)
                    .AsNoTracking()
                    .GroupBy(b => new { b.Assignment.Flat.FlatNumber, b.Assignment.ResidentType })
                    .Select(g => new BillingOverviewViewModel
                    {
                        FlatNumber = g.Key.FlatNumber,
                        ResidentType = g.Key.ResidentType,
                        TotalBills = g.Count(),
                        PaidCount = g.Count(b => b.BillStatus == "Paid"),
                        UnpaidCount = g.Count(b => b.BillStatus == "Unpaid"),
                        PartiallyPaidCount = g.Count(b => b.BillStatus == "PartiallyPaid"),
                        TotalAmountDue = g.Sum(b => b.DueAmount),
                        TotalAmountPaid = g.Sum(b => b.PaidAmount)
                    })
                    .OrderBy(o => o.FlatNumber)
                    .ToListAsync();

                return overview;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetBillingOverviewForManagerAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks if bills already exist for a specific month and year
        /// </summary>
        public async Task<bool> BillsExistForMonthAsync(int month, int year)
        {
            try
            {
                return await _dbContext.Bills
                    .AnyAsync(b => b.BillMonth == month && b.BillYear == year);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in BillsExistForMonthAsync: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Retrieves all active flat assignments with flat and resident user details
        /// </summary>
        public async Task<List<FlatAssignment>> GetActiveFlatAssignmentsAsync()
        {
            try
            {
                return await _dbContext.FlatAssignments
                    .Include(a => a.Flat)
                    .Include(a => a.User)
                    .Where(a => a.IsActive)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in GetActiveFlatAssignmentsAsync: {ex.Message}");
                throw;
            }
        }
    }
}
