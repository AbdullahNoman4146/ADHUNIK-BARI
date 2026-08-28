using System.Data;
using System.Security.Cryptography;
using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace ADHUNIK_BARI.Services
{
    public class PropertyPaymentService : IPropertyPaymentService
    {
        private static readonly TimeSpan ReservationDuration = TimeSpan.FromMinutes(20);

        private readonly ApplicationDbContext _dbContext;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PropertyPaymentService> _logger;

        public PropertyPaymentService(
            ApplicationDbContext dbContext,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<PropertyPaymentService> logger)
        {
            _dbContext = dbContext;
            _userManager = userManager;
            _emailService = emailService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<PropertyCheckoutStartResult> StartCheckoutAsync(PropertyApplicationViewModel model)
        {
            await ReleaseExpiredReservationsAsync();

            if (!model.TermsAccepted)
            {
                return Fail("You must accept the advance-payment terms before continuing.");
            }

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            try
            {
                var listing = await _dbContext.PropertyListings
                    .Include(x => x.Flat)
                    .SingleOrDefaultAsync(x => x.PropertyListingId == model.PropertyListingId);

                if (listing == null || listing.ListingStatus != PropertyListingStatuses.Published ||
                    listing.Flat == null || listing.Flat.FlatStatus != "Available" ||
                    await _dbContext.FlatAssignments.AnyAsync(a => a.FlatId == listing.FlatId && a.IsActive))
                {
                    return Fail("This property is no longer available for checkout.");
                }

                if (listing.ListingType == PropertyListingTypes.ToLet)
                {
                    if (!model.NumberOfOccupants.HasValue || model.NumberOfOccupants.Value <= 0)
                        return Fail("Number of occupants is required for a To-Let application.");

                    if (!model.ExpectedMoveInDate.HasValue)
                        return Fail("Expected move-in date is required for a To-Let application.");

                    var existingUser = await _userManager.FindByEmailAsync(model.Email.Trim());
                    if (existingUser != null)
                        return Fail("An account already exists for this email. Please contact management instead of creating another resident account.");
                }

                var application = new PropertyApplication
                {
                    PropertyListingId = listing.PropertyListingId,
                    FullName = model.FullName.Trim(),
                    Email = model.Email.Trim(),
                    Phone = model.Phone.Trim(),
                    CurrentAddress = model.CurrentAddress.Trim(),
                    Profession = Normalize(model.Profession),
                    NumberOfOccupants = listing.ListingType == PropertyListingTypes.ToLet ? model.NumberOfOccupants : null,
                    ExpectedMoveInDate = listing.ListingType == PropertyListingTypes.ToLet ? model.ExpectedMoveInDate : null,
                    Message = Normalize(model.Message),
                    ApplicationType = listing.ListingType,
                    Status = PropertyApplicationStatuses.PaymentProcessing,
                    AdvanceAmount = listing.AdvanceAmount,
                    PaymentStatus = PropertyPaymentStatuses.Pending,
                    CreatedAt = DateTime.UtcNow,
                    ReservationExpiresAt = DateTime.UtcNow.Add(ReservationDuration)
                };

                _dbContext.PropertyApplications.Add(application);
                listing.ListingStatus = PropertyListingStatuses.CheckoutReserved;
                listing.UpdatedAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();

                var amount = ToStripeAmount(listing.AdvanceAmount);
                var intentService = new PaymentIntentService();
                var intent = await intentService.CreateAsync(
                    new PaymentIntentCreateOptions
                    {
                        Amount = amount,
                        Currency = "bdt",
                        ReceiptEmail = application.Email,
                        AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                        Metadata = new Dictionary<string, string>
                        {
                            ["PaymentPurpose"] = "PropertyAdvance",
                            ["PropertyListingId"] = listing.PropertyListingId.ToString(),
                            ["PropertyApplicationId"] = application.PropertyApplicationId.ToString(),
                            ["ListingType"] = listing.ListingType
                        }
                    },
                    new RequestOptions { IdempotencyKey = $"property-advance-{application.PropertyApplicationId}" });

                application.StripePaymentIntentId = intent.Id;
                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                return new PropertyCheckoutStartResult
                {
                    Success = true,
                    ApplicationId = application.PropertyApplicationId,
                    Message = "Application created. Complete the advance payment before the reservation expires."
                };
            }
            catch (DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Property checkout conflict for listing {ListingId}.", model.PropertyListingId);
                return Fail("Another applicant has just reserved this property. Please refresh the listing.");
            }
            catch (StripeException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Stripe could not create a property advance PaymentIntent.");
                return Fail("The payment session could not be created. Please try again.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Could not start property checkout for listing {ListingId}.", model.PropertyListingId);
                return Fail("The application could not be started. Please try again.");
            }
        }

        public async Task<PropertyCheckoutViewModel?> GetCheckoutAsync(int applicationId)
        {
            await ReleaseExpiredReservationsAsync();

            var application = await _dbContext.PropertyApplications
                .Include(x => x.PropertyListing)
                    .ThenInclude(x => x!.Flat)
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.PropertyApplicationId == applicationId);

            if (application?.PropertyListing?.Flat == null ||
                application.PaymentStatus == PropertyPaymentStatuses.Succeeded ||
                string.IsNullOrWhiteSpace(application.StripePaymentIntentId) ||
                !application.ReservationExpiresAt.HasValue || application.ReservationExpiresAt <= DateTime.UtcNow)
            {
                return null;
            }

            var intent = await new PaymentIntentService().GetAsync(application.StripePaymentIntentId);
            if (intent == null || string.IsNullOrWhiteSpace(intent.ClientSecret))
                return null;

            return new PropertyCheckoutViewModel
            {
                PropertyApplicationId = application.PropertyApplicationId,
                PropertyListingId = application.PropertyListingId,
                ListingType = application.ApplicationType,
                ListingTitle = application.PropertyListing.Title,
                FlatNumber = application.PropertyListing.Flat.FlatNumber,
                AdvanceAmount = application.AdvanceAmount,
                ClientSecret = intent.ClientSecret,
                StripePaymentIntentId = intent.Id,
                ReservationExpiresAt = application.ReservationExpiresAt,
                ApplicantEmail = application.Email,
                PublishableKey = _configuration["Stripe:PublishableKey"] ?? string.Empty
            };
        }

        public async Task<PropertyPaymentFinalizeResult> ConfirmPaymentAsync(int applicationId, string paymentIntentId)
        {
            var application = await _dbContext.PropertyApplications
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.PropertyApplicationId == applicationId);

            if (application == null || !string.Equals(application.StripePaymentIntentId, paymentIntentId, StringComparison.Ordinal))
                return FinalizeFail("Invalid property payment reference.");

            try
            {
                var intent = await new PaymentIntentService().GetAsync(paymentIntentId);
                if (intent == null || intent.Status != "succeeded")
                    return FinalizeFail("Stripe has not confirmed this payment as successful yet.");

                var success = await ProcessPaymentSuccessAsync(intent.Id, intent.Amount, intent.Metadata);
                return new PropertyPaymentFinalizeResult
                {
                    Success = success,
                    ApplicationId = applicationId,
                    Message = success ? "Advance payment confirmed successfully." : "The payment was received but finalization needs review. Please contact management."
                };
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe verification failed for property application {ApplicationId}.", applicationId);
                return FinalizeFail("Unable to verify the payment with Stripe.");
            }
        }

        public async Task<bool> ProcessPaymentSuccessAsync(string paymentIntentId, long amount, Dictionary<string, string> metadata)
        {
            if (!metadata.TryGetValue("PaymentPurpose", out var purpose) || purpose != "PropertyAdvance" ||
                !metadata.TryGetValue("PropertyApplicationId", out var applicationText) || !int.TryParse(applicationText, out var applicationId) ||
                !metadata.TryGetValue("PropertyListingId", out var listingText) || !int.TryParse(listingText, out var listingId))
            {
                _logger.LogWarning("Property PaymentIntent {PaymentIntentId} has invalid metadata.", paymentIntentId);
                return false;
            }

            PaymentIntent verifiedIntent;
            try
            {
                verifiedIntent = await new PaymentIntentService().GetAsync(paymentIntentId);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Could not verify Property PaymentIntent {PaymentIntentId}.", paymentIntentId);
                return false;
            }

            if (verifiedIntent.Status != "succeeded" || verifiedIntent.Currency != "bdt" || verifiedIntent.Amount != amount)
                return false;

            await using var transaction = await _dbContext.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            string? temporaryPassword = null;
            PropertyApplication? application = null;

            try
            {
                application = await _dbContext.PropertyApplications
                    .Include(x => x.PropertyListing)
                        .ThenInclude(x => x!.Flat)
                    .SingleOrDefaultAsync(x => x.PropertyApplicationId == applicationId && x.PropertyListingId == listingId);

                if (application?.PropertyListing?.Flat == null ||
                    !string.Equals(application.StripePaymentIntentId, paymentIntentId, StringComparison.Ordinal))
                {
                    await transaction.RollbackAsync();
                    return false;
                }

                if (application.PaymentStatus == PropertyPaymentStatuses.Succeeded)
                {
                    await transaction.CommitAsync();
                    return true;
                }

                var expectedAmount = ToStripeAmount(application.AdvanceAmount);
                if (amount != expectedAmount || verifiedIntent.Amount != expectedAmount)
                {
                    application.Status = PropertyApplicationStatuses.NeedsManualReview;
                    application.FailureReason = "Stripe amount did not match the server-side advance amount.";
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return false;
                }

                application.PaymentStatus = PropertyPaymentStatuses.Succeeded;
                application.PaidAt = DateTime.UtcNow;
                application.ReservationExpiresAt = null;

                var listing = application.PropertyListing;
                var flat = listing.Flat;

                if (application.ApplicationType == PropertyListingTypes.ForSale)
                {
                    application.Status = PropertyApplicationStatuses.SaleAwaitingOfflineCompletion;
                    listing.ListingStatus = PropertyListingStatuses.SaleReserved;
                    listing.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    await SendSaleConfirmationAsync(application, listing);
                    return true;
                }

                if (listing.ListingStatus != PropertyListingStatuses.CheckoutReserved || flat.FlatStatus != "Available" ||
                    await _dbContext.FlatAssignments.AnyAsync(a => a.FlatId == flat.FlatId && a.IsActive))
                {
                    application.Status = PropertyApplicationStatuses.NeedsManualReview;
                    application.FailureReason = "Payment succeeded, but the flat is no longer safe to auto-assign.";
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return false;
                }

                var existingUser = await _userManager.FindByEmailAsync(application.Email);
                if (existingUser != null)
                {
                    application.Status = PropertyApplicationStatuses.NeedsManualReview;
                    application.FailureReason = "Payment succeeded, but the applicant email already belongs to an Identity account.";
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return false;
                }

                temporaryPassword = GenerateTemporaryPassword();
                var tenant = new ApplicationUser
                {
                    FullName = application.FullName,
                    PhoneNumber = application.Phone,
                    Phone = application.Phone,
                    UserName = application.Email,
                    Email = application.Email,
                    EmailConfirmed = true,
                    TemporaryPasswordStatus = true,
                    AccountStatus = "Active",
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await _userManager.CreateAsync(tenant, temporaryPassword);
                if (!createResult.Succeeded)
                {
                    application.Status = PropertyApplicationStatuses.NeedsManualReview;
                    application.FailureReason = string.Join("; ", createResult.Errors.Select(e => e.Description));
                    await _dbContext.SaveChangesAsync();
                    await transaction.CommitAsync();
                    return false;
                }

                var roleResult = await _userManager.AddToRoleAsync(tenant, "Tenant");
                if (!roleResult.Succeeded)
                    throw new InvalidOperationException("Tenant role assignment failed: " + string.Join("; ", roleResult.Errors.Select(e => e.Description)));

                _dbContext.FlatAssignments.Add(new FlatAssignment
                {
                    FlatId = flat.FlatId,
                    UserId = tenant.Id,
                    ResidentType = "Tenant",
                    AssignmentDate = DateTime.UtcNow,
                    IsActive = true
                });

                flat.FlatStatus = "Occupied";
                listing.ListingStatus = PropertyListingStatuses.Rented;
                listing.UpdatedAt = DateTime.UtcNow;
                application.CreatedResidentUserId = tenant.Id;
                application.Status = PropertyApplicationStatuses.TenantCreated;

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                var emailSent = await SendTenantConfirmationAsync(application, listing, temporaryPassword);
                application.EmailSent = emailSent;
                application.EmailSentAt = emailSent ? DateTime.UtcNow : null;
                if (!emailSent)
                    application.FailureReason = "Tenant was created and assigned, but the credential email could not be sent. Manager must reset/send credentials manually.";

                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Failed to finalize property payment {PaymentIntentId}.", paymentIntentId);
                return false;
            }
        }

        public async Task ReleaseExpiredReservationsAsync()
        {
            var now = DateTime.UtcNow;
            var expired = await _dbContext.PropertyApplications
                .Include(x => x.PropertyListing)
                .Where(x => x.PaymentStatus == PropertyPaymentStatuses.Pending &&
                            x.ReservationExpiresAt.HasValue && x.ReservationExpiresAt <= now &&
                            x.Status == PropertyApplicationStatuses.PaymentProcessing)
                .ToListAsync();

            if (expired.Count == 0)
                return;

            var succeededIntents = new List<(string IntentId, long Amount, Dictionary<string, string> Metadata)>();

            foreach (var application in expired)
            {
                if (!string.IsNullOrWhiteSpace(application.StripePaymentIntentId))
                {
                    try
                    {
                        var intent = await new PaymentIntentService().GetAsync(application.StripePaymentIntentId);
                        if (intent.Status == "succeeded")
                        {
                            succeededIntents.Add((intent.Id, intent.Amount, intent.Metadata));
                            continue;
                        }

                        if (intent.Status != "canceled")
                            await new PaymentIntentService().CancelAsync(intent.Id, new PaymentIntentCancelOptions());
                    }
                    catch (StripeException ex)
                    {
                        // Do not reopen the flat while Stripe status is unknown. A later request can retry safely.
                        _logger.LogWarning(ex, "Could not verify/cancel expired Property PaymentIntent {PaymentIntentId}; reservation remains closed for safety.", application.StripePaymentIntentId);
                        continue;
                    }
                }

                application.Status = PropertyApplicationStatuses.Cancelled;
                application.PaymentStatus = PropertyPaymentStatuses.Cancelled;
                application.ReservationExpiresAt = null;

                if (application.PropertyListing?.ListingStatus == PropertyListingStatuses.CheckoutReserved)
                {
                    application.PropertyListing.ListingStatus = PropertyListingStatuses.Published;
                    application.PropertyListing.UpdatedAt = now;
                }
            }

            await _dbContext.SaveChangesAsync();

            foreach (var succeeded in succeededIntents)
                await ProcessPaymentSuccessAsync(succeeded.IntentId, succeeded.Amount, succeeded.Metadata);
        }

        private async Task SendSaleConfirmationAsync(PropertyApplication application, PropertyListing listing)
        {
            var body = $"<h2>Advance payment received</h2><p>Thank you, {Html(application.FullName)}.</p>" +
                       $"<p>We received your advance payment of <strong>BDT {application.AdvanceAmount:N2}</strong> for {Html(listing.Title)}.</p>" +
                       "<p>The property has been reserved and removed from public availability. Please contact ADHUNIK BARI management to complete the legal/offline sale process.</p>";
            var sent = await _emailService.SendAsync(application.Email, "ADHUNIK BARI - Flat sale reservation confirmed", body);
            application.EmailSent = sent;
            application.EmailSentAt = sent ? DateTime.UtcNow : null;
            if (!sent)
                application.FailureReason = "Sale reservation succeeded, but confirmation email could not be sent.";
            await _dbContext.SaveChangesAsync();
        }

        private async Task<bool> SendTenantConfirmationAsync(PropertyApplication application, PropertyListing listing, string temporaryPassword)
        {
            var loginUrl = _configuration["App:LoginUrl"] ?? "/Account/Login";
            var body = $"<h2>Your ADHUNIK BARI tenant account is ready</h2><p>Hello {Html(application.FullName)},</p>" +
                       $"<p>Your advance payment of <strong>BDT {application.AdvanceAmount:N2}</strong> for {Html(listing.Title)} has been confirmed.</p>" +
                       $"<p><strong>Login email:</strong> {Html(application.Email)}<br/><strong>Temporary password:</strong> {Html(temporaryPassword)}</p>" +
                       $"<p>Login at {Html(loginUrl)} and change the temporary password after signing in.</p>";
            return await _emailService.SendAsync(application.Email, "ADHUNIK BARI - Payment confirmed and tenant account created", body);
        }

        private static string GenerateTemporaryPassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string special = "!@#$%";
            const string all = upper + lower + digits + special;

            var chars = new List<char>
            {
                upper[RandomNumberGenerator.GetInt32(upper.Length)],
                lower[RandomNumberGenerator.GetInt32(lower.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                special[RandomNumberGenerator.GetInt32(special.Length)]
            };

            while (chars.Count < 14)
                chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

            for (var i = chars.Count - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars.ToArray());
        }

        private static long ToStripeAmount(decimal amount) => checked((long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero));
        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        private static string Html(string value) => System.Net.WebUtility.HtmlEncode(value);
        private static PropertyCheckoutStartResult Fail(string message) => new() { Success = false, Message = message };
        private static PropertyPaymentFinalizeResult FinalizeFail(string message) => new() { Success = false, Message = message };
    }
}
