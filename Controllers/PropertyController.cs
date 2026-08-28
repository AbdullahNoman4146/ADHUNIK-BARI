using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.Services;
using ADHUNIK_BARI.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ADHUNIK_BARI.Controllers
{
    [AllowAnonymous]
    public class PropertyController : Controller
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IPropertyPaymentService propertyPaymentService;

        public PropertyController(ApplicationDbContext dbContext, IPropertyPaymentService propertyPaymentService)
        {
            this.dbContext = dbContext;
            this.propertyPaymentService = propertyPaymentService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? type)
        {
            await propertyPaymentService.ReleaseExpiredReservationsAsync();
            var selectedType = NormalizeListingType(type);
            var query = GetPublicListingQuery();

            if (selectedType != null)
                query = query.Where(listing => listing.ListingType == selectedType);

            var listings = await query
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
                    PublishedAt = listing.PublishedAt
                })
                .ToListAsync();

            return View(new PublicPropertyListingsViewModel
            {
                SelectedType = selectedType,
                Listings = listings
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

        private static string? NormalizeListingType(string? type)
        {
            if (string.Equals(type, PropertyListingTypes.ToLet, StringComparison.OrdinalIgnoreCase)) return PropertyListingTypes.ToLet;
            if (string.Equals(type, PropertyListingTypes.ForSale, StringComparison.OrdinalIgnoreCase)) return PropertyListingTypes.ForSale;
            return null;
        }
    }
}
