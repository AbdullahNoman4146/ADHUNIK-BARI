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


            // If already logged in
            // send user directly to dashboard

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




            if (roles.Contains("Tenant"))
            {

                return RedirectToAction(
                    "Dashboard",
                    "Tenant"
                );

            }




            if (roles.Contains("FlatOwner"))
            {

                return RedirectToAction(
                    "Dashboard",
                    "FlatOwner"
                );

            }




            return RedirectToAction(
                "Index",
                "Home"
            );


        }








        // Used by Login GET
        // when user already logged in

        private IActionResult RedirectToDashboard()
        {


            if (User.IsInRole("Manager"))
            {

                return RedirectToAction(
                    "Dashboard",
                    "Manager"
                );

            }



            if (User.IsInRole("Tenant"))
            {

                return RedirectToAction(
                    "Dashboard",
                    "Tenant"
                );

            }




            if (User.IsInRole("FlatOwner"))
            {

                return RedirectToAction(
                    "Dashboard",
                    "FlatOwner"
                );

            }




            return RedirectToAction(
                "Index",
                "Home"
            );


        }


    }

}