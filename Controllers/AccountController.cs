using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.ViewModels;


namespace ADHUNIK_BARI.Controllers
{


    public class AccountController : Controller
    {


        private readonly SignInManager<ApplicationUser> signInManager;


        public AccountController(
        SignInManager<ApplicationUser> signInManager)
        {

            this.signInManager = signInManager;

        }



        [HttpGet]
        public IActionResult Login()
        {

            return View();

        }




        [HttpPost]
        public async Task<IActionResult> Login(
        LoginViewModel model)
        {


            if (ModelState.IsValid)
            {


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
                    await signInManager.UserManager
                    .FindByEmailAsync(model.Email);



                    var roles =
                    await signInManager.UserManager
                    .GetRolesAsync(user);



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
                        "Owner"
                        );

                    }


                }



                ModelState.AddModelError(
                "",
                "Invalid login attempt"
                );


            }


            return View(model);


        }



        public async Task<IActionResult> Logout()
        {

            await signInManager.SignOutAsync();

            return RedirectToAction(
            "Login"
            );

        }


    }


}