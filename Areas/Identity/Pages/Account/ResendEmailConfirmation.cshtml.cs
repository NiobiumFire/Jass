// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using BelotWebApp.Areas.Identity.Data;
using BelotWebApp.EmailTemplates;
using BelotWebApp.Services.EmailService;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;

namespace BelotWebApp.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class ResendEmailConfirmationModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ResendEmailConfirmationModel(UserManager<ApplicationUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            //[Required]
            //[EmailAddress]
            //public string Email { get; set; }
            [Required]
            [StringLength(15, ErrorMessage = "The {0} must be at most {1} characters long.")]
            [DataType(DataType.Text)]
            [Display(Name = "Username")]
            public string UserName { get; set; }
        }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync() // not used
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            //var user = await _userManager.FindByEmailAsync(Input.Email);
            var user = await _userManager.FindByNameAsync(Input.UserName);
            if (user == null || user.EmailConfirmed)
            {
                // Don't reveal that the user that the account does not exist or is confirmed
                ModelState.AddModelError(string.Empty, "If you have an unverified account, a verification email has been sent to the registered email address.");
                return Page();
            }

            var userId = await _userManager.GetUserIdAsync(user);
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { userId = userId, code = code },
                protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(user.Email, EmailTemplate.ConfirmEmail, new Dictionary<string, string>
            {
                { "UserName", user.UserName },
                { "ConfirmLink", HtmlEncoder.Default.Encode(callbackUrl) }
            });

            ModelState.AddModelError(string.Empty, "If you have an unverified account, a verification email has been sent to the registered email address.");
            return Page();
        }
    }
}
