using BelotWebApp.BelotClasses;
using BelotWebApp.BelotClasses.Users;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;
using System.Text.Json;

namespace BelotWebApp.Middleware
{
    public class GuestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int _cookieLifetime = 1;
        private readonly BelotRoomRegistry _roomRegistry;
        private readonly IDataProtector _protector;

        public GuestMiddleware(RequestDelegate next, BelotRoomRegistry roomRegistry, IDataProtectionProvider provider)
        {
            _next = next;
            _roomRegistry = roomRegistry;
            _protector = provider.CreateProtector("GuestCookieProtector"); ;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                context.Response.Cookies.Delete("GuestData");
                await _next(context);
                return;
            }

            string? guestId = null;
            string? guestUsername = null;

            // Try decrypting existing cookie
            if (context.Request.Cookies.TryGetValue("GuestData", out string? encryptedGuestData))
            {
                try
                {
                    var json = _protector.Unprotect(encryptedGuestData);
                    var guestData = JsonSerializer.Deserialize<GuestData>(json);
                    guestId = guestData?.Id;
                    guestUsername = guestData?.Username;
                }
                catch
                {
                    // cookie invalid or tampered
                }
            }

            if (string.IsNullOrEmpty(guestId) || string.IsNullOrEmpty(guestUsername))
            {
                guestId = Guid.NewGuid().ToString();
                guestUsername = GenerateUniqueGuestName();
            }

            var data = new GuestData
            {
                Id = guestId,
                Username = guestUsername
            };

            encryptedGuestData = _protector.Protect(JsonSerializer.Serialize(data));

            // Create or refresh cookie
            context.Response.Cookies.Append("GuestData", encryptedGuestData, new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // prevents the browser from sending this cookie over http. Https only.
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddHours(_cookieLifetime)
            });

            // Assign a claims principal
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, guestId),
                new Claim(ClaimTypes.Name, guestUsername)
            };
            var identity = new ClaimsIdentity(claims);
            context.User = new ClaimsPrincipal(identity);

            await _next(context);
        }

        private string GenerateUniqueGuestName()
        {
            var random = new Random();

            var allUsers = _roomRegistry.GetAllConnectedUsers().ToHashSet();

            int attempts = 0;

            string username;
            do
            {
                if (++attempts > 9999)
                {
                    // Fallback: generate a GUID-based name
                    return string.Concat("Guest ", Guid.NewGuid().ToString("N").AsSpan(0, 8));
                }

                username = $"Guest {Random.Shared.Next(1000, 10000)}";
            }
            while (allUsers.Contains(username));

            return username;
        }
    }

}
