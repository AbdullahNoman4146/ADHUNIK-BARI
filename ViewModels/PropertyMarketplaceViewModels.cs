using System.ComponentModel.DataAnnotations;
using ADHUNIK_BARI.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ADHUNIK_BARI.ViewModels
{
    public class CreatePropertyListingViewModel
    {
        [Required]
        public int FlatId { get; set; }

        [Required]
        [MaxLength(20)]
        public string ListingType { get; set; } = PropertyListingTypes.ToLet;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string ShortDescription { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Price { get; set; }

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal AdvanceAmount { get; set; }

        [Range(0, int.MaxValue)]
        public int Bedrooms { get; set; }

        [Range(0, int.MaxValue)]
        public int Bathrooms { get; set; }

        [Range(0, int.MaxValue)]
        public int Balconies { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal AreaSqFt { get; set; }

        [MaxLength(50)]
        public string? FurnishingStatus { get; set; }

        [MaxLength(50)]
        public string? Facing { get; set; }

        [MaxLength(2000)]
        public string? Features { get; set; }

        public IFormFile? CoverImage { get; set; }

        [Required]
        public IFormFile? RoomLayoutImage { get; set; }

        public IEnumerable<Flat> AvailableFlats { get; set; } = Enumerable.Empty<Flat>();
    }

    public class EditPropertyListingViewModel
    {
        [Required]
        public int PropertyListingId { get; set; }

        public int FlatId { get; set; }

        public string FlatNumber { get; set; } = string.Empty;

        public int FloorNumber { get; set; }

        public string ListingStatus { get; set; } = PropertyListingStatuses.Draft;

        [Required]
        [MaxLength(20)]
        public string ListingType { get; set; } = PropertyListingTypes.ToLet;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(500)]
        public string ShortDescription { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal Price { get; set; }

        [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
        public decimal AdvanceAmount { get; set; }

        [Range(0, int.MaxValue)]
        public int Bedrooms { get; set; }

        [Range(0, int.MaxValue)]
        public int Bathrooms { get; set; }

        [Range(0, int.MaxValue)]
        public int Balconies { get; set; }

        [Range(typeof(decimal), "0", "79228162514264337593543950335")]
        public decimal AreaSqFt { get; set; }

        [MaxLength(50)]
        public string? FurnishingStatus { get; set; }

        [MaxLength(50)]
        public string? Facing { get; set; }

        [MaxLength(2000)]
        public string? Features { get; set; }

        public IFormFile? CoverImage { get; set; }

        // Optional replacement for the existing room layout image.
        public IFormFile? RoomLayoutImage { get; set; }

        // Preserve existing image paths so replacement remains optional.
        public string? CurrentCoverImagePath { get; set; }

        public string CurrentRoomLayoutImagePath { get; set; } = string.Empty;
    }

    public class PropertyApplicationViewModel
    {
        [Required]
        public int PropertyListingId { get; set; }

        [Required]
        [MaxLength(200)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string CurrentAddress { get; set; } = string.Empty;

        [MaxLength(150)]
        public string? Profession { get; set; }

        [Range(1, int.MaxValue)]
        public int? NumberOfOccupants { get; set; }

        public DateTime? ExpectedMoveInDate { get; set; }

        [MaxLength(2000)]
        public string? Message { get; set; }

        public bool TermsAccepted { get; set; }
    }

    public class PropertyCheckoutViewModel
    {
        public int PropertyApplicationId { get; set; }
        public int PropertyListingId { get; set; }
        public string ListingType { get; set; } = string.Empty;
        public string ListingTitle { get; set; } = string.Empty;
        public string FlatNumber { get; set; } = string.Empty;
        public decimal AdvanceAmount { get; set; }
        public string ClientSecret { get; set; } = string.Empty;
        public string StripePaymentIntentId { get; set; } = string.Empty;
        public DateTime? ReservationExpiresAt { get; set; }
        public string ApplicantEmail { get; set; } = string.Empty;
        public string PublishableKey { get; set; } = string.Empty;
    }

    public class PublicPropertyListingCardViewModel
    {
        public int PropertyListingId { get; set; }

        public string ListingType { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string ShortDescription { get; set; } = string.Empty;

        public string FlatNumber { get; set; } = string.Empty;

        public int FloorNumber { get; set; }

        public decimal Price { get; set; }

        public decimal AdvanceAmount { get; set; }

        public int Bedrooms { get; set; }

        public int Bathrooms { get; set; }

        public decimal AreaSqFt { get; set; }

        public string? CoverImagePath { get; set; }

        public string RoomLayoutImagePath { get; set; } = string.Empty;

        public DateTime? PublishedAt { get; set; }

        // Parking Marketplace Extensions
        public bool IsParking { get; set; }
        public int? ParkingSpotId { get; set; }
        public string? SpotNumber { get; set; }
        public string? FloorName { get; set; }
        public string? VehicleType { get; set; }
    }

    public class PublicPropertyListingDetailsViewModel : PublicPropertyListingCardViewModel
    {
        public string Description { get; set; } = string.Empty;

        public int Balconies { get; set; }

        public string? FurnishingStatus { get; set; }

        public string? Facing { get; set; }

        public string? Features { get; set; }
    }

    public class PublicPropertyListingsViewModel
    {
        public string? SelectedType { get; set; }

        public IReadOnlyList<PublicPropertyListingCardViewModel> Listings { get; set; } = Array.Empty<PublicPropertyListingCardViewModel>();
    }

    public class ManagerPropertyListingsViewModel
    {
        public string SelectedSection { get; set; } = "active";

        public IReadOnlyList<PropertyListing> Listings { get; set; } = Array.Empty<PropertyListing>();

        public int TotalCount { get; set; }

        public int ActiveCount { get; set; }

        public int PublishedCount { get; set; }

        public int DraftCount { get; set; }

        public int ReservedOrCompletedCount { get; set; }

        public int ArchivedCount { get; set; }
    }

    public class HomePageViewModel
    {
        public IReadOnlyList<PublicPropertyListingCardViewModel> LatestPropertyListings { get; set; } = Array.Empty<PublicPropertyListingCardViewModel>();

        public int AvailableListingCount { get; set; }

        public int ToLetListingCount { get; set; }

        public int ForSaleListingCount { get; set; }

        public decimal? StartingRent { get; set; }

        public decimal? StartingSalePrice { get; set; }
    }

    public class PublicParkingDetailsViewModel
    {
        public int ParkingSpotId { get; set; }
        public string SpotNumber { get; set; } = string.Empty;
        public string FloorName { get; set; } = string.Empty;
        public string FloorCode { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string ListingType { get; set; } = string.Empty;
        public string VehicleType { get; set; } = "Car";
        public decimal Price { get; set; }
        public decimal AdvanceAmount { get; set; }
        public string? ListingNotes { get; set; }
        public DateTime? PublishedAt { get; set; }
    }

    public class ApplyParkingViewModel
    {
        [Required]
        public int ParkingSpotId { get; set; }

        public string SpotNumber { get; set; } = string.Empty;
        public string FloorName { get; set; } = string.Empty;
        public string ListingType { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal AdvanceAmount { get; set; }
        public string VehicleType { get; set; } = "Car";

        [Required(ErrorMessage = "Full name is required.")]
        [MaxLength(200)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email address is required.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [MaxLength(256)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [MaxLength(50)]
        public string Phone { get; set; } = string.Empty;

        [Display(Name = "Your Flat (Optional)")]
        public int? FlatId { get; set; }

        public string? SelectedFlatNumber { get; set; }

        [MaxLength(50)]
        [Display(Name = "Vehicle Registration Number (optional)")]
        public string? VehicleRegNumber { get; set; }

        [Display(Name = "Expected Start / Move-in Date")]
        public DateTime? ExpectedStartDate { get; set; }

        [MaxLength(1000)]
        [Display(Name = "Message / Notes to Management (optional)")]
        public string? Notes { get; set; }

        public bool TermsAccepted { get; set; }

        public List<SelectListItem> AvailableFlats { get; set; } = new();

        public bool IsLoggedInResident { get; set; }
        public string? ResidentRole { get; set; }
    }

    public class ParkingCheckoutViewModel
    {
        public int ParkingApplicationId { get; set; }
        public int ParkingSpotId { get; set; }
        public string SpotNumber { get; set; } = string.Empty;
        public string FloorName { get; set; } = string.Empty;
        public string ListingType { get; set; } = string.Empty;
        public decimal AdvanceAmount { get; set; }
        public string ClientSecret { get; set; } = string.Empty;
        public string StripePaymentIntentId { get; set; } = string.Empty;
        public DateTime? ReservationExpiresAt { get; set; }
        public string ApplicantName { get; set; } = string.Empty;
        public string ApplicantEmail { get; set; } = string.Empty;
        public string ApplicantPhone { get; set; } = string.Empty;
        public string? VehicleType { get; set; }
        public string? VehicleRegNumber { get; set; }
        public bool IsOutsider { get; set; }
        public string? FlatNumber { get; set; }
        public string PublishableKey { get; set; } = string.Empty;
        public bool IsSimulatedDevPayment { get; set; }
    }

    public class ParkingBookingSuccessViewModel
    {
        public int ParkingSpotId { get; set; }
        public string SpotNumber { get; set; } = string.Empty;
        public string FloorName { get; set; } = string.Empty;
        public string FlatNumber { get; set; } = string.Empty;
        public string ResidentName { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string? VehicleRegNumber { get; set; }
        public string ListingType { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal AdvanceAmount { get; set; }
        public string BookingReference { get; set; } = string.Empty;
        public DateTime BookedAt { get; set; } = DateTime.UtcNow;
        public bool IsLoggedIn { get; set; }
        public bool IsOutsider { get; set; }
        public string? GeneratedAccountEmail { get; set; }
        public string? TemporaryPassword { get; set; }
        public bool EmailSent { get; set; }
    }
}
