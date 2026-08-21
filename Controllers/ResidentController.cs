using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ADHUNIK_BARI.Models;


namespace ADHUNIK_BARI.Controllers
{


    [Authorize(Roles = "Tenant,FlatOwner")]
    public class ResidentController : Controller
    {


        private readonly UserManager<ApplicationUser> userManager;



        public ResidentController(
            UserManager<ApplicationUser> userManager)
        {

            this.userManager = userManager;

        }




        public async Task<IActionResult> Dashboard()
        {


            var user =
                await userManager.GetUserAsync(User);



            if (user != null)
            {

                ViewBag.RequirePasswordChange =
                    user.TemporaryPasswordStatus;

            }



            return View();

        }



    }


}