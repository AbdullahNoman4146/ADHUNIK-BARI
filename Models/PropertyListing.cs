using System.ComponentModel.DataAnnotations;

namespace ADHUNIK_BARI.Models
{
    public class PropertyListing
    {
        [Key]
        public int PropertyListingId { get; set; }

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

        [MaxLength(500)]
        public string? CoverImagePath { get; set; }

        [Required]
        [MaxLength(500)]
        public string RoomLayoutImagePath { get; set; } = string.Empty;

        [Required]
        [MaxLength(30)]
        public string ListingStatus { get; set; } = PropertyListingStatuses.Draft;

        public DateTime? PublishedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [Required]
        public string CreatedByUserId { get; set; } = string.Empty;

        public Flat? Flat { get; set; }

        public ApplicationUser? CreatedByUser { get; set; }

        public ICollection<PropertyApplication> Applications { get; set; } = new List<PropertyApplication>();
    }
}