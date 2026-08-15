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
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<AppUser> userManager;
        private readonly IOtpService otpService;

        [BindProperty]
        public ResetPassword RPModel { get; set; }

        public ResetPasswordModel(UserManager<AppUser> userManager, IOtpService otpService)
        {
            this.userManager = userManager;
            this.otpService = otpService;
        }

        public IActionResult OnGet()
        {
            if (!HasPendingReset())
            {
                return RedirectToPage("/ForgotPassword");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!HasPendingReset()) return RedirectToPage("/ForgotPassword");
            if (!ModelState.IsValid) return Page();

            string email = otpService.GetPendingEmail(HttpContext.Session, ForgotPasswordModel.Purpose)
                           ?? HttpContext.Session.GetString("ForcedResetEmail")!;

            string token = HttpContext.Session.GetString(ForgotPasswordModel.ResetTokenSessionKey)
                           ?? HttpContext.Session.GetString("ForcedResetToken")!;

            var user = await userManager.FindByEmailAsync(email);
            if (user == null)
            {
                ClearSession();
                return RedirectToPage("/ForgotPassword");
            }

            var result = await userManager.ResetPasswordAsync(user, token, RPModel.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                return Page();
            }

            if (user.RequiresPasswordReset)
            {
                user.RequiresPasswordReset = false;
                await userManager.UpdateAsync(user);
            }

            ClearSession();
            return RedirectToPage("/Login");
        }

        private bool HasPendingReset()
        {
            bool hasOtpReset = !string.IsNullOrEmpty(otpService.GetPendingEmail(HttpContext.Session, ForgotPasswordModel.Purpose)) &&
                               !string.IsNullOrEmpty(HttpContext.Session.GetString(ForgotPasswordModel.ResetTokenSessionKey));

            bool hasForcedReset = !string.IsNullOrEmpty(HttpContext.Session.GetString("ForcedResetEmail")) &&
                                  !string.IsNullOrEmpty(HttpContext.Session.GetString("ForcedResetToken"));

            return hasOtpReset || hasForcedReset;
        }

        private void ClearSession()
        {
            otpService.Clear(HttpContext.Session, ForgotPasswordModel.Purpose);
            HttpContext.Session.Remove(ForgotPasswordModel.ResetTokenSessionKey);
            HttpContext.Session.Remove("ForcedResetEmail");
            HttpContext.Session.Remove("ForcedResetToken");
        }
    }
}
