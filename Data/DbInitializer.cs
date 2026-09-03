using Microsoft.AspNetCore.Identity;


namespace ADHUNIK_BARI.Data
{
    public static class DbInitializer
    {

        public static async Task SeedRoles(
            RoleManager<IdentityRole> roleManager)
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

        }

    }
}