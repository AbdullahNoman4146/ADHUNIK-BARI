using ADHUNIK_BARI;
using ADHUNIK_BARI.Data;
using ADHUNIK_BARI.Models;
using ADHUNIK_BARI.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Stripe;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    );
    options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Register Billing and Payment Services
builder.Services.AddScoped<IBillingService, ADHUNIK_BARI.Services.BillingService>();
builder.Services.AddScoped<IPaymentService, ADHUNIK_BARI.Services.PaymentService>();
builder.Services.AddScoped<IPropertyPaymentService, PropertyPaymentService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IGymService, GymService>();

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
    var dbContext = services.GetRequiredService<ApplicationDbContext>();

    try
    {
        await dbContext.Database.MigrateAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning($"Auto-migration notice: {ex.Message}");
    }

    try
    {
        await dbContext.Database.ExecuteSqlRawAsync(@"
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'GymMemberships')
            BEGIN
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'CardNumber')
                BEGIN
                    ALTER TABLE [GymMemberships] ADD [CardNumber] nvarchar(50) NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'FeePaidAt')
                BEGIN
                    ALTER TABLE [GymMemberships] ADD [FeePaidAt] datetime2 NULL;
                END
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[GymMemberships]') AND name = 'IsFeePaid')
                BEGIN
                    ALTER TABLE [GymMemberships] ADD [IsFeePaid] bit NOT NULL CONSTRAINT [DF_GymMemberships_IsFeePaid] DEFAULT 0;
                END
            END
        ");
    }
    catch (Exception ex)
    {
        app.Logger.LogWarning($"Gym schema ensure warning: {ex.Message}");
    }

    await DbInitializer.SeedRoles(roleManager);
}



app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();