using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.Models;


var builder = WebApplication.CreateBuilder(args);



builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(
builder.Configuration.GetConnectionString("DefaultConnection")
));



builder.Services.AddIdentity<ApplicationUser, IdentityRole>()

.AddEntityFrameworkStores<ApplicationDbContext>()

.AddDefaultTokenProviders();



builder.Services.AddControllersWithViews();



var app = builder.Build();



if (!app.Environment.IsDevelopment())
{

    app.UseExceptionHandler("/Home/Error");

}



app.UseStaticFiles();



app.UseRouting();



app.UseAuthentication();

app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{

    var services = scope.ServiceProvider;


    var userManager =
        services.GetRequiredService<UserManager<ApplicationUser>>();


    var roleManager =
        services.GetRequiredService<RoleManager<IdentityRole>>();



    await DbInitializer.SeedRoles(
        roleManager
    );



}



app.MapControllerRoute(

    name: "default",

    pattern: "{controller=Home}/{action=Index}/{id?}"

);



app.Run();