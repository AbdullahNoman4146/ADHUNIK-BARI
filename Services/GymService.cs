using Microsoft.EntityFrameworkCore;
using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.ViewModels;
using Stripe;

namespace ADHUNIK_BARI.Services
{
    public class GymService : IGymService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IConfiguration _configuration;
        private readonly ILogger<GymService> _logger;
        private static bool _gymSchemaEnsured = false;

        public GymService(ApplicationDbContext dbContext, IConfiguration configuration, ILogger<GymService> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
        }

        private async Task EnsureGymSchemaAsync()
        {
            if (_gymSchemaEnsured) return;

            try
            {
                await _dbContext.Database.ExecuteSqlRawAsync(@"
                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'GymMemberships')
                    BEGIN
                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'CardNumber')
                            ALTER TABLE [GymMemberships] ADD [CardNumber] nvarchar(50) NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'FeePaidAt')
                            ALTER TABLE [GymMemberships] ADD [FeePaidAt] datetime2 NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'IsFeePaid')
                            ALTER TABLE [GymMemberships] ADD [IsFeePaid] bit NOT NULL CONSTRAINT [DF_GymMemberships_IsFeePaid] DEFAULT 0;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'MemberName')
                            ALTER TABLE [GymMemberships] ADD [MemberName] nvarchar(150) NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'MemberPhone')
                            ALTER TABLE [GymMemberships] ADD [MemberPhone] nvarchar(50) NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'MemberRelation')
                            ALTER TABLE [GymMemberships] ADD [MemberRelation] nvarchar(50) NOT NULL CONSTRAINT [DF_GymMemberships_MemberRelation] DEFAULT 'Self';

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'MemberGender')
                            ALTER TABLE [GymMemberships] ADD [MemberGender] nvarchar(20) NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'MemberAge')
                            ALTER TABLE [GymMemberships] ADD [MemberAge] int NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'MemberPhotoUrl')
                            ALTER TABLE [GymMemberships] ADD [MemberPhotoUrl] nvarchar(500) NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'PaySlipNumber')
                            ALTER TABLE [GymMemberships] ADD [PaySlipNumber] nvarchar(50) NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'PaySlipIssuedAt')
                            ALTER TABLE [GymMemberships] ADD [PaySlipIssuedAt] datetime2 NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'PaySlipAmount')
                            ALTER TABLE [GymMemberships] ADD [PaySlipAmount] decimal(18,2) NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'PaymentMethod')
                            ALTER TABLE [GymMemberships] ADD [PaymentMethod] nvarchar(50) NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'PaymentTransactionId')
                            ALTER TABLE [GymMemberships] ADD [PaymentTransactionId] nvarchar(100) NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'VerificationCode')
                            ALTER TABLE [GymMemberships] ADD [VerificationCode] nvarchar(50) NULL;

                        IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'DurationMonths')
                            ALTER TABLE [GymMemberships] ADD [DurationMonths] int NOT NULL CONSTRAINT [DF_GymMemberships_DurationMonths] DEFAULT 1;
                    END
                ");
                _gymSchemaEnsured = true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Gym schema ensure notice: {Message}", ex.Message);
            }
        }

        public async Task<GymSetting> GetGymSettingsAsync()
        {
            await EnsureGymSchemaAsync();

            var settings = await _dbContext.GymSettings
                .Include(s => s.UpdatedByUser)
                .FirstOrDefaultAsync();

            if (settings == null)
            {
                settings = new GymSetting
                {
                    GymName = "Adhunik Bari Fitness & Health Club",
                    OpeningHours = "6:00 AM - 10:00 PM (Daily)",
                    MonthlyFee = 500.00m,
                    EquipmentInformation = "Commercial Treadmills, Elliptical Cross-Trainers, Multi-Station Cable Machine, Olympic Barbells & Bumper Plates, Dumbbell Sets (2.5kg - 32kg), Adjustable Benches, Power Squat Rack, Kettlebells, Yoga Mats & Resistance Bands.",
                    RulesAndGuidelines = "1. Proper athletic footwear and clothing required at all times.\n2. Wipe down machines and equipment with sanitizing towels after each use.\n3. Return weights, dumbbells, and plates to designated racks.\n4. No food or open beverage containers allowed inside the facility.\n5. Cardio machine usage limited to 30 minutes during peak hours (6 PM - 9 PM).\n6. Non-resident guests are strictly prohibited without prior management clearance.\n7. Use equipment safely and report any malfunctioning machinery to building staff immediately.",
                    Location = "2nd Floor Amenities Area (Beside Community Center)",
                    ContactPhone = "+880 1712-345678",
                    IsActive = true,
                    UpdatedAt = DateTime.UtcNow
                };

                _dbContext.GymSettings.Add(settings);
                await _dbContext.SaveChangesAsync();
            }

            return settings;
        }

        public async Task<bool> UpdateGymSettingsAsync(GymSettingsViewModel model, string? managerUserId)
        {
            try
            {
                await EnsureGymSchemaAsync();

                var settings = await _dbContext.GymSettings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    settings = new GymSetting();
                    _dbContext.GymSettings.Add(settings);
                }

                settings.GymName = model.GymName.Trim();
                settings.OpeningHours = model.OpeningHours.Trim();
                settings.MonthlyFee = model.MonthlyFee;
                settings.EquipmentInformation = model.EquipmentInformation?.Trim();
                settings.RulesAndGuidelines = model.RulesAndGuidelines?.Trim();
                settings.Location = model.Location?.Trim();
                settings.ContactPhone = model.ContactPhone?.Trim();
                settings.IsActive = model.IsActive;
                settings.UpdatedAt = DateTime.UtcNow;
                settings.UpdatedByUserId = managerUserId;

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update gym settings");
                return false;
            }
        }

        public async Task<ResidentGymDashboardViewModel> GetResidentGymDashboardAsync(string userId)
        {
            await RunAutoExpiryCheckAsync();

            var settings = await GetGymSettingsAsync();

            var assignment = await _dbContext.FlatAssignments
                .Include(a => a.Flat)
                .Include(a => a.User)
                .Where(a => a.UserId == userId && a.IsActive)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            // Load all memberships associated with this resident
            var memberships = await _dbContext.GymMemberships
                .Include(m => m.User)
                .Include(m => m.Flat)
                .Include(m => m.ApprovedByUser)
                .Where(m => m.UserId == userId)
                .OrderByDescending(m => m.RequestedAt)
                .AsNoTracking()
                .ToListAsync();

            var itemVms = memberships.Select(MapToMembershipItem).ToList();

            var viewModel = new ResidentGymDashboardViewModel
            {
                GymSettings = MapToSettingsViewModel(settings),
                MyMemberships = itemVms,
                ResidentName = assignment?.User?.FullName ?? "Resident Member",
                FlatNumber = assignment?.Flat?.FlatNumber ?? "N/A",
                ResidentType = assignment?.ResidentType ?? "Resident"
            };

            return viewModel;
        }

        public async Task<(bool Success, string Message)> ApplyForGymMembershipAsync(string userId, GymApplicationRequest request)
        {
            try
            {
                await RunAutoExpiryCheckAsync();

                var assignment = await _dbContext.FlatAssignments
                    .Include(a => a.User)
                    .Where(a => a.UserId == userId && a.IsActive)
                    .FirstOrDefaultAsync();

                var memberName = !string.IsNullOrWhiteSpace(request.MemberName) 
                    ? request.MemberName.Trim() 
                    : (assignment?.User?.FullName ?? "Resident Member");

                // Prevent exact duplicate pending application for the same member name
                var existingPending = await _dbContext.GymMemberships
                    .Where(m => m.UserId == userId && 
                                m.Status == GymMembershipStatuses.Pending &&
                                (m.MemberName == memberName || (string.IsNullOrEmpty(m.MemberName) && memberName == assignment.User.FullName)))
                    .FirstOrDefaultAsync();

                if (existingPending != null)
                {
                    return (false, $"An application for {memberName} is already pending manager review.");
                }

                var settings = await GetGymSettingsAsync();

                var packageMonths = request.PackageMonths switch
                {
                    3 => 3,
                    6 => 6,
                    _ => 1
                };
                var packageFee = GymApplicationRequest.GetPackageFee(packageMonths);

                var membership = new GymMembership
                {
                    UserId = userId,
                    FlatId = assignment?.FlatId,
                    MemberName = memberName,
                    MemberRelation = !string.IsNullOrWhiteSpace(request.MemberRelation) ? request.MemberRelation.Trim() : "Self",
                    MemberPhone = !string.IsNullOrWhiteSpace(request.MemberPhone) ? request.MemberPhone.Trim() : assignment?.User?.PhoneNumber,
                    MemberGender = request.MemberGender,
                    MemberAge = request.MemberAge,
                    MemberPhotoUrl = request.MemberPhotoUrl,
                    Status = GymMembershipStatuses.Pending,
                    DurationMonths = packageMonths,
                    MonthlyFee = packageFee,
                    PaySlipAmount = packageFee,
                    RequestedAt = DateTime.UtcNow,
                    Notes = request.Notes?.Trim(),
                    IsFeePaid = false,
                    RenewalStatus = "None",
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.GymMemberships.Add(membership);
                await _dbContext.SaveChangesAsync();

                return (true, $"Gym membership application for {memberName} ({packageMonths} Month{(packageMonths > 1 ? "s" : "")} Pass — ৳{packageFee:N0}) submitted! Management will review and issue your pay slip.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply for gym membership for user {UserId}", userId);
                return (false, "An error occurred while submitting your application. Please try again.");
            }
        }

        public async Task<ManagerGymDashboardViewModel> GetManagerGymDashboardAsync()
        {
            await RunAutoExpiryCheckAsync();

            var settings = await GetGymSettingsAsync();

            var allMemberships = await _dbContext.GymMemberships
                .Include(m => m.User)
                .Include(m => m.Flat)
                .Include(m => m.ApprovedByUser)
                .OrderByDescending(m => m.RequestedAt)
                .AsNoTracking()
                .ToListAsync();

            var itemVms = allMemberships.Select(MapToMembershipItem).ToList();

            var pendingRequests = itemVms.Where(m => 
                (m.Status == GymMembershipStatuses.Pending && string.IsNullOrWhiteSpace(m.PaySlipNumber)) || 
                m.RenewalStatus == "RenewalPending").ToList();

            var pendingPaymentSlips = itemVms.Where(m => 
                !m.IsPaid && !string.IsNullOrWhiteSpace(m.PaySlipNumber)).ToList();

            var activeMembers = itemVms.Where(m => 
                m.Status == GymMembershipStatuses.Active && 
                m.IsPaid &&
                m.RenewalStatus != "RenewalPending").ToList();

            var expiredOrCancelled = itemVms.Where(m => 
                m.Status == GymMembershipStatuses.Expired || 
                m.Status == GymMembershipStatuses.Cancelled || 
                m.Status == GymMembershipStatuses.Rejected).ToList();

            return new ManagerGymDashboardViewModel
            {
                GymSettings = MapToSettingsViewModel(settings),
                PendingRequests = pendingRequests,
                PendingPaymentSlips = pendingPaymentSlips,
                ActiveMembers = activeMembers,
                ExpiredOrCancelledMembers = expiredOrCancelled
            };
        }

        public async Task<(bool Success, string Message)> ApproveMembershipAsync(string managerUserId, GymApprovalRequest request)
        {
            try
            {
                await EnsureGymSchemaAsync();

                var membership = await _dbContext.GymMemberships
                    .Include(m => m.User)
                    .Include(m => m.Flat)
                    .FirstOrDefaultAsync(m => m.GymMembershipId == request.GymMembershipId);

                if (membership == null)
                {
                    return (false, "Membership record not found.");
                }

                if (request.ExpiryDate <= request.StartDate)
                {
                    return (false, "Expiry date must be after the start date.");
                }

                var feeAmount = request.TotalFeeAmount > 0 ? request.TotalFeeAmount : membership.MonthlyFee;

                membership.Status = GymMembershipStatuses.Pending; // Stays Pending until resident pays the 500 fee slip
                membership.StartDate = request.StartDate.Date;
                membership.ExpiryDate = request.ExpiryDate.Date;
                membership.PaySlipAmount = feeAmount;
                membership.PaySlipNumber = $"GYM-SLIP-{membership.GymMembershipId:D5}";
                membership.PaySlipIssuedAt = DateTime.UtcNow;
                membership.CardNumber = $"AB-GYM-{membership.GymMembershipId:D5}";
                membership.VerificationCode = $"ABGYM-{membership.GymMembershipId:D5}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
                membership.ApprovedAt = DateTime.UtcNow;
                membership.ApprovedByUserId = managerUserId;
                membership.IsFeePaid = false; // Unpaid until resident pays 500 fee slip
                membership.FeePaidAt = null;
                membership.PaymentMethod = null;
                membership.PaymentTransactionId = null;
                membership.RenewalStatus = "None";
                membership.RenewalRequestedAt = null;
                membership.CancellationRequestedAt = null;
                membership.CancellationEffectiveDate = null;
                membership.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                var name = membership.DisplayMemberName;
                return (true, $"Application for {name} approved! Gym Pay Slip #{membership.PaySlipNumber} (৳{feeAmount:N0}) has been generated. Resident can now pay to unlock the official ID card.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to approve membership {MembershipId}", request.GymMembershipId);
                return (false, "An error occurred while approving the membership.");
            }
        }

        public async Task<(bool Success, string Message)> RejectMembershipAsync(string managerUserId, GymRejectionRequest request)
        {
            try
            {
                await EnsureGymSchemaAsync();

                var membership = await _dbContext.GymMemberships
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.GymMembershipId == request.GymMembershipId);

                if (membership == null)
                {
                    return (false, "Membership record not found.");
                }

                membership.Status = GymMembershipStatuses.Rejected;
                membership.RejectedAt = DateTime.UtcNow;
                membership.RejectionReason = request.RejectionReason.Trim();
                membership.RenewalStatus = "None";
                membership.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                return (true, $"Membership request for {membership.DisplayMemberName} was rejected.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reject membership {MembershipId}", request.GymMembershipId);
                return (false, "An error occurred while rejecting the request.");
            }
        }

        public async Task<GymPaySlipViewModel?> GetGymPaySlipDetailsAsync(int membershipId, string? requesterUserId, bool isManager)
        {
            await EnsureGymSchemaAsync();

            var membership = await _dbContext.GymMemberships
                .Include(m => m.User)
                .Include(m => m.Flat)
                .Include(m => m.ApprovedByUser)
                .FirstOrDefaultAsync(m => m.GymMembershipId == membershipId);

            if (membership == null)
            {
                return null;
            }

            if (!isManager && membership.UserId != requesterUserId)
            {
                return null;
            }

            var settings = await GetGymSettingsAsync();

            var assignment = await _dbContext.FlatAssignments
                .Include(a => a.Flat)
                .Where(a => a.UserId == membership.UserId && a.IsActive)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var flatNo = membership.Flat?.FlatNumber ?? assignment?.Flat?.FlatNumber ?? "N/A";
            var resType = assignment?.ResidentType ?? "Resident Member";

            var paySlip = new GymPaySlipViewModel
            {
                MembershipId = membership.GymMembershipId,
                PaySlipNumber = membership.FormattedPaySlipNumber,
                MemberName = membership.DisplayMemberName,
                ApplicantName = membership.User?.FullName ?? "Resident",
                FlatNumber = flatNo,
                ResidentType = resType,
                MemberRelation = membership.MemberRelation,
                MemberPhone = membership.DisplayMemberPhone,
                StartDate = membership.StartDate ?? membership.RequestedAt,
                ExpiryDate = membership.ExpiryDate ?? (membership.StartDate ?? membership.RequestedAt).AddMonths(membership.DurationMonths > 0 ? membership.DurationMonths : 1),
                DurationMonths = membership.DurationMonths > 0 ? membership.DurationMonths : 1,
                PaySlipAmount = membership.PaySlipAmount ?? membership.MonthlyFee,
                IsPaid = membership.IsFeePaid,
                FeePaidAt = membership.FeePaidAt,
                PaymentMethod = membership.PaymentMethod,
                PaymentTransactionId = membership.PaymentTransactionId,
                GymName = settings.GymName,
                IssuedAt = membership.PaySlipIssuedAt ?? membership.ApprovedAt ?? membership.RequestedAt,
                ApprovedByName = membership.ApprovedByUser?.FullName ?? "Building Management",
                CardNumber = membership.FormattedCardNumber,
                Notes = membership.Notes
            };

            var publishableKey = _configuration["Stripe:PublishableKey"] ?? string.Empty;
            paySlip.PublishableKey = publishableKey;

            if (!paySlip.IsPaid)
            {
                var stripeKey = _configuration["Stripe:SecretKey"]
                    ?? _configuration["STRIPE_SECRET_KEY"]
                    ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");

                if (!string.IsNullOrWhiteSpace(stripeKey))
                {
                    try
                    {
                        var intentService = new PaymentIntentService();
                        var amountInSmallestUnit = checked((long)Math.Round(paySlip.PaySlipAmount * 100m, MidpointRounding.AwayFromZero));
                        var intent = await intentService.CreateAsync(new PaymentIntentCreateOptions
                        {
                            Amount = amountInSmallestUnit,
                            Currency = "bdt",
                            ReceiptEmail = membership.User?.Email,
                            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                            Metadata = new Dictionary<string, string>
                            {
                                ["PaymentPurpose"] = "GymFee",
                                ["GymMembershipId"] = membership.GymMembershipId.ToString(),
                                ["PaySlipNumber"] = paySlip.PaySlipNumber,
                                ["MemberName"] = paySlip.MemberName,
                                ["UserId"] = membership.UserId
                            },
                            Description = $"ADHUNIK BARI - Gym Fee for {paySlip.MemberName} ({paySlip.PaySlipNumber})"
                        }, new RequestOptions { IdempotencyKey = $"gym-fee-{membership.GymMembershipId}-{DateTime.UtcNow:yyyyMM}" });

                        paySlip.ClientSecret = intent.ClientSecret;
                        paySlip.StripePaymentIntentId = intent.Id;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to create Stripe PaymentIntent for gym membership {MembershipId}: {Message}", membershipId, ex.Message);
                    }
                }
            }

            return paySlip;
        }

        public async Task<(bool Success, string Message)> ConfirmGymStripePaymentAsync(string userId, int membershipId, string? paymentIntentId)
        {
            try
            {
                await EnsureGymSchemaAsync();

                var membership = await _dbContext.GymMemberships
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.GymMembershipId == membershipId && m.UserId == userId);

                if (membership == null)
                {
                    return (false, "Gym membership record not found.");
                }

                if (membership.IsFeePaid)
                {
                    return (true, "Gym membership fee is already paid. Your ID Card is active.");
                }

                // Verify with Stripe if paymentIntentId is provided
                if (!string.IsNullOrWhiteSpace(paymentIntentId))
                {
                    try
                    {
                        var stripeKey = _configuration["Stripe:SecretKey"]
                            ?? _configuration["STRIPE_SECRET_KEY"]
                            ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");

                        if (!string.IsNullOrWhiteSpace(stripeKey))
                        {
                            var intent = await new PaymentIntentService().GetAsync(paymentIntentId);
                            if (intent != null && intent.Status != "succeeded")
                            {
                                return (false, $"Stripe payment status is '{intent.Status}'. Payment has not completed yet.");
                            }
                        }
                    }
                    catch (Exception stripeEx)
                    {
                        _logger.LogWarning(stripeEx, "Stripe verification warning: {Message}", stripeEx.Message);
                    }
                }

                membership.Status = GymMembershipStatuses.Active; // Automatically moves to Active member upon 500 taka payment
                membership.IsFeePaid = true;
                membership.FeePaidAt = DateTime.UtcNow;
                membership.PaymentMethod = "Stripe Card Payment";
                membership.PaymentTransactionId = !string.IsNullOrWhiteSpace(paymentIntentId)
                    ? paymentIntentId
                    : $"STRIPE-TXN-{DateTime.UtcNow:yyyyMMddHHmmss}";

                if (string.IsNullOrWhiteSpace(membership.CardNumber))
                {
                    membership.CardNumber = $"AB-GYM-{membership.GymMembershipId:D5}";
                }
                if (string.IsNullOrWhiteSpace(membership.VerificationCode))
                {
                    membership.VerificationCode = $"ABGYM-{membership.GymMembershipId:D5}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
                }
                membership.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return (true, $"Payment of ৳{(membership.PaySlipAmount ?? membership.MonthlyFee):N0} confirmed via Stripe for {membership.DisplayMemberName}! Your official Gym Member ID Card with QR code is ready.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to confirm Stripe payment for membership {MembershipId}", membershipId);
                return (false, "An error occurred while confirming Stripe payment.");
            }
        }

        public async Task<(bool Success, string Message)> PayGymPaySlipAsync(string userId, GymPaymentRequest request)
        {
            try
            {
                await EnsureGymSchemaAsync();

                var membership = await _dbContext.GymMemberships
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.GymMembershipId == request.MembershipId && m.UserId == userId);

                if (membership == null)
                {
                    return (false, "Gym membership or pay slip not found.");
                }

                if (membership.IsFeePaid)
                {
                    return (false, "This gym pay slip has already been paid.");
                }

                membership.Status = GymMembershipStatuses.Active; // Automatically moves to Active member upon payment
                membership.IsFeePaid = true;
                membership.FeePaidAt = DateTime.UtcNow;
                membership.PaymentMethod = request.PaymentMethod;
                membership.PaymentTransactionId = !string.IsNullOrWhiteSpace(request.TransactionId)
                    ? request.TransactionId.Trim()
                    : $"TXN-GYM-{DateTime.UtcNow:yyyyMMddHHmmss}";
                if (string.IsNullOrWhiteSpace(membership.CardNumber))
                {
                    membership.CardNumber = $"AB-GYM-{membership.GymMembershipId:D5}";
                }
                if (string.IsNullOrWhiteSpace(membership.VerificationCode))
                {
                    membership.VerificationCode = $"ABGYM-{membership.GymMembershipId:D5}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
                }
                membership.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return (true, $"Payment of ৳{(membership.PaySlipAmount ?? membership.MonthlyFee):N0} for {membership.DisplayMemberName} confirmed! Your official Gym Member ID Card with QR code is ready.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process payment for membership {MembershipId}", request.MembershipId);
                return (false, "An error occurred while processing payment.");
            }
        }

        public async Task<(bool Success, string Message)> MarkGymFeePaidAsync(string managerUserId, int membershipId, string? paymentMethod = "Cash to Management")
        {
            try
            {
                await EnsureGymSchemaAsync();

                var membership = await _dbContext.GymMemberships
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.GymMembershipId == membershipId);

                if (membership == null)
                {
                    return (false, "Membership record not found.");
                }

                membership.Status = GymMembershipStatuses.Active; // Automatically moves to Active member upon payment
                membership.IsFeePaid = true;
                membership.FeePaidAt = DateTime.UtcNow;
                membership.PaymentMethod = paymentMethod ?? "Cash to Management";
                membership.PaymentTransactionId = $"OFFLINE-MGR-{DateTime.UtcNow:yyyyMMddHHmm}";
                if (string.IsNullOrWhiteSpace(membership.CardNumber))
                {
                    membership.CardNumber = $"AB-GYM-{membership.GymMembershipId:D5}";
                }
                if (string.IsNullOrWhiteSpace(membership.VerificationCode))
                {
                    membership.VerificationCode = $"ABGYM-{membership.GymMembershipId:D5}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
                }
                membership.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return (true, $"Gym fee marked as paid for {membership.DisplayMemberName}. Official member ID card with QR code is ready for print.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark gym fee paid for membership {MembershipId}", membershipId);
                return (false, "Failed to mark fee as paid.");
            }
        }

        public async Task<GymIdCardViewModel?> GetGymIdCardDetailsAsync(int membershipId, string? requesterUserId, bool isManager, string? baseUrl = null)
        {
            await EnsureGymSchemaAsync();

            var membership = await _dbContext.GymMemberships
                .Include(m => m.User)
                .Include(m => m.Flat)
                .Include(m => m.ApprovedByUser)
                .FirstOrDefaultAsync(m => m.GymMembershipId == membershipId);

            if (membership == null)
            {
                return null;
            }

            // Authorization
            if (!isManager && membership.UserId != requesterUserId)
            {
                return null;
            }

            // Residents can only view card once active AND fee is paid
            if (!isManager && (membership.Status != GymMembershipStatuses.Active || !membership.IsFeePaid))
            {
                return null;
            }

            var settings = await GetGymSettingsAsync();

            var assignment = await _dbContext.FlatAssignments
                .Include(a => a.Flat)
                .Where(a => a.UserId == membership.UserId && a.IsActive)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var flatNo = membership.Flat?.FlatNumber ?? assignment?.Flat?.FlatNumber ?? "N/A";
            var resType = assignment?.ResidentType ?? "Resident Member";

            var code = membership.FormattedVerificationCode;
            var verUrl = !string.IsNullOrWhiteSpace(baseUrl)
                ? $"{baseUrl.TrimEnd('/')}/Gym/Verify?code={code}"
                : $"/Gym/Verify?code={code}";

            // Custom official QR code image provided by user
            var qrUrl = "/images/gym-qr-code.png";

            var card = new GymIdCardViewModel
            {
                MembershipId = membership.GymMembershipId,
                CardNumber = membership.FormattedCardNumber,
                MemberName = membership.DisplayMemberName,
                ApplicantName = membership.User?.FullName ?? "Resident",
                MemberRelation = membership.MemberRelation,
                MemberGender = membership.MemberGender,
                MemberAge = membership.MemberAge,
                MemberPhotoUrl = membership.MemberPhotoUrl,
                Email = membership.User?.Email,
                Phone = membership.DisplayMemberPhone,
                FlatNumber = flatNo,
                ResidentType = resType,
                Status = membership.Status,
                StartDate = membership.StartDate ?? membership.RequestedAt,
                ExpiryDate = membership.ExpiryDate ?? (membership.StartDate ?? membership.RequestedAt).AddMonths(1),
                GymName = settings.GymName,
                Location = !string.IsNullOrWhiteSpace(settings.Location) ? settings.Location : "Amenities Area (Floor 2)",
                OpeningHours = settings.OpeningHours,
                EmergencyPhone = settings.ContactPhone,
                ApprovedByName = membership.ApprovedByUser?.FullName ?? "Building Management",
                IssuedAt = membership.ApprovedAt ?? membership.RequestedAt,
                VerificationCode = code,
                VerificationUrl = "https://q.me-qr.com/y8mmqmuz",
                QrCodeUrl = qrUrl
            };

            return card;
        }

        public async Task<GymVerificationViewModel> VerifyGymPassAsync(string code)
        {
            await EnsureGymSchemaAsync();

            if (string.IsNullOrWhiteSpace(code))
            {
                return new GymVerificationViewModel
                {
                    IsValid = false,
                    StatusBadge = "Invalid",
                    Message = "No pass verification code provided."
                };
            }

            var cleanCode = code.Trim();

            var membership = await _dbContext.GymMemberships
                .Include(m => m.User)
                .Include(m => m.Flat)
                .Include(m => m.ApprovedByUser)
                .FirstOrDefaultAsync(m => m.VerificationCode == cleanCode || 
                                          m.CardNumber == cleanCode || 
                                          m.GymMembershipId.ToString() == cleanCode);

            if (membership == null)
            {
                return new GymVerificationViewModel
                {
                    IsValid = false,
                    StatusBadge = "Not Found",
                    Message = "Unrecognized or fraudulent Gym Pass. No matching record exists in the system."
                };
            }

            var settings = await GetGymSettingsAsync();

            var assignment = await _dbContext.FlatAssignments
                .Include(a => a.Flat)
                .Where(a => a.UserId == membership.UserId && a.IsActive)
                .AsNoTracking()
                .FirstOrDefaultAsync();

            var flatNo = membership.Flat?.FlatNumber ?? assignment?.Flat?.FlatNumber ?? "N/A";
            var expiry = membership.ExpiryDate ?? DateTime.UtcNow;
            var isExpired = expiry.Date < DateTime.UtcNow.Date;
            var daysRem = Math.Max(0, (int)(expiry.Date - DateTime.UtcNow.Date).TotalDays);

            if (!membership.IsFeePaid)
            {
                return new GymVerificationViewModel
                {
                    IsValid = false,
                    StatusBadge = "Payment Pending",
                    Message = $"Pass registered for {membership.DisplayMemberName} but fee payment has not been received.",
                    CardNumber = membership.FormattedCardNumber,
                    MemberName = membership.DisplayMemberName,
                    ApplicantName = membership.User?.FullName ?? "Resident",
                    FlatNumber = flatNo,
                    MemberRelation = membership.MemberRelation,
                    StartDate = membership.StartDate,
                    ExpiryDate = membership.ExpiryDate,
                    DaysRemaining = daysRem,
                    GymName = settings.GymName,
                    Location = settings.Location ?? "Amenities Area",
                    OpeningHours = settings.OpeningHours,
                    ApprovedByName = membership.ApprovedByUser?.FullName
                };
            }

            if (isExpired || membership.Status != GymMembershipStatuses.Active)
            {
                return new GymVerificationViewModel
                {
                    IsValid = false,
                    StatusBadge = isExpired ? "Expired" : membership.Status,
                    Message = $"This pass expired on {expiry:MMM dd, yyyy}. Renewal required for facility access.",
                    CardNumber = membership.FormattedCardNumber,
                    MemberName = membership.DisplayMemberName,
                    ApplicantName = membership.User?.FullName ?? "Resident",
                    FlatNumber = flatNo,
                    MemberRelation = membership.MemberRelation,
                    StartDate = membership.StartDate,
                    ExpiryDate = membership.ExpiryDate,
                    DaysRemaining = 0,
                    GymName = settings.GymName,
                    Location = settings.Location ?? "Amenities Area",
                    OpeningHours = settings.OpeningHours,
                    ApprovedByName = membership.ApprovedByUser?.FullName
                };
            }

            return new GymVerificationViewModel
            {
                IsValid = true,
                StatusBadge = "Active & Verified",
                Message = "Official Member Pass is Active and fully authorized for gym entry.",
                CardNumber = membership.FormattedCardNumber,
                MemberName = membership.DisplayMemberName,
                ApplicantName = membership.User?.FullName ?? "Resident",
                FlatNumber = flatNo,
                MemberRelation = membership.MemberRelation,
                StartDate = membership.StartDate,
                ExpiryDate = membership.ExpiryDate,
                DaysRemaining = daysRem,
                GymName = settings.GymName,
                Location = settings.Location ?? "Amenities Area",
                OpeningHours = settings.OpeningHours,
                ApprovedByName = membership.ApprovedByUser?.FullName
            };
        }

        public async Task<(bool Success, string Message)> RequestGymRenewalAsync(string userId, int membershipId)
        {
            try
            {
                await EnsureGymSchemaAsync();

                var membership = await _dbContext.GymMemberships
                    .FirstOrDefaultAsync(m => m.GymMembershipId == membershipId && m.UserId == userId);

                if (membership == null)
                {
                    return (false, "Membership not found or unauthorized.");
                }

                if (membership.RenewalStatus == "RenewalPending")
                {
                    return (false, "You already have a renewal request pending manager approval.");
                }

                membership.RenewalStatus = "RenewalPending";
                membership.RenewalRequestedAt = DateTime.UtcNow;
                membership.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                return (true, $"Renewal request for {membership.DisplayMemberName} submitted to manager for approval.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request gym renewal for membership {MembershipId}", membershipId);
                return (false, "Failed to request renewal. Please try again.");
            }
        }

        public async Task<(bool Success, string Message)> RequestGymCancellationAsync(string userId, GymCancellationRequest request)
        {
            try
            {
                await EnsureGymSchemaAsync();

                var membership = await _dbContext.GymMemberships
                    .FirstOrDefaultAsync(m => m.GymMembershipId == request.GymMembershipId && m.UserId == userId);

                if (membership == null)
                {
                    return (false, "Membership not found or unauthorized.");
                }

                if (membership.Status != GymMembershipStatuses.Active)
                {
                    return (false, "Only active memberships can be cancelled.");
                }

                DateTime effectiveDate;
                if (membership.ExpiryDate.HasValue && membership.ExpiryDate.Value.Date > DateTime.UtcNow.Date)
                {
                    effectiveDate = membership.ExpiryDate.Value.Date;
                }
                else
                {
                    var now = DateTime.UtcNow;
                    effectiveDate = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month));
                }

                membership.CancellationRequestedAt = DateTime.UtcNow;
                membership.CancellationEffectiveDate = effectiveDate;
                membership.CancellationReason = request.CancellationReason?.Trim();
                membership.RenewalStatus = "None";
                membership.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();

                return (true, $"Cancellation requested for {membership.DisplayMemberName}. Pass remains active until {effectiveDate:MMM dd, yyyy}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to request gym cancellation for membership {MembershipId}", request.GymMembershipId);
                return (false, "Failed to process cancellation request. Please try again.");
            }
        }

        public async Task<(bool Success, string Message)> CancelMembershipByManagerAsync(string managerUserId, int membershipId, string? reason)
        {
            try
            {
                await EnsureGymSchemaAsync();

                var membership = await _dbContext.GymMemberships
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.GymMembershipId == membershipId);

                if (membership == null)
                {
                    return (false, "Membership not found.");
                }

                membership.Status = GymMembershipStatuses.Cancelled;
                membership.CancellationRequestedAt = DateTime.UtcNow;
                membership.CancellationEffectiveDate = DateTime.UtcNow.Date;
                membership.CancellationReason = !string.IsNullOrWhiteSpace(reason) ? reason.Trim() : "Cancelled by building manager";
                membership.RenewalStatus = "None";
                membership.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                return (true, $"Membership for {membership.DisplayMemberName} has been cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel membership {MembershipId}", membershipId);
                return (false, "An error occurred while cancelling membership.");
            }
        }

        public async Task<(bool Success, string Message)> RenewMembershipByManagerAsync(string managerUserId, int membershipId, int durationMonths = 1)
        {
            try
            {
                await EnsureGymSchemaAsync();

                var membership = await _dbContext.GymMemberships
                    .Include(m => m.User)
                    .FirstOrDefaultAsync(m => m.GymMembershipId == membershipId);

                if (membership == null)
                {
                    return (false, "Membership not found.");
                }

                var baseDate = (membership.ExpiryDate.HasValue && membership.ExpiryDate.Value.Date > DateTime.UtcNow.Date)
                    ? membership.ExpiryDate.Value.Date
                    : DateTime.UtcNow.Date;

                var settings = await GetGymSettingsAsync();
                var months = Math.Max(1, durationMonths);
                var renewalFee = settings.MonthlyFee * months;

                membership.Status = GymMembershipStatuses.Active;
                membership.ExpiryDate = baseDate.AddMonths(months);
                membership.PaySlipAmount = renewalFee;
                membership.PaySlipNumber = $"GYM-SLIP-{membership.GymMembershipId:D5}-R";
                membership.PaySlipIssuedAt = DateTime.UtcNow;
                membership.IsFeePaid = false; // requires renewal payment
                membership.RenewalStatus = "None";
                membership.RenewalRequestedAt = null;
                membership.ApprovedAt = DateTime.UtcNow;
                membership.ApprovedByUserId = managerUserId;
                membership.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                return (true, $"Renewal pay slip issued for {membership.DisplayMemberName} (৳{renewalFee:N0} for {months} mo). Valid until {membership.ExpiryDate:MMM dd, yyyy} once paid.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to renew membership {MembershipId}", membershipId);
                return (false, "An error occurred while renewing membership.");
            }
        }

        private async Task RunAutoExpiryCheckAsync()
        {
            try
            {
                await EnsureGymSchemaAsync();

                var today = DateTime.UtcNow.Date;

                var expiredMemberships = await _dbContext.GymMemberships
                    .Where(m => m.Status == GymMembershipStatuses.Active && m.ExpiryDate != null && m.ExpiryDate.Value.Date < today)
                    .ToListAsync();

                foreach (var m in expiredMemberships)
                {
                    m.Status = GymMembershipStatuses.Expired;
                    m.UpdatedAt = DateTime.UtcNow;
                }

                var effectiveCancellations = await _dbContext.GymMemberships
                    .Where(m => m.Status == GymMembershipStatuses.Active && 
                           m.CancellationEffectiveDate != null && 
                           m.CancellationEffectiveDate.Value.Date < today)
                    .ToListAsync();

                foreach (var m in effectiveCancellations)
                {
                    m.Status = GymMembershipStatuses.Cancelled;
                    m.UpdatedAt = DateTime.UtcNow;
                }

                if (expiredMemberships.Count > 0 || effectiveCancellations.Count > 0)
                {
                    await _dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Auto expiry check warning: {Message}", ex.Message);
            }
        }

        private static GymSettingsViewModel MapToSettingsViewModel(GymSetting s)
        {
            return new GymSettingsViewModel
            {
                GymSettingId = s.GymSettingId,
                GymName = s.GymName,
                OpeningHours = s.OpeningHours,
                MonthlyFee = s.MonthlyFee,
                EquipmentInformation = s.EquipmentInformation,
                RulesAndGuidelines = s.RulesAndGuidelines,
                Location = s.Location,
                ContactPhone = s.ContactPhone,
                IsActive = s.IsActive,
                UpdatedAt = s.UpdatedAt,
                UpdatedByName = s.UpdatedByUser?.FullName
            };
        }

        private static GymMembershipItemViewModel MapToMembershipItem(GymMembership m)
        {
            return new GymMembershipItemViewModel
            {
                GymMembershipId = m.GymMembershipId,
                UserId = m.UserId,
                ResidentName = m.User?.FullName ?? "Resident",
                MemberName = m.DisplayMemberName,
                MemberRelation = m.MemberRelation,
                MemberPhone = m.DisplayMemberPhone,
                MemberGender = m.MemberGender,
                MemberAge = m.MemberAge,
                MemberPhotoUrl = m.MemberPhotoUrl,
                Email = m.User?.Email,
                Phone = m.User?.PhoneNumber,
                FlatNumber = m.Flat?.FlatNumber ?? "N/A",
                Status = m.Status,
                MonthlyFee = m.MonthlyFee,
                DurationMonths = m.DurationMonths > 0 ? m.DurationMonths : 1,
                PaySlipAmount = m.PaySlipAmount ?? m.MonthlyFee,
                PaySlipNumber = m.PaySlipNumber,
                RequestedAt = m.RequestedAt,
                StartDate = m.StartDate,
                ExpiryDate = m.ExpiryDate,
                ApprovedAt = m.ApprovedAt,
                ApprovedByName = m.ApprovedByUser?.FullName,
                RejectedAt = m.RejectedAt,
                RejectionReason = m.RejectionReason,
                CancellationRequestedAt = m.CancellationRequestedAt,
                CancellationEffectiveDate = m.CancellationEffectiveDate,
                CancellationReason = m.CancellationReason,
                RenewalStatus = m.RenewalStatus,
                RenewalRequestedAt = m.RenewalRequestedAt,
                Notes = m.Notes,
                IsFeePaid = m.IsFeePaid,
                FeePaidAt = m.FeePaidAt,
                PaymentMethod = m.PaymentMethod,
                CardNumber = m.CardNumber
            };
        }

        public async Task<(bool Success, string Message)> UpdateMemberPhotoAsync(string? requesterUserId, int membershipId, string photoUrl, bool isManager = false)
        {
            try
            {
                await EnsureGymSchemaAsync();

                var membership = await _dbContext.GymMemberships
                    .FirstOrDefaultAsync(m => m.GymMembershipId == membershipId);

                if (membership == null)
                {
                    return (false, "Membership not found.");
                }

                if (!isManager && membership.UserId != requesterUserId)
                {
                    return (false, "Unauthorized to update photo for this pass.");
                }

                membership.MemberPhotoUrl = photoUrl;
                membership.UpdatedAt = DateTime.UtcNow;

                await _dbContext.SaveChangesAsync();
                return (true, "Member photo updated successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update member photo for {MembershipId}", membershipId);
                return (false, "Failed to update member photo.");
            }
        }

        public async Task<int> GetPendingMembershipRequestsCountAsync()
        {
            try
            {
                await EnsureGymSchemaAsync();

                return await _dbContext.GymMemberships
                    .AsNoTracking()
                    .CountAsync(m => 
                        (m.Status == GymMembershipStatuses.Pending && (m.PaySlipNumber == null || m.PaySlipNumber == "")) || 
                        m.RenewalStatus == "RenewalPending");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to count pending gym membership requests.");
                return 0;
            }
        }

        public async Task<int> GetResidentPendingGymCountAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return 0;
            }

            try
            {
                await EnsureGymSchemaAsync();

                return await _dbContext.GymMemberships
                    .AsNoTracking()
                    .CountAsync(m => 
                        m.UserId == userId && 
                        ((m.Status == GymMembershipStatuses.Pending && !m.IsFeePaid) || m.RenewalStatus == "RenewalPending"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to count resident pending gym items for user {UserId}.", userId);
                return 0;
            }
        }
    }
}
