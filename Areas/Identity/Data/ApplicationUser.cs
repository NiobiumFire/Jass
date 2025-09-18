using Microsoft.AspNetCore.Identity;

namespace BelotWebApp.Areas.Identity.Data;

// Add profile data for application users by adding properties to the ApplicationUser class
public class ApplicationUser : IdentityUser
{
    public string? CurrentSessionId { get; set; } // stores the session ID for the active login - prevent log in on multiple tabs or devices

    public int GamesTotal { get; set; }
    public int GamesWon { get; set; }
    public float Score { get; set; } // ranking system
}

