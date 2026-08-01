using BelotWebApp.BelotClasses;
using BelotWebApp.BelotClasses.Training;
using BelotWebApp.Configuration;
using BelotWebApp.Data;
using BelotWebApp.Middleware;
using BelotWebApp.Notification;
using BelotWebApp.Services;
using BelotWebApp.Services.AppPathService;
using BelotWebApp.Services.EmailService;
using BelotWebApp.Services.ZipService;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Serilog;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.

        var appPaths = new AppPaths(builder.Configuration); // manual instantiation, not DI-resolved

        Log.Logger = new LoggerConfiguration()
        .Enrich.FromLogContext()
        .MinimumLevel.Information()
        .WriteTo.Console()
        // Hub logs
        .WriteTo.Logger(config => config
            .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("SourceContext")
                && e.Properties["SourceContext"].ToString().Contains("BelotRoomHub"))
            .WriteTo.File(
                Path.Combine(appPaths.HubLogFolder, "BelotRoomHubLog-.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message} RoomId={RoomId}{NewLine}{Exception}"))
        // FileCleanupService logs
        .WriteTo.Logger(config => config
            .Filter.ByIncludingOnly(e => e.Properties.ContainsKey("SourceContext")
                && e.Properties["SourceContext"].ToString().Contains("FileCleanupService"))
            .WriteTo.File(
                Path.Combine(appPaths.CleanupLogFolder, "CleanupLog-.txt"),
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {Message}{NewLine}{Exception}"))
        .CreateLogger();

        builder.Host.UseSerilog();

        builder.Services.AddSingleton<IAppPaths>(appPaths);

        builder.Services.AddHostedService<FileCleanupService>();

        builder.Services.AddDbContext<AuthDbContext>((serviceProvider, options) =>
        {
            var appPaths = serviceProvider.GetRequiredService<IAppPaths>();
            options.UseSqlite($"Data Source={appPaths.DatabaseFile}");
        });

        builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.SignIn.RequireConfirmedAccount = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AuthDbContext>()
            .AddDefaultTokenProviders();

        builder.Services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/Account/Login";
            options.AccessDeniedPath = "/Account/AccessDenied";
        });

        builder.Services.AddControllersWithViews(options =>
        {
            options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
        });
        builder.Services.AddRazorPages();
        builder.Services.AddSignalR(options =>
        {
            options.KeepAliveInterval = TimeSpan.FromMilliseconds(3000);
        }).AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.Converters.Add(new DeclarationConverter());
        });

        builder.Services.AddSingleton<ServerNotificationManager>();

        builder.Services.AddScoped<IEmailSender, EmailService>();

        builder.Services.AddSingleton<IZipService, ZipService>();

        builder.Services.AddSingleton<BelotRoomRegistry>();

        builder.Services.AddSingleton<BelotGameSimulator>();

        builder.Services.AddSingleton<SimulationResult>();

        var app = builder.Build();

        // Ensure database and migrations are applied at startup
        using (var scope = app.Services.CreateScope())
        {
            var services = scope.ServiceProvider;
            try
            {
                var context = services.GetRequiredService<AuthDbContext>();
                context.Database.Migrate();
            }
            catch (Exception)
            {
                //Console.WriteLine($"Error applying migrations: {ex.Message}");
            }
        }

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthentication();

        app.UseAuthorization();

        app.UseMiddleware<GuestMiddleware>();

        app.MapHub<BelotRoomHub>("/belotroom/{roomId}");

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
        app.MapRazorPages();

        // Ensure roles exist at startup
        await EnsureRolesAsync(app);

        // Grant primary admin
        await EnsurePrimaryAdminAsync(app);

        try
        {
            app.Run();
        }
        finally
        {
            Log.CloseAndFlush();
        }
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