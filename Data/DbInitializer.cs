using Microsoft.AspNetCore.Identity;
using ADHUNIK_BARI.Models;


namespace ADHUNIK_BARI.Data
{
    public static class DbInitializer
    {

        public static async Task SeedRolesAndAdmin(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration)
        {


            string[] roles =
            {
                "Manager",
                "Tenant",
                "FlatOwner"
            };


            foreach (var role in roles)
            {

                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(
                        new IdentityRole(role)
                    );
                }

            }



            var adminEmail = configuration["AdminAccount:Email"];

            var adminPassword = configuration["AdminAccount:Password"];


            var admin = await userManager.FindByEmailAsync(adminEmail);



            if (admin == null)
            {

                var manager = new ApplicationUser
                {

                    UserName = adminEmail,

                    Email = adminEmail,

                    FullName = "System Manager",

                    Phone = "01700000000",

                    AccountStatus = "Active",

                    TemporaryPasswordStatus = false,

                    CreatedAt = DateTime.Now

                };


                var result = await userManager.CreateAsync(
                                manager,
                                adminPassword
                                );


                if (result.Succeeded)
                {

                    await userManager.AddToRoleAsync(
                        manager,
                        "Manager"
                    );

                }

            }


        }


    }
}