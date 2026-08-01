using Microsoft.AspNetCore.Mvc;

namespace BelotWebApp.Models.Administration
{
    public class AdminUserModel
    {
        public AdminUserModel()
        {
            Users = [];
        }
        [BindProperty]
        public List<InputModel> Users { get; set; }

    }
    public class InputModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public List<bool> IsInRole { get; set; } = [];
    }
}
