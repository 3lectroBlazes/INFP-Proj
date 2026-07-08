using INFP_Proj.Models;
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

        [BindProperty]
        public ResetPassword RPModel { get; set; }

        public ResetPasswordModel(UserManager<AppUser> userManager)
        {
            this.userManager = userManager;
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
            if (!HasPendingReset())
            {
                return RedirectToPage("/ForgotPassword");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            string email = HttpContext.Session.GetString(ForgotPasswordModel.OtpEmailSessionKey)!;
            string token = HttpContext.Session.GetString(ForgotPasswordModel.ResetTokenSessionKey)!;

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

            ClearSession();
            return RedirectToPage("/Login");
        }

        private bool HasPendingReset()
        {
            return !string.IsNullOrEmpty(HttpContext.Session.GetString(ForgotPasswordModel.OtpEmailSessionKey)) &&
                   !string.IsNullOrEmpty(HttpContext.Session.GetString(ForgotPasswordModel.ResetTokenSessionKey));
        }

        private void ClearSession()
        {
            HttpContext.Session.Remove(ForgotPasswordModel.OtpSessionKey);
            HttpContext.Session.Remove(ForgotPasswordModel.OtpEmailSessionKey);
            HttpContext.Session.Remove(ForgotPasswordModel.OtpExpirySessionKey);
            HttpContext.Session.Remove(ForgotPasswordModel.ResetTokenSessionKey);
        }
    }
}
