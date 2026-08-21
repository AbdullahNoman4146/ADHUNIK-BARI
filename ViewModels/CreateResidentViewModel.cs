using System.ComponentModel.DataAnnotations;

namespace ADHUNIK_BARI.ViewModels
{
    public class CreateResidentViewModel
    {

        [Required]
        public string FullName { get; set; }



        [Required]
        public string Phone { get; set; }



        [Required]
        [EmailAddress]
        public string Email { get; set; }



        [Required]
        public string ResidentType { get; set; }



        [Required]
        [MinLength(6)]
        public string TemporaryPassword { get; set; }

    }
}