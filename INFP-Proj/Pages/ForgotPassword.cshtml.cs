using System.Globalization;
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
        public const string ResetTokenSessionKey = "PasswordReset_Token";

        [BindProperty]
        public ForgotPassword FPModel { get; set; }

        [BindProperty]
        public string? OtpCode { get; set; }

        public bool OtpSent { get; set; }
        public string? SentToEmail { get; set; }

        public ForgotPasswordModel(UserManager<AppUser> userManager, IEmailService emailService)
        {
            this.userManager = userManager;
            this.emailService = emailService;
        }

        public void OnGet()
        {
            SentToEmail = HttpContext.Session.GetString(OtpEmailSessionKey);
            OtpSent = !string.IsNullOrEmpty(SentToEmail) && !string.IsNullOrEmpty(HttpContext.Session.GetString(OtpSessionKey));
        }

        public IActionResult OnGetReset()
        {
            ClearOtpSession();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSendAsync()
        {
            ModelState.Clear();
            if (!TryValidateModel(FPModel, nameof(FPModel)))
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
                $"Your one-time password is: {otp}\n\nThis code will expire in 10 minutes. If you did not request a password reset, you can ignore this email.");

            if (!sent)
            {
                ClearOtpSession();
                ModelState.AddModelError("", "We couldn't send the reset code right now. Please try again later.");
                return Page();
            }

            OtpSent = true;
            SentToEmail = FPModel.Email;
            return Page();
        }

        public async Task<IActionResult> OnPostResendAsync()
        {
            string? pendingEmail = HttpContext.Session.GetString(OtpEmailSessionKey);

            if (string.IsNullOrEmpty(pendingEmail))
            {
                ClearOtpSession();
                OtpSent = false;
                ModelState.AddModelError("", "Your session has expired. Please enter your email again.");
                return Page();
            }

            var user = await userManager.FindByEmailAsync(pendingEmail);
            if (user == null)
            {
                ClearOtpSession();
                OtpSent = false;
                ModelState.AddModelError("", "No account was found with that email address.");
                return Page();
            }

            string otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

            bool sent = await emailService.SendEmailAsync(
                pendingEmail,
                "Your Hospital Portal password reset code",
                $"Your one-time password is: {otp}\n\nThis code will expire in 10 minutes. If you did not request a password reset, you can ignore this email.");

            OtpSent = true;
            SentToEmail = pendingEmail;

            if (!sent)
            {
                ModelState.AddModelError("", "We couldn't resend the code right now. Please try again later.");
                return Page();
            }

            // Only overwrite the previous code once the new one is confirmed sent,
            // otherwise a failed resend would silently invalidate a still-valid pending code.
            HttpContext.Session.SetString(OtpSessionKey, otp);
            HttpContext.Session.SetString(OtpExpirySessionKey, DateTime.UtcNow.AddMinutes(10).ToString("O"));

            return Page();
        }

        public async Task<IActionResult> OnPostVerifyAsync()
        {
            string? pendingEmail = HttpContext.Session.GetString(OtpEmailSessionKey);
            string? pendingOtp = HttpContext.Session.GetString(OtpSessionKey);
            string? expiryRaw = HttpContext.Session.GetString(OtpExpirySessionKey);

            OtpSent = !string.IsNullOrEmpty(pendingEmail);
            SentToEmail = pendingEmail;

            if (string.IsNullOrEmpty(pendingEmail) || string.IsNullOrEmpty(pendingOtp) || string.IsNullOrEmpty(expiryRaw))
            {
                ClearOtpSession();
                OtpSent = false;
                ModelState.AddModelError("", "Your reset code has expired. Please request a new one.");
                return Page();
            }

            if (DateTime.UtcNow > DateTime.Parse(expiryRaw, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind))
            {
                ClearOtpSession();
                OtpSent = false;
                ModelState.AddModelError("", "Your reset code has expired. Please request a new one.");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(OtpCode) || !string.Equals(OtpCode.Trim(), pendingOtp, StringComparison.Ordinal))
            {
                ModelState.AddModelError("", "That code doesn't match. Please try again.");
                return Page();
            }

            var user = await userManager.FindByEmailAsync(pendingEmail);
            if (user == null)
            {
                ClearOtpSession();
                OtpSent = false;
                ModelState.AddModelError("", "No account was found with that email address.");
                return Page();
            }

            string resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            HttpContext.Session.SetString(ResetTokenSessionKey, resetToken);
            HttpContext.Session.Remove(OtpSessionKey);
            HttpContext.Session.Remove(OtpExpirySessionKey);

            return RedirectToPage("/ResetPassword");
        }

        private void ClearOtpSession()
        {
            HttpContext.Session.Remove(OtpSessionKey);
            HttpContext.Session.Remove(OtpEmailSessionKey);
            HttpContext.Session.Remove(OtpExpirySessionKey);
            HttpContext.Session.Remove(ResetTokenSessionKey);
        }
    }
}
