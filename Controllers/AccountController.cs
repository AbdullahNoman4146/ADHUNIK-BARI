using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.ViewModels;


namespace ADHUNIK_BARI.Controllers
{

    public class AccountController : Controller
    {


        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;



        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager)
        {

            this.signInManager = signInManager;
            this.userManager = userManager;

        }





        // =========================
        // LOGIN PAGE
        // =========================

        [HttpGet]
        public IActionResult Login()
        {


            if (User.Identity != null &&
               User.Identity.IsAuthenticated)
            {

                return RedirectToDashboard();

            }


            return View();

        }








        // =========================
        // LOGIN PROCESS
        // =========================


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {


            if (!ModelState.IsValid)
            {

                return View(model);

            }





            var result =
                await signInManager.PasswordSignInAsync(

                    model.Email,

                    model.Password,

                    false,

                    false

                );






            if (result.Succeeded)
            {


                var user =
                    await userManager.FindByEmailAsync(
                        model.Email
                    );





                if (user != null)
                {


                    return await RedirectUserDashboard(user);


                }





                return RedirectToAction(
                    "Index",
                    "Home"
                );


            }






            ModelState.AddModelError(
                "",
                "Invalid email or password"
            );



            return View(model);


        }









        // =========================
        // CHANGE PASSWORD PAGE
        // =========================


        [HttpGet]
        public IActionResult ChangePassword()
        {

            return View();

        }









        // =========================
        // CHANGE PASSWORD PROCESS
        // =========================


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {


            if (!ModelState.IsValid)
            {

                return View(model);

            }





            var user =
                await userManager.GetUserAsync(User);





            if (user == null)
            {

                return RedirectToAction(
                    "Login"
                );

            }






            var result =
                await userManager.ChangePasswordAsync(
                    user,
                    model.OldPassword,
                    model.NewPassword
                );







            if (result.Succeeded)
            {


                // Temporary password completed

                user.TemporaryPasswordStatus = false;



                await userManager.UpdateAsync(user);






                TempData["Success"] =
                    "Password changed successfully";






                // Stay in resident dashboard

                return RedirectToAction(
                    "Dashboard",
                    "Resident"
                );


            }








            foreach (var error in result.Errors)
            {

                ModelState.AddModelError(
                    "",
                    error.Description
                );

            }






            return View(model);


        }









        // =========================
        // LOGOUT
        // =========================


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {


            await signInManager.SignOutAsync();



            return RedirectToAction(
                "Index",
                "Home"
            );


        }









        // =========================
        // ROLE BASED REDIRECT
        // =========================


        private async Task<IActionResult> RedirectUserDashboard(
            ApplicationUser user)
        {


            var roles =
                await userManager.GetRolesAsync(user);






            if (roles.Contains("Manager"))
            {

                return RedirectToAction(
                    "Dashboard",
                    "Manager"
                );

            }






            if (
                roles.Contains("Tenant") ||
                roles.Contains("FlatOwner")
            )
            {

                return RedirectToAction(
                    "Dashboard",
                    "Resident"
                );

            }






            return RedirectToAction(
                "Index",
                "Home"
            );


        }









        // Used when already logged in

        private IActionResult RedirectToDashboard()
        {


            if (User.IsInRole("Manager"))
            {

                return RedirectToAction(
                    "Dashboard",
                    "Manager"
                );

            }






            if (
                User.IsInRole("Tenant") ||
                User.IsInRole("FlatOwner")
            )
            {

                return RedirectToAction(
                    "Dashboard",
                    "Resident"
                );

            }







            return RedirectToAction(
                "Index",
                "Home"
            );


        }



    }

}