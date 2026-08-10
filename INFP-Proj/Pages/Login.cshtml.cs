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
    public class LoginModel : PageModel
    {
        private readonly SignInManager<AppUser> signInManager;
        private readonly UserManager<AppUser> userManager;
        private readonly IOtpService otpService;

        private const string Purpose = "Login2FA";
        private const string PendingRememberMeKey = "Login2FA_RememberMe";
        private const string EmailSubject = "Your Hospital Portal verification code";
        private const string BodyTemplate = "Your one-time verification code is: {0}\n\nThis code will expire in 10 minutes. If this wasn't you, you can safely ignore this email.";

        [BindProperty]
        public Login LModel { get; set; }

        [BindProperty]
        public string? OtpCode { get; set; }

        public bool RequiresOtp { get; set; }

        public LoginModel(SignInManager<AppUser> signInManager, UserManager<AppUser> userManager, IOtpService otpService)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this.otpService = otpService;
        }

        private IActionResult RedirectToRoleHome()
        {
            bool isAdmin = User.HasClaim(c => c.Type == "IsAdmin" && c.Value == "True");
            return RedirectToPage(isAdmin ? "/Admin/Index" : "/User/Index");
        }

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToRoleHome();
            }

            RequiresOtp = !string.IsNullOrEmpty(otpService.GetPendingEmail(HttpContext.Session, Purpose));
            return Page();
        }

        public IActionResult OnGetReset()
        {
            ClearPending();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLoginAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await userManager.FindByEmailAsync(LModel.Email);
            if (user == null)
            {
                ModelState.AddModelError("", "Username or Password incorrect");
                return Page();
            }

            DateTimeOffset? lockoutEnd = await userManager.GetLockoutEndDateAsync(user);
            if (await userManager.GetLockoutEnabledAsync(user) && lockoutEnd.HasValue)
            {
                TimeSpan remaining = lockoutEnd.Value - DateTimeOffset.UtcNow;
                if (remaining > TimeSpan.Zero)
                {
                    ModelState.AddModelError("", $"This account is locked. Try again in {FormatRemaining(remaining)}.");
                    return Page();
                }

                // Lockout window has passed: reset the counter and the lockout flag.
                await userManager.ResetAccessFailedCountAsync(user);
                await userManager.SetLockoutEndDateAsync(user, null);
                await userManager.SetLockoutEnabledAsync(user, false);
            }

            var checkResult = await signInManager.CheckPasswordSignInAsync(user, LModel.Password, lockoutOnFailure: true);
            if (checkResult.IsLockedOut)
            {
                await userManager.SetLockoutEnabledAsync(user, true);
                DateTimeOffset? newLockoutEnd = await userManager.GetLockoutEndDateAsync(user);
                TimeSpan remaining = newLockoutEnd.HasValue ? newLockoutEnd.Value - DateTimeOffset.UtcNow : TimeSpan.FromMinutes(5);
                ModelState.AddModelError("", $"Too many failed attempts. This account is locked. Try again in {FormatRemaining(remaining)}.");
                return Page();
            }

            if (!checkResult.Succeeded)
            {
                ModelState.AddModelError("", "Username or Password incorrect");
                return Page();
            }

            if (user.TwoFactorEnabled)
            {
                bool sent = await otpService.GenerateAndSendAsync(HttpContext.Session, Purpose, user.Email!, EmailSubject, BodyTemplate);
                if (!sent)
                {
                    ModelState.AddModelError("", "We couldn't send your verification code right now. Please try again later.");
                    return Page();
                }

                HttpContext.Session.SetString(PendingRememberMeKey, LModel.RememberMe.ToString());
                RequiresOtp = true;
                return Page();
            }

            await signInManager.SignInAsync(user, LModel.RememberMe);
            return RedirectToRoleHome();
        }

        public async Task<IActionResult> OnPostResendAsync()
        {
            string? pendingEmail = otpService.GetPendingEmail(HttpContext.Session, Purpose);

            if (string.IsNullOrEmpty(pendingEmail))
            {
                ClearPending();
                RequiresOtp = false;
                ModelState.AddModelError("", "Your session has expired. Please sign in again.");
                return Page();
            }

            var user = await userManager.FindByEmailAsync(pendingEmail);
            if (user == null)
            {
                ClearPending();
                RequiresOtp = false;
                ModelState.AddModelError("", "Username or Password incorrect");
                return Page();
            }

            bool sent = await otpService.GenerateAndSendAsync(HttpContext.Session, Purpose, pendingEmail, EmailSubject, BodyTemplate);

            RequiresOtp = true;

            if (!sent)
            {
                ModelState.AddModelError("", "We couldn't resend your verification code right now. Please try again later.");
                return Page();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostVerifyOtpAsync()
        {
            string? pendingEmail = otpService.GetPendingEmail(HttpContext.Session, Purpose);
            RequiresOtp = !string.IsNullOrEmpty(pendingEmail);

            if (string.IsNullOrEmpty(pendingEmail))
            {
                ClearPending();
                RequiresOtp = false;
                ModelState.AddModelError("", "Your verification code has expired. Please sign in again.");
                return Page();
            }

            var verifyResult = otpService.Verify(HttpContext.Session, Purpose, pendingEmail, OtpCode ?? string.Empty);
            if (verifyResult == OtpVerifyResult.Expired)
            {
                ClearPending();
                RequiresOtp = false;
                ModelState.AddModelError("", "Your verification code has expired. Please sign in again.");
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
                ClearPending();
                RequiresOtp = false;
                ModelState.AddModelError("", "Username or Password incorrect");
                return Page();
            }

            bool rememberMe = bool.TryParse(HttpContext.Session.GetString(PendingRememberMeKey), out bool remember) && remember;
            ClearPending();

            await signInManager.SignInAsync(user, LModel.RememberMe);
            return RedirectToRoleHome();
        }

        private void ClearPending()
        {
            otpService.Clear(HttpContext.Session, Purpose);
            HttpContext.Session.Remove(PendingRememberMeKey);
        }

        private static string FormatRemaining(TimeSpan remaining)
        {
            if (remaining < TimeSpan.Zero)
            {
                remaining = TimeSpan.Zero;
            }

            int minutes = (int)remaining.TotalMinutes;
            int seconds = remaining.Seconds;
            return minutes > 0 ? $"{minutes}m {seconds}s" : $"{seconds}s";
        }
    }
}
