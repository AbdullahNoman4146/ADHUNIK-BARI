using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.ViewModels;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace ADHUNIK_BARI.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ILogger<PaymentService> _logger;

        public PaymentService(ApplicationDbContext dbContext, ILogger<PaymentService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<PaymentIntentResponse> CreatePaymentIntentAsync(string userId, int billId, List<int> selectedItemIds)
        {
            try
            {
                // Validate bill exists and belongs to current user's assignment
                var bill = await _dbContext.Bills
                    .Include(b => b.BillItems)
                    .Include(b => b.Assignment)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(b => b.BillId == billId);

                if (bill == null)
                {
                    return new PaymentIntentResponse
                    {
                        Success = false,
                        Message = "Bill not found"
                    };
                }

                // Verify user owns this bill or is manager/admin
                if (bill.Assignment != null && bill.Assignment.UserId != userId)
                {
                    var isManagerOrAdmin = await _dbContext.UserRoles
                        .Join(_dbContext.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, r.Name })
                        .AnyAsync(x => x.UserId == userId && (x.Name == "Manager" || x.Name == "Admin"));

                    if (!isManagerOrAdmin)
                    {
                        return new PaymentIntentResponse
                        {
                            Success = false,
                            Message = "Unauthorized: Bill does not belong to this user"
                        };
                    }
                }

                // Validate selected items belong to this bill and sum their amounts
                var selectedItems = bill.BillItems
                    .Where(item => selectedItemIds.Contains(item.BillItemId))
                    .ToList();

                if (!selectedItems.Any())
                {
                    return new PaymentIntentResponse
                    {
                        Success = false,
                        Message = "No valid bill items selected"
                    };
                }

                if (selectedItems.Count != selectedItemIds.Count)
                {
                    return new PaymentIntentResponse
                    {
                        Success = false,
                        Message = "Some selected items do not belong to this bill"
                    };
                }

                // Server-side calculate total amount (NEVER trust client amounts)
                decimal totalAmountDecimal = selectedItems.Sum(item => item.Amount);

                // Convert BDT to smallest unit (fils): 1 BDT = 100 fils
                long amountInSmallestUnit = (long)(totalAmountDecimal * 100);

                if (amountInSmallestUnit <= 0)
                {
                    return new PaymentIntentResponse
                    {
                        Success = false,
                        Message = "Invalid payment amount"
                    };
                }

                // Get user for email
                var user = await _dbContext.Users.FindAsync(userId);
                if (user == null)
                {
                    return new PaymentIntentResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Create Stripe PaymentIntent
                var intentOptions = new PaymentIntentCreateOptions
                {
                    Amount = amountInSmallestUnit,
                    Currency = "bdt",
                    ReceiptEmail = user.Email,
                    Metadata = new Dictionary<string, string>
                    {
                        { "BillId", billId.ToString() },
                        { "ResidentId", userId },
                        { "SelectedItemIds", string.Join(",", selectedItemIds) }
                    },
                    Description = $"ADHUNIK BARI - Payment for Bill #{billId}"
                };

                var service = new PaymentIntentService();
                var intent = await service.CreateAsync(intentOptions);

                return new PaymentIntentResponse
                {
                    Success = true,
                    ClientSecret = intent.ClientSecret,
                    AmountInSmallestUnit = amountInSmallestUnit,
                    Currency = "bdt",
                    PaymentIntentId = intent.Id,
                    Message = "PaymentIntent created successfully"
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError($"Stripe error creating PaymentIntent: {ex.Message}");
                return new PaymentIntentResponse
                {
                    Success = false,
                    Message = $"Payment processing error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error creating PaymentIntent: {ex.Message}");
                return new PaymentIntentResponse
                {
                    Success = false,
                    Message = "An unexpected error occurred. Please try again."
                };
            }
        }

        public async Task<ConfirmPaymentResult> ConfirmPaymentAsync(string userId, string paymentIntentId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(paymentIntentId))
                {
                    return new ConfirmPaymentResult { Success = false, Message = "PaymentIntent ID is required." };
                }

                // 1. Check if already processed (idempotency)
                var existingPayment = await _dbContext.Payments
                    .Include(p => p.Bill)
                    .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);

                if (existingPayment != null)
                {
                    return new ConfirmPaymentResult
                    {
                        Success = true,
                        Message = "Payment has already been processed.",
                        PaymentId = existingPayment.PaymentId,
                        BillId = existingPayment.BillId
                    };
                }

                // 2. Fetch payment intent from Stripe to verify status
                var stripeService = new PaymentIntentService();
                var intent = await stripeService.GetAsync(paymentIntentId);

                if (intent == null)
                {
                    return new ConfirmPaymentResult { Success = false, Message = "Payment intent not found in Stripe." };
                }

                if (intent.Status != "succeeded")
                {
                    return new ConfirmPaymentResult { Success = false, Message = $"Payment is not succeeded (Status: {intent.Status})." };
                }

                // 3. Process database updates
                bool processed = await ProcessPaymentSuccessAsync(intent.Id, intent.Amount, intent.Metadata);

                if (!processed)
                {
                    return new ConfirmPaymentResult { Success = false, Message = "Failed to update database records." };
                }

                // 4. Get the created payment record
                var createdPayment = await _dbContext.Payments
                    .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);

                return new ConfirmPaymentResult
                {
                    Success = true,
                    Message = "Payment processed successfully.",
                    PaymentId = createdPayment?.PaymentId ?? 0,
                    BillId = createdPayment?.BillId ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ConfirmPaymentAsync: {ex.Message}");
                return new ConfirmPaymentResult { Success = false, Message = $"Payment confirmation error: {ex.Message}" };
            }
        }

        public async Task<bool> ProcessPaymentSuccessAsync(string paymentIntentId, long amount, Dictionary<string, string> metadata)
        {
            // Check if already processed
            var existing = await _dbContext.Payments.FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);
            if (existing != null)
            {
                return true;
            }

            using (var transaction = await _dbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    // Validate metadata
                    if (!metadata.TryGetValue("BillId", out var billIdStr) ||
                        !metadata.TryGetValue("ResidentId", out var residentId) ||
                        !metadata.TryGetValue("SelectedItemIds", out var selectedItemIdsStr))
                    {
                        _logger.LogError("Invalid payment metadata received");
                        return false;
                    }

                    if (!int.TryParse(billIdStr, out int billId))
                    {
                        _logger.LogError("Invalid BillId in metadata");
                        return false;
                    }

                    // Parse selected item IDs
                    var selectedItemIds = selectedItemIdsStr
                        .Split(',')
                        .Select(id => int.TryParse(id.Trim(), out int parsed) ? parsed : (int?)null)
                        .Where(id => id.HasValue)
                        .Select(id => id.Value)
                        .ToList();

                    if (!selectedItemIds.Any())
                    {
                        _logger.LogError("No valid item IDs in metadata");
                        return false;
                    }

                    // Fetch bill with items
                    var bill = await _dbContext.Bills
                        .Include(b => b.BillItems)
                        .Include(b => b.Assignment)
                        .FirstOrDefaultAsync(b => b.BillId == billId);

                    if (bill == null)
                    {
                        _logger.LogError($"Bill {billId} not found");
                        return false;
                    }

                    // Double-check ownership
                    if (bill.Assignment != null && bill.Assignment.UserId != residentId)
                    {
                        _logger.LogWarning($"Bill {billId} assignment user does not match metadata residentId");
                    }

                    // Mark selected items as Paid
                    var itemsToUpdate = bill.BillItems
                        .Where(item => selectedItemIds.Contains(item.BillItemId))
                        .ToList();

                    foreach (var item in itemsToUpdate)
                    {
                        item.PaymentStatus = "Paid";
                    }

                    // Recalculate bill paid and due amounts
                    var paidItems = bill.BillItems.Where(item => item.PaymentStatus == "Paid").ToList();
                    var unpaidItems = bill.BillItems.Where(item => item.PaymentStatus != "Paid").ToList();

                    bill.PaidAmount = paidItems.Sum(item => item.Amount);
                    bill.DueAmount = unpaidItems.Sum(item => item.Amount);

                    if (!unpaidItems.Any())
                    {
                        bill.BillStatus = "Paid";
                    }
                    else if (paidItems.Any())
                    {
                        bill.BillStatus = "PartiallyPaid";
                    }
                    else
                    {
                        bill.BillStatus = "Unpaid";
                    }

                    // Create Payment record
                    var payment = new Payment
                    {
                        BillId = billId,
                        UserId = residentId,
                        Amount = amount / 100m,
                        AmountPaid = amount / 100m, // Convert from smallest unit (fils) back to BDT
                        StripePaymentIntentId = paymentIntentId,
                        Reference = $"STRIPE-{paymentIntentId.Substring(Math.Max(0, paymentIntentId.Length - 10))}",
                        PaidItemsJson = string.Join(",", selectedItemIds),
                        PaymentDate = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        PaymentStatus = "Completed"
                    };

                    _dbContext.Payments.Add(payment);

                    // Save changes
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation($"Payment {paymentIntentId} processed successfully for Bill {billId}. Paid: {payment.AmountPaid}, Due Remaining: {bill.DueAmount}");
                    return true;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Error processing payment: {ex.Message}");
                    return false;
                }
            }
        }

        public async Task<ReceiptViewModel?> GetReceiptDetailsAsync(int paymentId, string userId, bool isManagerOrAdmin = false)
        {
            try
            {
                // Fetch payment with bill, items, flat, and users
                var payment = await _dbContext.Payments
                    .Include(p => p.User)
                    .Include(p => p.Bill)
                        .ThenInclude(b => b.BillItems)
                    .Include(p => p.Bill.Assignment)
                        .ThenInclude(a => a.Flat)
                    .Include(p => p.Bill.Assignment.User)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.PaymentId == paymentId);

                if (payment == null)
                {
                    _logger.LogWarning($"Payment ID {paymentId} not found in database.");
                    return null;
                }

                // Access control: allow if manager/admin OR if payment belongs to this user OR bill assigned to this user
                bool isOwner = (payment.UserId == userId) || 
                               (payment.Bill?.Assignment != null && payment.Bill.Assignment.UserId == userId);

                if (!isManagerOrAdmin && !isOwner)
                {
                    _logger.LogWarning($"Access denied for user {userId} to payment {paymentId}. Payment userId: {payment.UserId}");
                    return null;
                }

                // Parse paid item IDs from JSON
                var paidItemIds = (payment.PaidItemsJson ?? "")
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(id => int.TryParse(id.Trim(), out int parsed) ? parsed : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id.Value)
                    .ToList();

                var allItems = payment.Bill?.BillItems?.ToList() ?? new List<BillItem>();

                // Get paid items
                var paidItems = allItems
                    .Where(item => paidItemIds.Contains(item.BillItemId))
                    .Select(item => new PaidItemDetail
                    {
                        BillItemId = item.BillItemId,
                        ItemType = item.ItemType,
                        Description = item.Description,
                        Amount = item.Amount
                    })
                    .ToList();

                if (!paidItems.Any())
                {
                    if (allItems.Any())
                    {
                        paidItems = allItems.Select(item => new PaidItemDetail
                        {
                            BillItemId = item.BillItemId,
                            ItemType = item.ItemType,
                            Description = item.Description,
                            Amount = item.Amount
                        }).ToList();
                    }
                    else
                    {
                        paidItems = new List<PaidItemDetail>
                        {
                            new PaidItemDetail
                            {
                                BillItemId = 1,
                                ItemType = "Apartment Charges",
                                Description = "Monthly Service & Utility Dues",
                                Amount = payment.AmountPaid
                            }
                        };
                    }
                }

                // Resolve resident user details
                var residentUser = payment.User ?? payment.Bill?.Assignment?.User;
                if (residentUser == null && !string.IsNullOrEmpty(payment.UserId))
                {
                    residentUser = await _dbContext.Users.FindAsync(payment.UserId);
                }

                var flat = payment.Bill?.Assignment?.Flat;
                var residentRole = payment.Bill?.Assignment?.ResidentType == "Tenant" ? "Tenant" : "Flat Owner";

                return new ReceiptViewModel
                {
                    ReceiptNumber = $"RCP{payment.PaymentId:D6}",
                    PaymentDateTime = payment.PaymentDate != default ? payment.PaymentDate : payment.CreatedAt,
                    TransactionId = payment.StripePaymentIntentId ?? payment.Reference ?? $"TXN{payment.PaymentId:D6}",
                    StripeReceiptUrl = payment.StripeReceiptUrl,
                    ResidentName = residentUser?.FullName ?? "Resident",
                    ResidentEmail = residentUser?.Email ?? "N/A",
                    ResidentPhone = residentUser?.Phone ?? residentUser?.PhoneNumber ?? "N/A",
                    ResidentRole = residentRole,
                    FlatNumber = flat?.FlatNumber ?? "101",
                    FloorNumber = flat?.FloorNumber ?? 1,
                    TotalAmountPaid = payment.AmountPaid,
                    PaymentStatus = payment.PaymentStatus ?? "Completed",
                    BillingMonth = payment.Bill?.BillMonth ?? DateTime.UtcNow.Month,
                    BillingYear = payment.Bill?.BillYear ?? DateTime.UtcNow.Year,
                    PaidItems = paidItems
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving receipt details: {ex.Message} \n {ex.StackTrace}");
                return null;
            }
        }
    }
}
