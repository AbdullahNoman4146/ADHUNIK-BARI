using System.ComponentModel.DataAnnotations;


namespace ADHUNIK_BARI.ViewModels
{

    public class ChangePasswordViewModel
    {


        [Required(ErrorMessage = "Old password is required")]
        public string OldPassword { get; set; }




        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string NewPassword { get; set; }




        [Required(ErrorMessage = "Please confirm your new password")]
        [Compare(
            "NewPassword",
            ErrorMessage = "New password and confirm password do not match"
        )]
        public string ConfirmPassword { get; set; }



    }

}