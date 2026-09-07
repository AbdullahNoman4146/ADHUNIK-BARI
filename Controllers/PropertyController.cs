using System.Security.Cryptography;
using Stripe;
using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.Services;
using ADHUNIK_BARI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ADHUNIK_BARI.Controllers
{
    [AllowAnonymous]
    public class PropertyController : Controller
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IPropertyPaymentService propertyPaymentService;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IEmailService emailService;
        private readonly IConfiguration configuration;
        private readonly ILogger<PropertyController> logger;

        public PropertyController(
            ApplicationDbContext dbContext,
            IPropertyPaymentService propertyPaymentService,
            UserManager<ApplicationUser> userManager,
            IEmailService emailService,
            IConfiguration configuration,
            ILogger<PropertyController> logger)
        {
            this.dbContext = dbContext;
            this.propertyPaymentService = propertyPaymentService;
            this.userManager = userManager;
            this.emailService = emailService;
            this.configuration = configuration;
            this.logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? type)
        {
            await propertyPaymentService.ReleaseExpiredReservationsAsync();
            var selectedType = NormalizeListingType(type);
            var listings = new List<PublicPropertyListingCardViewModel>();

            // Load flat listings unless filtered strictly for Parking
            if (selectedType != "Parking")
            {
                var query = GetPublicListingQuery();

                if (selectedType != null)
                    query = query.Where(listing => listing.ListingType == selectedType);

                listings = await query
                    .OrderByDescending(listing => listing.PublishedAt ?? listing.CreatedAt)
                    .Select(listing => new PublicPropertyListingCardViewModel
                    {
                        PropertyListingId = listing.PropertyListingId,
                        ListingType = listing.ListingType,
                        Title = listing.Title,
                        ShortDescription = listing.ShortDescription,
                        FlatNumber = listing.Flat!.FlatNumber,
                        FloorNumber = listing.Flat.FloorNumber,
                        Price = listing.Price,
                        AdvanceAmount = listing.AdvanceAmount,
                        Bedrooms = listing.Bedrooms,
                        Bathrooms = listing.Bathrooms,
                        AreaSqFt = listing.AreaSqFt,
                        CoverImagePath = listing.CoverImagePath,
                        RoomLayoutImagePath = listing.RoomLayoutImagePath,
                        PublishedAt = listing.PublishedAt,
                        IsParking = false
                    })
                    .ToListAsync();
            }

            // Load parking spots listed for To-Let or For Sale
            var parkingQuery = dbContext.ParkingSpots
                .Include(s => s.Floor)
                .AsNoTracking()
                .Where(s => s.Status == "ForSale" || s.Status == "ToLet");

            if (selectedType == PropertyListingTypes.ToLet)
            {
                parkingQuery = parkingQuery.Where(s => s.Status == "ToLet");
            }
            else if (selectedType == PropertyListingTypes.ForSale)
            {
                parkingQuery = parkingQuery.Where(s => s.Status == "ForSale");
            }

            var spots = await parkingQuery
                .OrderBy(s => s.SpotNumber)
                .ToListAsync();

            var parkingCards = spots.Select(s => new PublicPropertyListingCardViewModel
            {
                PropertyListingId = 0,
                ParkingSpotId = s.ParkingSpotId,
                IsParking = true,
                SpotNumber = s.SpotNumber,
                FloorName = s.Floor?.FloorName ?? "Basement",
                VehicleType = s.ParkingType ?? "Car",
                ListingType = s.Status == "ForSale" ? PropertyListingTypes.ForSale : PropertyListingTypes.ToLet,
                Title = $"Parking Space {s.SpotNumber} · {s.Floor?.FloorName ?? "Basement"}",
                ShortDescription = !string.IsNullOrWhiteSpace(s.ListingNotes)
                    ? s.ListingNotes
                    : $"Dedicated {s.ParkingType ?? "Car"} parking bay on {s.Floor?.FloorName ?? "Basement"}. 24/7 CCTV surveillance & secure access.",
                FlatNumber = s.SpotNumber,
                FloorNumber = 0,
                Price = s.ListingPrice ?? s.ParkingFee,
                AdvanceAmount = s.Status == "ForSale" ? Math.Min(s.ListingPrice ?? 25000, 25000) : (s.ListingPrice ?? s.ParkingFee),
                PublishedAt = s.CreatedAt
            }).ToList();

            var allListings = listings.Concat(parkingCards).ToList();

            return View(new PublicPropertyListingsViewModel
            {
                SelectedType = selectedType,
                Listings = allListings
            });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            await propertyPaymentService.ReleaseExpiredReservationsAsync();
            var listing = await LoadPublicDetailsAsync(id);
            return listing == null ? NotFound() : View(listing);
        }

        [HttpGet]
        public async Task<IActionResult> Apply(int id)
        {
            await propertyPaymentService.ReleaseExpiredReservationsAsync();
            var listing = await LoadPublicDetailsAsync(id);
            if (listing == null)
                return NotFound();

            ViewBag.Listing = listing;
            return View(new PropertyApplicationViewModel { PropertyListingId = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Apply(PropertyApplicationViewModel model)
        {
            await propertyPaymentService.ReleaseExpiredReservationsAsync();
            var listing = await LoadPublicDetailsAsync(model.PropertyListingId);
            if (listing == null)
            {
                ModelState.AddModelError(string.Empty, "This property is no longer available.");
                return View(model);
            }

            if (listing.ListingType == PropertyListingTypes.ToLet)
            {
                if (!model.NumberOfOccupants.HasValue || model.NumberOfOccupants <= 0)
                    ModelState.AddModelError(nameof(model.NumberOfOccupants), "Number of occupants is required for To-Let.");
                if (!model.ExpectedMoveInDate.HasValue)
                    ModelState.AddModelError(nameof(model.ExpectedMoveInDate), "Expected move-in date is required for To-Let.");
            }

            if (!model.TermsAccepted)
                ModelState.AddModelError(nameof(model.TermsAccepted), "You must accept the advance-payment terms.");

            ViewBag.Listing = listing;
            if (!ModelState.IsValid)
                return View(model);

            var result = await propertyPaymentService.StartCheckoutAsync(model);
            if (!result.Success || !result.ApplicationId.HasValue)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                var refreshed = await LoadPublicDetailsAsync(model.PropertyListingId);
                ViewBag.Listing = refreshed ?? listing;
                return View(model);
            }

            return RedirectToAction("Checkout", "PropertyPayment", new { id = result.ApplicationId.Value });
        }

        private async Task<PublicPropertyListingDetailsViewModel?> LoadPublicDetailsAsync(int id)
        {
            return await GetPublicListingQuery()
                .Where(item => item.PropertyListingId == id)
                .Select(item => new PublicPropertyListingDetailsViewModel
                {
                    PropertyListingId = item.PropertyListingId,
                    ListingType = item.ListingType,
                    Title = item.Title,
                    ShortDescription = item.ShortDescription,
                    Description = item.Description,
                    FlatNumber = item.Flat!.FlatNumber,
                    FloorNumber = item.Flat.FloorNumber,
                    Price = item.Price,
                    AdvanceAmount = item.AdvanceAmount,
                    Bedrooms = item.Bedrooms,
                    Bathrooms = item.Bathrooms,
                    Balconies = item.Balconies,
                    AreaSqFt = item.AreaSqFt,
                    FurnishingStatus = item.FurnishingStatus,
                    Facing = item.Facing,
                    Features = item.Features,
                    CoverImagePath = item.CoverImagePath,
                    RoomLayoutImagePath = item.RoomLayoutImagePath,
                    PublishedAt = item.PublishedAt
                })
                .SingleOrDefaultAsync();
        }

        private IQueryable<PropertyListing> GetPublicListingQuery()
        {
            return dbContext.PropertyListings
                .AsNoTracking()
                .Where(listing =>
                    listing.ListingStatus == PropertyListingStatuses.Published &&
                    listing.Flat != null &&
                    listing.Flat.FlatStatus == "Available" &&
                    !dbContext.FlatAssignments.Any(assignment => assignment.FlatId == listing.FlatId && assignment.IsActive));
        }

        [HttpGet]
        public async Task<IActionResult> ParkingDetails(int id)
        {
            var spot = await dbContext.ParkingSpots
                .Include(s => s.Floor)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ParkingSpotId == id && (s.Status == "ForSale" || s.Status == "ToLet"));

            if (spot == null)
            {
                TempData["Error"] = "This parking space is no longer available on the marketplace.";
                return RedirectToAction(nameof(Index), new { type = "Parking" });
            }

            var viewModel = new PublicParkingDetailsViewModel
            {
                ParkingSpotId = spot.ParkingSpotId,
                SpotNumber = spot.SpotNumber,
                FloorName = spot.Floor?.FloorName ?? "Basement",
                FloorCode = spot.Floor?.FloorCode ?? "B",
                Status = spot.Status,
                ListingType = spot.Status == "ForSale" ? PropertyListingTypes.ForSale : PropertyListingTypes.ToLet,
                VehicleType = spot.ParkingType ?? "Car",
                Price = spot.ListingPrice ?? spot.ParkingFee,
                AdvanceAmount = spot.Status == "ForSale" ? Math.Min(spot.ListingPrice ?? 25000, 25000) : (spot.ListingPrice ?? spot.ParkingFee),
                ListingNotes = spot.ListingNotes,
                PublishedAt = spot.CreatedAt
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ApplyParking(int id)
        {
            var spot = await dbContext.ParkingSpots
                .Include(s => s.Floor)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ParkingSpotId == id && (s.Status == "ForSale" || s.Status == "ToLet"));

            if (spot == null)
            {
                TempData["Error"] = "This parking space is no longer available.";
                return RedirectToAction(nameof(Index), new { type = "Parking" });
            }

            var model = new ApplyParkingViewModel
            {
                ParkingSpotId = spot.ParkingSpotId,
                SpotNumber = spot.SpotNumber,
                FloorName = spot.Floor?.FloorName ?? "Basement",
                ListingType = spot.Status == "ForSale" ? PropertyListingTypes.ForSale : PropertyListingTypes.ToLet,
                Price = spot.ListingPrice ?? spot.ParkingFee,
                AdvanceAmount = spot.Status == "ForSale" ? Math.Min(spot.ListingPrice ?? 25000, 25000) : (spot.ListingPrice ?? spot.ParkingFee),
                VehicleType = spot.ParkingType ?? "Car",
                ExpectedStartDate = DateTime.Today.AddDays(1)
            };

            if (User.Identity?.IsAuthenticated == true)
            {
                var currentUser = await userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    model.FullName = currentUser.FullName ?? string.Empty;
                    model.Email = currentUser.Email ?? string.Empty;
                    model.Phone = currentUser.PhoneNumber ?? currentUser.Phone ?? string.Empty;

                    var assignment = await dbContext.FlatAssignments
                        .Include(a => a.Flat)
                        .Where(a => a.UserId == currentUser.Id && a.IsActive)
                        .FirstOrDefaultAsync();

                    if (assignment?.Flat != null)
                    {
                        model.FlatId = assignment.FlatId;
                        model.SelectedFlatNumber = assignment.Flat.FlatNumber;
                        model.IsLoggedInResident = true;
                        model.ResidentRole = assignment.ResidentType;
                    }
                }
            }

            model.AvailableFlats = await GetFlatSelectListAsync();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyParking(ApplyParkingViewModel model)
        {
            var spot = await dbContext.ParkingSpots
                .Include(s => s.Floor)
                .FirstOrDefaultAsync(s => s.ParkingSpotId == model.ParkingSpotId);

            if (spot == null || (spot.Status != "ForSale" && spot.Status != "ToLet"))
            {
                TempData["Error"] = "This parking space is no longer available for booking.";
                return RedirectToAction(nameof(Index), new { type = "Parking" });
            }

            model.SpotNumber = spot.SpotNumber;
            model.FloorName = spot.Floor?.FloorName ?? "Basement";
            model.ListingType = spot.Status == "ForSale" ? PropertyListingTypes.ForSale : PropertyListingTypes.ToLet;
            model.Price = spot.ListingPrice ?? spot.ParkingFee;
            model.VehicleType = spot.ParkingType ?? "Car";

            ApplicationUser? currentUser = null;
            if (User.Identity?.IsAuthenticated == true)
            {
                currentUser = await userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    var assignment = await dbContext.FlatAssignments
                        .Include(a => a.Flat)
                        .Where(a => a.UserId == currentUser.Id && a.IsActive)
                        .FirstOrDefaultAsync();

                    if (assignment?.Flat != null)
                    {
                        model.FlatId = assignment.FlatId;
                        model.SelectedFlatNumber = assignment.Flat.FlatNumber;
                        model.IsLoggedInResident = true;
                        model.ResidentRole = assignment.ResidentType;
                    }
                }
            }

            if (!model.TermsAccepted)
            {
                ModelState.AddModelError(nameof(model.TermsAccepted), "You must accept the terms before proceeding to payment.");
            }

            Flat? flat = null;
            if (model.FlatId.HasValue && model.FlatId.Value > 0)
            {
                flat = await dbContext.Flats.FirstOrDefaultAsync(f => f.FlatId == model.FlatId.Value);
                if (flat == null)
                {
                    ModelState.AddModelError(nameof(model.FlatId), "The selected flat does not exist.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.AvailableFlats = await GetFlatSelectListAsync();
                return View(model);
            }

            bool isOutsider = flat == null;
            var advanceAmount = spot.Status == "ForSale" 
                ? Math.Min(spot.ListingPrice ?? 25000, 25000) 
                : (spot.ListingPrice ?? spot.ParkingFee);

            var application = new ParkingApplication
            {
                ParkingSpotId = spot.ParkingSpotId,
                FlatId = flat?.FlatId,
                FullName = model.FullName.Trim(),
                Email = model.Email.Trim(),
                Phone = model.Phone.Trim(),
                VehicleType = model.VehicleType,
                VehicleRegNumber = model.VehicleRegNumber?.Trim(),
                Notes = model.Notes?.Trim(),
                ApplicationType = spot.Status == "ForSale" ? PropertyListingTypes.ForSale : PropertyListingTypes.ToLet,
                Status = PropertyApplicationStatuses.PaymentProcessing,
                AdvanceAmount = advanceAmount,
                PaymentStatus = PropertyPaymentStatuses.Pending,
                CreatedAt = DateTime.UtcNow,
                ReservationExpiresAt = DateTime.UtcNow.AddMinutes(20),
                CreatedUserId = currentUser?.Id,
                IsOutsider = isOutsider
            };

            dbContext.ParkingApplications.Add(application);
            await dbContext.SaveChangesAsync();

            // Create Stripe PaymentIntent or fallback safely for local development simulation
            string intentId = string.Empty;
            try
            {
                var stripeKey = configuration["Stripe:SecretKey"] 
                    ?? configuration["STRIPE_SECRET_KEY"] 
                    ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");

                if (!string.IsNullOrWhiteSpace(stripeKey))
                {
                    var amount = checked((long)Math.Round(advanceAmount * 100m, MidpointRounding.AwayFromZero));
                    var intentService = new PaymentIntentService();
                    var intent = await intentService.CreateAsync(new PaymentIntentCreateOptions
                    {
                        Amount = amount,
                        Currency = "bdt",
                        ReceiptEmail = application.Email,
                        AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions { Enabled = true },
                        Metadata = new Dictionary<string, string>
                        {
                            ["PaymentPurpose"] = "ParkingAdvance",
                            ["ParkingApplicationId"] = application.ParkingApplicationId.ToString(),
                            ["ParkingSpotId"] = spot.ParkingSpotId.ToString(),
                            ["IsOutsider"] = isOutsider.ToString()
                        }
                    }, new RequestOptions { IdempotencyKey = $"parking-adv-{application.ParkingApplicationId}" });

                    intentId = intent.Id;
                }
                else
                {
                    intentId = $"sim_pi_{Guid.NewGuid():N}";
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Stripe intent creation failed. Falling back to local test simulation for dev.");
                intentId = $"sim_pi_{Guid.NewGuid():N}";
            }

            application.StripePaymentIntentId = intentId;
            await dbContext.SaveChangesAsync();

            return RedirectToAction(nameof(ParkingCheckout), new { id = application.ParkingApplicationId });
        }

        [HttpGet]
        public async Task<IActionResult> ParkingCheckout(int id)
        {
            var application = await dbContext.ParkingApplications
                .Include(a => a.ParkingSpot)
                    .ThenInclude(p => p!.Floor)
                .Include(a => a.Flat)
                .FirstOrDefaultAsync(a => a.ParkingApplicationId == id);

            if (application == null || application.ParkingSpot == null)
            {
                TempData["Error"] = "This parking checkout session is invalid or has expired.";
                return RedirectToAction(nameof(Index), new { type = "Parking" });
            }

            if (application.PaymentStatus == PropertyPaymentStatuses.Succeeded)
            {
                return RedirectToAction(nameof(ParkingBookingSuccess), new { id = application.ParkingApplicationId });
            }

            if (application.ReservationExpiresAt.HasValue && application.ReservationExpiresAt.Value <= DateTime.UtcNow)
            {
                TempData["Error"] = "This parking reservation session has expired. Please apply again.";
                return RedirectToAction(nameof(ParkingDetails), new { id = application.ParkingSpotId });
            }

            string clientSecret = string.Empty;
            bool isSimulated = false;

            if (!string.IsNullOrWhiteSpace(application.StripePaymentIntentId))
            {
                if (application.StripePaymentIntentId.StartsWith("sim_pi_"))
                {
                    isSimulated = true;
                    clientSecret = $"sim_secret_{Guid.NewGuid():N}";
                }
                else
                {
                    try
                    {
                        var intent = await new PaymentIntentService().GetAsync(application.StripePaymentIntentId);
                        clientSecret = intent?.ClientSecret ?? string.Empty;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Could not retrieve Stripe client secret. Enabling simulated payment fallback.");
                        isSimulated = true;
                        clientSecret = $"sim_secret_{Guid.NewGuid():N}";
                    }
                }
            }
            else
            {
                isSimulated = true;
                clientSecret = $"sim_secret_{Guid.NewGuid():N}";
            }

            var viewModel = new ParkingCheckoutViewModel
            {
                ParkingApplicationId = application.ParkingApplicationId,
                ParkingSpotId = application.ParkingSpotId,
                SpotNumber = application.ParkingSpot.SpotNumber,
                FloorName = application.ParkingSpot.Floor?.FloorName ?? "Basement",
                ListingType = application.ApplicationType,
                AdvanceAmount = application.AdvanceAmount,
                ClientSecret = clientSecret,
                StripePaymentIntentId = application.StripePaymentIntentId ?? "",
                ReservationExpiresAt = application.ReservationExpiresAt,
                ApplicantName = application.FullName,
                ApplicantEmail = application.Email,
                ApplicantPhone = application.Phone,
                VehicleType = application.VehicleType,
                VehicleRegNumber = application.VehicleRegNumber,
                IsOutsider = application.IsOutsider,
                FlatNumber = application.Flat?.FlatNumber,
                PublishableKey = configuration["Stripe:PublishableKey"] ?? string.Empty,
                IsSimulatedDevPayment = isSimulated
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmParkingPayment(int applicationId, string paymentIntentId)
        {
            if (applicationId <= 0 || string.IsNullOrWhiteSpace(paymentIntentId))
            {
                return BadRequest(new { success = false, message = "Invalid payment confirmation request." });
            }

            var application = await dbContext.ParkingApplications
                .Include(a => a.ParkingSpot)
                    .ThenInclude(p => p!.Floor)
                .Include(a => a.Flat)
                .FirstOrDefaultAsync(a => a.ParkingApplicationId == applicationId);

            if (application == null || application.ParkingSpot == null)
            {
                return BadRequest(new { success = false, message = "Application not found." });
            }

            if (application.PaymentStatus == PropertyPaymentStatuses.Succeeded)
            {
                return Ok(new
                {
                    success = true,
                    redirectUrl = Url.Action(nameof(ParkingBookingSuccess), new { id = application.ParkingApplicationId })
                });
            }

            // If real Stripe PaymentIntent, verify status with Stripe
            if (!paymentIntentId.StartsWith("sim_pi_"))
            {
                try
                {
                    var intent = await new PaymentIntentService().GetAsync(paymentIntentId);
                    if (intent == null || intent.Status != "succeeded")
                    {
                        return BadRequest(new { success = false, message = "Stripe has not confirmed this payment as successful yet." });
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Stripe payment verification failed for application {ApplicationId}.", applicationId);
                    return BadRequest(new { success = false, message = "Payment verification with Stripe could not be completed." });
                }
            }

            var spot = application.ParkingSpot;
            string? tempPassword = null;

            await using var tx = await dbContext.Database.BeginTransactionAsync();
            try
            {
                application.PaymentStatus = PropertyPaymentStatuses.Succeeded;
                application.PaidAt = DateTime.UtcNow;
                application.Status = "Completed";
                application.StripePaymentIntentId = paymentIntentId;
                application.ReservationExpiresAt = null;

                spot.Status = "Assigned";
                spot.IsAvailable = false;

                var regInfo = !string.IsNullOrWhiteSpace(application.VehicleRegNumber) ? $" (Vehicle Reg: {application.VehicleRegNumber})" : "";
                var notesInfo = !string.IsNullOrWhiteSpace(application.Notes) ? $" Notes: {application.Notes}" : "";
                var targetInfo = application.Flat != null ? $"Flat {application.Flat.FlatNumber}" : "External Member / Outsider";

                spot.ListingNotes = $"Buyer: {application.FullName} ({application.Phone}){regInfo}{notesInfo}";

                if (application.IsOutsider)
                {
                    tempPassword = GenerateTemporaryPassword();

                    var existingUser = await userManager.FindByEmailAsync(application.Email);
                    if (existingUser == null)
                    {
                        var newUser = new ApplicationUser
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

                        var createRes = await userManager.CreateAsync(newUser, tempPassword);
                        if (!createRes.Succeeded)
                        {
                            logger.LogError("Could not create outsider account: {Errors}", string.Join(", ", createRes.Errors.Select(e => e.Description)));
                        }
                        else
                        {
                            await userManager.AddToRoleAsync(newUser, "ParkingUser");
                            application.CreatedUserId = newUser.Id;
                            spot.AssignedUserId = newUser.Id;
                        }
                    }
                    else
                    {
                        // Existing user booking as an outsider:
                        // 1. Reset password to tempPassword so credentials in email are valid
                        var resetToken = await userManager.GeneratePasswordResetTokenAsync(existingUser);
                        var resetRes = await userManager.ResetPasswordAsync(existingUser, resetToken, tempPassword);
                        if (!resetRes.Succeeded)
                        {
                            logger.LogWarning("Could not reset password for existing outsider user {Email}: {Errors}", existingUser.Email, string.Join(", ", resetRes.Errors.Select(e => e.Description)));
                        }

                        existingUser.TemporaryPasswordStatus = true;
                        await userManager.UpdateAsync(existingUser);

                        // 2. Adjust roles: Ensure role is ParkingUser, remove Tenant/FlatOwner so account is restricted to parking portal
                        var userRoles = await userManager.GetRolesAsync(existingUser);
                        if (userRoles.Contains("Tenant"))
                        {
                            await userManager.RemoveFromRoleAsync(existingUser, "Tenant");
                        }
                        if (userRoles.Contains("FlatOwner"))
                        {
                            await userManager.RemoveFromRoleAsync(existingUser, "FlatOwner");
                        }
                        if (!userRoles.Contains("ParkingUser"))
                        {
                            await userManager.AddToRoleAsync(existingUser, "ParkingUser");
                        }

                        application.CreatedUserId = existingUser.Id;
                        spot.AssignedUserId = existingUser.Id;
                    }
                }
                else
                {
                    spot.FlatId = application.FlatId;
                    if (!string.IsNullOrWhiteSpace(application.CreatedUserId))
                    {
                        spot.AssignedUserId = application.CreatedUserId;
                    }
                }

                dbContext.ParkingActivityLogs.Add(new ParkingActivityLog
                {
                    ParkingSpotId = spot.ParkingSpotId,
                    Action = "Marketplace Booking & Payment",
                    Details = $"{application.FullName} completed advance payment of ৳{application.AdvanceAmount:N0} for {targetInfo}{regInfo}. Spot marked Assigned.",
                    CreatedBy = application.FullName,
                    CreatedAt = DateTime.UtcNow
                });

                await dbContext.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                logger.LogError(ex, "Transaction failed while finalizing parking payment for {ApplicationId}", applicationId);
                return BadRequest(new { success = false, message = "Could not finalize booking. Please contact support." });
            }

            // Send Confirmation Email
            try
            {
                var baseLoginUrl = configuration["App:LoginUrl"] 
                    ?? (Url.Action("Login", "Account", null, Request.Scheme) ?? "https://localhost:7086/Account/Login");
                var cleanLoginUrl = baseLoginUrl.Split('?')[0];

                if (application.IsOutsider && !string.IsNullOrWhiteSpace(tempPassword))
                {
                    var loginUrl = $"{cleanLoginUrl}?email={Uri.EscapeDataString(application.Email)}&forceLogout=true";

                    var body = $"<div style='font-family:Segoe UI,sans-serif;max-width:600px;margin:0 auto;color:#1e293b;'>" +
                               $"<div style='background:#15803d;color:#ffffff;padding:24px;border-radius:12px 12px 0 0;text-align:center;'>" +
                               $"<h1 style='margin:0;font-size:24px;letter-spacing:1px;'>ADHUNIK BARI</h1>" +
                               $"<p style='margin:6px 0 0 0;font-size:14px;opacity:0.9;'>Dedicated Parking Portal Access</p>" +
                               $"</div>" +
                               $"<div style='background:#ffffff;padding:28px;border:1px solid #e2e8f0;border-radius:0 0 12px 12px;'>" +
                               $"<h2 style='color:#0f172a;margin-top:0;'>Payment Confirmed & Account Ready</h2>" +
                               $"<p>Hello <strong>{System.Net.WebUtility.HtmlEncode(application.FullName)}</strong>,</p>" +
                               $"<p>Thank you for your payment of <strong>BDT {application.AdvanceAmount:N2}</strong> for Parking Bay <strong>{spot.SpotNumber}</strong> ({spot.Floor?.FloorName ?? "Basement"}).</p>" +
                               $"<p>Because you are an external parking member, a dedicated parking account has been prepared for you:</p>" +
                               $"<div style='background:#f8fafc;border:2px solid #16a34a;border-radius:8px;padding:20px;margin:24px 0;'>" +
                               $"<div style='font-weight:bold;color:#15803d;margin-bottom:12px;font-size:15px;text-transform:uppercase;letter-spacing:0.5px;'>Your Sign-In Credentials</div>" +
                               $"<p style='margin:0 0 10px 0;font-size:15px;'><strong>Account Email / Username:</strong> <span style='font-family:monospace;font-size:15px;color:#0f172a;'>{System.Net.WebUtility.HtmlEncode(application.Email)}</span></p>" +
                               $"<p style='margin:0;font-size:15px;'><strong>Temporary Password:</strong> <span style='background:#dcfce7;color:#166534;padding:4px 10px;border-radius:4px;font-family:monospace;font-weight:bold;font-size:16px;'>{tempPassword}</span></p>" +
                               $"</div>" +
                               $"<div style='text-align:center;margin:30px 0;'>" +
                               $"<a href='{loginUrl}' style='display:inline-block;background:#15803d;color:#ffffff;padding:14px 28px;border-radius:30px;font-weight:600;text-decoration:none;font-size:15px;'>Sign In to Parking Portal</a>" +
                               $"</div>" +
                               $"<p style='font-size:13px;color:#64748b;'>Direct login link: <a href='{loginUrl}' style='color:#15803d;'>{loginUrl}</a></p>" +
                               $"<p style='color:#64748b;font-size:13px;border-top:1px solid #f1f5f9;padding-top:16px;margin-top:24px;'>Upon logging in, you can set your permanent password under Account Security. Your access is dedicated exclusively to garage parking and related billing.</p>" +
                               $"</div></div>";

                    bool emailSent = await emailService.SendAsync(application.Email, "ADHUNIK BARI - Parking Space Allocation & Account Credentials", body);
                    application.EmailSent = emailSent;
                    application.EmailSentAt = emailSent ? DateTime.UtcNow : null;
                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    var body = $"<div style='font-family:Segoe UI,sans-serif;max-width:600px;margin:0 auto;color:#1e293b;'>" +
                               $"<div style='background:#15803d;color:#ffffff;padding:20px;border-radius:12px 12px 0 0;text-align:center;'>" +
                               $"<h1 style='margin:0;font-size:22px;'>ADHUNIK BARI</h1>" +
                               $"<p style='margin:6px 0 0 0;font-size:14px;opacity:0.9;'>Parking Booking Confirmation</p>" +
                               $"</div>" +
                               $"<div style='background:#ffffff;padding:24px;border:1px solid #e2e8f0;border-radius:0 0 12px 12px;'>" +
                               $"<h2>Parking Bay Confirmed</h2>" +
                               $"<p>Hello <strong>{System.Net.WebUtility.HtmlEncode(application.FullName)}</strong>,</p>" +
                               $"<p>We received your advance payment of <strong>BDT {application.AdvanceAmount:N2}</strong> for Parking Bay <strong>{spot.SpotNumber}</strong> ({spot.Floor?.FloorName ?? "Basement"}).</p>" +
                               $"<p>The space has been linked to your profile and flat. You can review it anytime from your resident dashboard under 'My Parking'.</p>" +
                               $"</div></div>";

                    bool emailSent = await emailService.SendAsync(application.Email, "ADHUNIK BARI - Parking Bay Confirmed", body);
                    application.EmailSent = emailSent;
                    application.EmailSentAt = emailSent ? DateTime.UtcNow : null;
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Confirmation email could not be sent to {Email}.", application.Email);
            }

            if (!string.IsNullOrWhiteSpace(tempPassword))
            {
                TempData["ParkingTempPassword"] = tempPassword;
            }

            return Ok(new
            {
                success = true,
                redirectUrl = Url.Action(nameof(ParkingBookingSuccess), new { id = application.ParkingApplicationId })
            });
        }

        [HttpGet]
        public async Task<IActionResult> ParkingBookingSuccess(int id)
        {
            var application = await dbContext.ParkingApplications
                .Include(a => a.ParkingSpot)
                    .ThenInclude(p => p!.Floor)
                .Include(a => a.Flat)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.ParkingApplicationId == id);

            if (application == null || application.ParkingSpot == null)
            {
                return RedirectToAction(nameof(Index), new { type = "Parking" });
            }

            var spot = application.ParkingSpot;
            var viewModel = new ParkingBookingSuccessViewModel
            {
                ParkingSpotId = spot.ParkingSpotId,
                SpotNumber = spot.SpotNumber,
                FloorName = spot.Floor?.FloorName ?? "Basement",
                FlatNumber = application.Flat != null ? $"Flat {application.Flat.FlatNumber}" : "External Member (No Flat)",
                ResidentName = application.FullName,
                VehicleType = application.VehicleType ?? "Car",
                VehicleRegNumber = application.VehicleRegNumber,
                ListingType = application.ApplicationType,
                Price = spot.ListingPrice ?? spot.ParkingFee,
                AdvanceAmount = application.AdvanceAmount,
                BookingReference = $"PRK-{spot.ParkingSpotId}-{application.ParkingApplicationId}",
                BookedAt = application.PaidAt ?? application.CreatedAt,
                IsLoggedIn = User.Identity?.IsAuthenticated == true,
                IsOutsider = application.IsOutsider,
                GeneratedAccountEmail = application.IsOutsider ? application.Email : null,
                TemporaryPassword = TempData["ParkingTempPassword"] as string,
                EmailSent = application.EmailSent
            };

            return View(viewModel);
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

            while (chars.Count < 12)
                chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);

            for (var i = chars.Count - 1; i > 0; i--)
            {
                var j = RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars.ToArray());
        }

        private async Task<List<SelectListItem>> GetFlatSelectListAsync()
        {
            return await dbContext.Flats
                .AsNoTracking()
                .OrderBy(f => f.FloorNumber)
                .ThenBy(f => f.FlatNumber)
                .Select(f => new SelectListItem
                {
                    Value = f.FlatId.ToString(),
                    Text = $"Flat {f.FlatNumber} (Floor {f.FloorNumber})"
                })
                .ToListAsync();
        }

        private static string? NormalizeListingType(string? type)
        {
            if (string.Equals(type, PropertyListingTypes.ToLet, StringComparison.OrdinalIgnoreCase)) return PropertyListingTypes.ToLet;
            if (string.Equals(type, PropertyListingTypes.ForSale, StringComparison.OrdinalIgnoreCase)) return PropertyListingTypes.ForSale;
            if (string.Equals(type, "Parking", StringComparison.OrdinalIgnoreCase)) return "Parking";
            return null;
        }
    }
}
