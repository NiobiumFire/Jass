using BelotWebApp.Areas.Identity.Data;
using BelotWebApp.BelotClasses;
using BelotWebApp.BelotClasses.Training;
using BelotWebApp.Configuration;
using BelotWebApp.Data;
using BelotWebApp.Services.AppPathService;
using BelotWebApp.Services.EmailService;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        builder.Services.AddSingleton<IAppPaths, AppPaths>();

        builder.Services.AddDbContext<AuthDbContext>((serviceProvider, options) =>
        {
            var appPaths = serviceProvider.GetRequiredService<IAppPaths>();
            options.UseSqlite($"Data Source={appPaths.DatabaseFile}");
        });

        builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<AuthDbContext>();

        builder.Services.AddControllersWithViews();
        builder.Services.AddRazorPages();
        builder.Services.AddSignalR().AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.Converters.Add(new DeclarationConverter());
        });

        builder.Services.AddScoped<IEmailSender, EmailService>();

        builder.Services.AddSingleton<BelotGameRegistry>();

        builder.Services.AddSingleton<BelotGameSimulator>();

        builder.Services.AddSingleton<SimulationResult>();

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();

        app.UseAuthorization();

        app.MapHub<BelotRoom>("/belotroom/{roomId}");

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();

        // Ensure roles exist at startup
        await EnsureRolesAsync(app);

        // Grant primary admin
        await EnsurePrimaryAdminAsync(app);

        app.Run();
    }

    private static async Task EnsureRolesAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRolesExist(roleManager); // Your existing method
    }

    private static async Task EnsurePrimaryAdminAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        var username = config["PrimaryAdmin:Username"];
        var email = config["PrimaryAdmin:Email"];
        var password = config["PrimaryAdmin:Password"];

        if (!string.IsNullOrWhiteSpace(username) &&
            !string.IsNullOrWhiteSpace(email) &&
            !string.IsNullOrWhiteSpace(password))
        {
            var user = await userManager.FindByNameAsync(username);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = username,
                    Email = email,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(user, password);
                if (!createResult.Succeeded)
                {
                    var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                    throw new Exception($"Failed to create admin user: {errors}");
                }
            }

            if (!await userManager.IsInRoleAsync(user, "Player"))
            {
                await userManager.AddToRoleAsync(user, "Player");
            }

            if (!await userManager.IsInRoleAsync(user, "Admin"))
            {
                await userManager.AddToRoleAsync(user, "Admin");
            }
        }
    }

    private static async Task EnsureRolesExist(RoleManager<IdentityRole> roleManager)
    {
        string[] roles = ["Player", "Admin"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }
}