using BelotWebApp.BelotClasses;
using Microsoft.AspNetCore.DataProtection;
using System.Security.Claims;

namespace BelotWebApp.Middleware
{
    public class GuestMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly int _cookieLifetime = 1;
        private readonly BelotGameRegistry _gameRegistry;
        private readonly IDataProtector _protector;

        public GuestMiddleware(RequestDelegate next, BelotGameRegistry gameRegistry, IDataProtectionProvider provider)
        {
            _next = next;
            _gameRegistry = gameRegistry;
            _protector = provider.CreateProtector("GuestCookieProtector"); ;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated == true)
            {
                context.Response.Cookies.Delete("UserData1");
                await _next(context);
                return;
            }

            string? guestUsername = null;

            // Try decrypting existing cookie
            if (context.Request.Cookies.TryGetValue("UserData1", out string? encryptedGuestUsername))
            {
                try
                {
                    guestUsername = _protector.Unprotect(encryptedGuestUsername);
                }
                catch
                {
                    // cookie invalid or tampered
                }
            }

            if (string.IsNullOrEmpty(guestUsername))
            {
                guestUsername = GenerateUniqueGuestName();
            }

            encryptedGuestUsername = _protector.Protect(guestUsername);

            // Create or refresh cookie
            context.Response.Cookies.Append("UserData1", encryptedGuestUsername, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(_cookieLifetime)
            });

            // Assign a claims principal
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, guestUsername)
            };
            var identity = new ClaimsIdentity(claims);
            context.User = new ClaimsPrincipal(identity);

            await _next(context);
        }

        private string GenerateUniqueGuestName()
        {
            var random = new Random();

            var games = _gameRegistry.GetAllGames();
            var players = games.SelectMany(g => g.Players).Select(p => p.Username).ToHashSet();
            var spectators = games.SelectMany(g => g.Spectators).Select(p => p.Username).ToHashSet();

            int attempts = 0;

            string username;
            do
            {
                if (++attempts > 9999)
                {
                    // Fallback: generate a GUID-based name
                    return string.Concat("Guest ", Guid.NewGuid().ToString("N").AsSpan(0, 8));
                }

                username = $"Guest {Random.Shared.Next(1000, 9999)}";
            }
            while (players.Contains(username) || spectators.Contains(username));

            return username;
        }
    }

}
