using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.ViewModels;


namespace ADHUNIK_BARI.Controllers
{

    [Authorize(Roles = "Manager")]
    public class ManagerController : Controller
    {


        private readonly UserManager<ApplicationUser> userManager;


        public ManagerController(
            UserManager<ApplicationUser> userManager)
        {
            this.userManager = userManager;
        }





        public IActionResult Dashboard()
        {
            return View();
        }





        // GET CREATE RESIDENT

        [HttpGet]
        public IActionResult CreateResident()
        {
            return View();
        }







        // POST CREATE RESIDENT

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateResident(
            CreateResidentViewModel model)
        {


            if (!ModelState.IsValid)
            {
                return View(model);
            }





            var existingUser =
                await userManager.FindByEmailAsync(model.Email);



            if (existingUser != null)
            {

                ModelState.AddModelError(
                    "",
                    "Email already exists"
                );


                return View(model);

            }







            ApplicationUser user = new ApplicationUser
            {


                FullName = model.FullName,
                Phone = model.Phone,


                UserName = model.Email,


                Email = model.Email,


                EmailConfirmed = true,


                TemporaryPasswordStatus = true,


                AccountStatus = "Active",


                CreatedAt = DateTime.Now


            };







            var result =
                await userManager.CreateAsync(
                    user,
                    model.TemporaryPassword
                );








            if (result.Succeeded)
            {



                var roleResult =
                    await userManager.AddToRoleAsync(
                        user,
                        model.ResidentType
                    );






                if (!roleResult.Succeeded)
                {


                    foreach (var error in roleResult.Errors)
                    {

                        ModelState.AddModelError(
                            "",
                            error.Description
                        );

                    }



                    return View(model);


                }







                TempData["Success"] =
                    "Resident account created successfully";






                return RedirectToAction(
                    "Dashboard"
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



    }

}