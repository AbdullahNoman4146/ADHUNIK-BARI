using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ADHUNIK_BARI.Models
{
    public class GymSetting
    {
        [Key]
        public int GymSettingId { get; set; }

        [Required]
        [MaxLength(150)]
        public string GymName { get; set; } = "Adhunik Bari Fitness & Health Club";

        [Required]
        [MaxLength(100)]
        public string OpeningHours { get; set; } = "6:00 AM - 10:00 PM (Daily)";

        [Range(0, 100000)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal MonthlyFee { get; set; } = 500.00m;

        [MaxLength(2000)]
        public string? EquipmentInformation { get; set; } = "Commercial Treadmills, Elliptical Trainers, Multi-Station Cable Gym, Olympic Barbell & Weight Plates, Dumbbells (2.5kg - 32kg), Adjustable Benches, Power Squat Rack, Kettlebells, Yoga Mats & Resistance Bands.";

        [MaxLength(2000)]
        public string? RulesAndGuidelines { get; set; } = "1. Proper athletic footwear and clothing required at all times.\n2. Wipe down machines and equipment with sanitizing towels after each use.\n3. Return weights, dumbbells, and plates to designated racks.\n4. No food or open beverage containers allowed inside the facility.\n5. Cardio machine usage limited to 30 minutes during peak hours (6 PM - 9 PM).\n6. Non-resident guests are strictly prohibited without prior management clearance.\n7. Use equipment safely and report any malfunctioning machinery to building staff immediately.";

        [MaxLength(150)]
        public string? Location { get; set; } = "2nd Floor Amenities Area (Beside Community Center)";

        [MaxLength(50)]
        public string? ContactPhone { get; set; } = "+880 1712-345678";

        public bool IsActive { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(450)]
        public string? UpdatedByUserId { get; set; }

        [ForeignKey("UpdatedByUserId")]
        public ApplicationUser? UpdatedByUser { get; set; }
    }
}
