
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using RecruitmentTracker.Data;
using RecruitmentTracker.Models;
using QuestPDF.Infrastructure;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// DATABASE
// ==========================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ==========================================
// IDENTITY + ROLES
// ==========================================

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// ==========================================
// MVC
// ==========================================

builder.Services.AddControllersWithViews();

builder.Services.AddHttpClient();

if (Environment.GetEnvironmentVariable("PLAYWRIGHT_QA_MODE") == "1")
{
    // Registration creates test accounts; QA never sends a real email.
    builder.Services.AddSingleton<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, QaNoOpEmailSender>();
}

var app = builder.Build();

// ==========================================
// ERROR HANDLING
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();

app.UseAuthorization();

// ==========================================
// ROUTING
// ==========================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// ==========================================
// CREATE ROLES + SYSTEM ADMIN
// ==========================================

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Only the Playwright runner enables this flag. Its connection string points
    // to a separate QA database, so normal application data is not changed.
    if (Environment.GetEnvironmentVariable("PLAYWRIGHT_QA_MODE") == "1")
    {
        var qaDatabase = services.GetRequiredService<ApplicationDbContext>();
        await qaDatabase.Database.EnsureCreatedAsync();
    }

    await CreateRoles(services);

    await CreateSystemAdmin(services);
}

app.Run();


// ==========================================
// CREATE ROLES
// ==========================================

async Task CreateRoles(IServiceProvider serviceProvider)
{
    var roleManager =
        serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles =
    {
        "SystemAdmin",
        "HR",
        "Candidate",
        "Interviewer",
        "HiringManager"
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(
                new IdentityRole(role));
        }
    }
}


// ==========================================
// CREATE SYSTEM ADMIN
// ==========================================

async Task CreateSystemAdmin(IServiceProvider serviceProvider)
{
    var userManager =
        serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

    var roleManager =
        serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    // ==========================================
    // INITIAL SYSTEM ADMIN DETAILS
    // ==========================================

    string adminEmail = "admin@recruitmenttracker.com";

    string adminPassword = "Admin@12345";

    string adminName = "System Administrator";


    // ==========================================
    // MAKE SURE ROLE EXISTS
    // ==========================================

    if (!await roleManager.RoleExistsAsync("SystemAdmin"))
    {
        await roleManager.CreateAsync(
            new IdentityRole("SystemAdmin"));
    }


    // ==========================================
    // CHECK IF ADMIN ALREADY EXISTS
    // ==========================================

    var admin =
        await userManager.FindByEmailAsync(adminEmail);


    // ==========================================
    // CREATE ADMIN IF NEEDED
    // ==========================================

    if (admin == null)
    {
        admin = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = adminName,
            EmailConfirmed = true
        };

        var result =
            await userManager.CreateAsync(
                admin,
                adminPassword);

        if (!result.Succeeded)
        {
            var errors = string.Join(
                ", ",
                result.Errors.Select(e => e.Description));

            throw new Exception(
                "Could not create SystemAdmin: "
                + errors);
        }
    }


    // ==========================================
    // MAKE SURE ADMIN HAS SYSTEMADMIN ROLE
    // ==========================================

    if (!await userManager.IsInRoleAsync(
            admin,
            "SystemAdmin"))
    {
        await userManager.AddToRoleAsync(
            admin,
            "SystemAdmin");
    }
}

sealed class QaNoOpEmailSender : Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
        => Task.CompletedTask;
}
