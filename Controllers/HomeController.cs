using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.ViewModels;
using ADHUNIK_BARI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace ADHUNIK_BARI.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext dbContext;
        private readonly IPropertyPaymentService propertyPaymentService;

        public HomeController(ApplicationDbContext dbContext, IPropertyPaymentService propertyPaymentService)
        {
            this.dbContext = dbContext;
            this.propertyPaymentService = propertyPaymentService;
        }

        public async Task<IActionResult> Index()
        {
            await propertyPaymentService.ReleaseExpiredReservationsAsync();

            var availableListings = dbContext.PropertyListings
                .AsNoTracking()
                .Where(listing =>
                    listing.ListingStatus == PropertyListingStatuses.Published &&
                    listing.Flat != null &&
                    listing.Flat.FlatStatus == "Available" &&
                    !dbContext.FlatAssignments.Any(assignment =>
                        assignment.FlatId == listing.FlatId && assignment.IsActive));

            var listingSnapshot = await availableListings
                .Select(listing => new { listing.ListingType, listing.Price })
                .ToListAsync();

            var latestListings = await availableListings
                .OrderByDescending(listing => listing.PublishedAt ?? listing.CreatedAt)
                .Take(3)
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

            return View(new HomePageViewModel
            {
                LatestPropertyListings = latestListings,
                AvailableListingCount = listingSnapshot.Count,
                ToLetListingCount = listingSnapshot.Count(listing => listing.ListingType == PropertyListingTypes.ToLet),
                ForSaleListingCount = listingSnapshot.Count(listing => listing.ListingType == PropertyListingTypes.ForSale),
                StartingRent = listingSnapshot
                    .Where(listing => listing.ListingType == PropertyListingTypes.ToLet)
                    .Select(listing => (decimal?)listing.Price)
                    .Min(),
                StartingSalePrice = listingSnapshot
                    .Where(listing => listing.ListingType == PropertyListingTypes.ForSale)
                    .Select(listing => (decimal?)listing.Price)
                    .Min()
            });
        }
        public IActionResult Contact()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
