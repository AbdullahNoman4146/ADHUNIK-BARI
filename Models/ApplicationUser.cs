using Microsoft.AspNetCore.Identity;

namespace ADHUNIK_BARI.Models
{
    public class ApplicationUser : IdentityUser
    {

        public string FullName { get; set; }

        public string Phone { get; set; }

        public bool TemporaryPasswordStatus { get; set; }

        public string AccountStatus { get; set; }

        public DateTime CreatedAt { get; set; }

    }
}