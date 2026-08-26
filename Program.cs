using ADHUNIK_BARI;
using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Register Billing and Payment Services
builder.Services.AddScoped<IBillingService, ADHUNIK_BARI.Services.BillingService>();
builder.Services.AddScoped<IPaymentService, ADHUNIK_BARI.Services.PaymentService>();

builder.Services.AddHttpClient();

builder.Services.AddScoped<
    IAIComplaintSummaryService,
    GeminiComplaintSummaryService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure Stripe API key securely from User Secrets / Environment Variables / Configuration
var stripeSecretKey = builder.Configuration["Stripe:SecretKey"]
    ?? builder.Configuration["STRIPE_SECRET_KEY"]
    ?? Environment.GetEnvironmentVariable("STRIPE_SECRET_KEY");

if (!string.IsNullOrWhiteSpace(stripeSecretKey))
{
    StripeConfiguration.ApiKey = stripeSecretKey;
}
else
{
    app.Logger.LogWarning("Stripe Secret Key is not configured in User Secrets or environment variables.");
}

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
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    await DbInitializer.SeedRoles(roleManager);
    

}



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();