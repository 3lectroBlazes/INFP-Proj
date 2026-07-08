using System.Security.Cryptography;
using INFP_Proj.Models;
using INFP_Proj.Services;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INFP_Proj.Pages
{
    [AllowAnonymous]
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> userManager;
        private readonly IEmailService emailService;

        public const string OtpSessionKey = "PasswordReset_Otp";
        public const string OtpEmailSessionKey = "PasswordReset_Email";
        public const string OtpExpirySessionKey = "PasswordReset_Expiry";

        [BindProperty]
        public ForgotPassword FPModel { get; set; }

        public bool OtpSent { get; set; }

        public ForgotPasswordModel(UserManager<AppUser> userManager, IEmailService emailService)
        {
            this.userManager = userManager;
            this.emailService = emailService;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await userManager.FindByEmailAsync(FPModel.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "No account was found with that email address.");
                return Page();
            }

            string otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            HttpContext.Session.SetString(OtpSessionKey, otp);
            HttpContext.Session.SetString(OtpEmailSessionKey, FPModel.Email);
            HttpContext.Session.SetString(OtpExpirySessionKey, DateTime.UtcNow.AddMinutes(10).ToString("O"));

            bool sent = await emailService.SendEmailAsync(
                FPModel.Email,
                "Your Hospital Portal password reset code",
                $"Your one-time password reset code is: {otp}\n\nThis code will expire in 10 minutes. If you did not request a password reset, you can ignore this email.");

            if (!sent)
            {
                ModelState.AddModelError("", "We couldn't send the reset code right now. Please try again later.");
                return Page();
            }

            OtpSent = true;
            return Page();
        }
    }
}
