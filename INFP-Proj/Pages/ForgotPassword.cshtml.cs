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
        private readonly IOtpService otpService;

        public const string Purpose = "PasswordReset";
        public const string ResetTokenSessionKey = "PasswordReset_Token";

        private const string EmailSubject = "Your Hospital Portal password reset code";
        private const string BodyTemplate = "Your one-time password reset code is: {0}\n\nThis code will expire in 10 minutes. If you did not request a password reset, you can ignore this email.";

        [BindProperty]
        public ForgotPassword FPModel { get; set; }

        [BindProperty]
        public string? OtpCode { get; set; }

        public bool OtpSent { get; set; }
        public string? SentToEmail { get; set; }

        public ForgotPasswordModel(UserManager<AppUser> userManager, IOtpService otpService)
        {
            this.userManager = userManager;
            this.otpService = otpService;
        }

        public void OnGet()
        {
            SentToEmail = otpService.GetPendingEmail(HttpContext.Session, Purpose);
            OtpSent = !string.IsNullOrEmpty(SentToEmail);
        }

        public IActionResult OnGetReset()
        {
            ClearAll();
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

            bool sent = await otpService.GenerateAndSendAsync(HttpContext.Session, Purpose, FPModel.Email, EmailSubject, BodyTemplate);

            if (!sent)
            {
                otpService.Clear(HttpContext.Session, Purpose);
                ModelState.AddModelError("", "We couldn't send the reset code right now. Please try again later.");
                return Page();
            }

            OtpSent = true;
            SentToEmail = FPModel.Email;
            return Page();
        }

        public async Task<IActionResult> OnPostResendAsync()
        {
            string? pendingEmail = otpService.GetPendingEmail(HttpContext.Session, Purpose);

            if (string.IsNullOrEmpty(pendingEmail))
            {
                ClearAll();
                OtpSent = false;
                ModelState.AddModelError("", "Your session has expired. Please enter your email again.");
                return Page();
            }

            var user = await userManager.FindByEmailAsync(pendingEmail);
            if (user == null)
            {
                ClearAll();
                OtpSent = false;
                ModelState.AddModelError("", "No account was found with that email address.");
                return Page();
            }

            bool sent = await otpService.GenerateAndSendAsync(HttpContext.Session, Purpose, pendingEmail, EmailSubject, BodyTemplate);

            OtpSent = true;
            SentToEmail = pendingEmail;

            if (!sent)
            {
                ModelState.AddModelError("", "We couldn't resend the code right now. Please try again later.");
                return Page();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostVerifyAsync()
        {
            string? pendingEmail = otpService.GetPendingEmail(HttpContext.Session, Purpose);
            OtpSent = !string.IsNullOrEmpty(pendingEmail);
            SentToEmail = pendingEmail;

            if (string.IsNullOrEmpty(pendingEmail))
            {
                ClearAll();
                OtpSent = false;
                ModelState.AddModelError("", "Your reset code has expired. Please request a new one.");
                return Page();
            }

            var verifyResult = otpService.Verify(HttpContext.Session, Purpose, pendingEmail, OtpCode ?? string.Empty);
            if (verifyResult == OtpVerifyResult.Expired)
            {
                ClearAll();
                OtpSent = false;
                ModelState.AddModelError("", "Your reset code has expired. Please request a new one.");
                return Page();
            }
            if (verifyResult == OtpVerifyResult.Mismatch)
            {
                ModelState.AddModelError("", "That code doesn't match. Please try again.");
                return Page();
            }

            var user = await userManager.FindByEmailAsync(pendingEmail);
            if (user == null)
            {
                ClearAll();
                OtpSent = false;
                ModelState.AddModelError("", "No account was found with that email address.");
                return Page();
            }

            string resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
            HttpContext.Session.SetString(ResetTokenSessionKey, resetToken);

            // Only clear the code itself; the email is still needed by the reset-password step.
            otpService.ClearCode(HttpContext.Session, Purpose);

            return RedirectToPage("/ResetPassword");
        }

        private void ClearAll()
        {
            otpService.Clear(HttpContext.Session, Purpose);
            HttpContext.Session.Remove(ResetTokenSessionKey);
        }
    }
}
