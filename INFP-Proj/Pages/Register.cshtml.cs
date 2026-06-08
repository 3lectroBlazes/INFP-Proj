using INFP_Proj.Models;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INFP_Proj.Pages
{
    [AllowAnonymous]
    public class RegisterModel : PageModel
    {
        private UserManager<AppUser> userManager { get; }
        private SignInManager<AppUser> signInManager { get; }

        [BindProperty]
        public Register RModel { get; set; }

        public RegisterModel(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (ModelState.IsValid)
            {
                var user = new AppUser()
                {
                    UserName = RModel.Email,
                    Email = RModel.Email,
                    FirstName = string.Empty,
                    LastName = string.Empty
                };
                var result = await userManager.CreateAsync(user, RModel.Password);
                if (result.Succeeded)
                {
                    await signInManager.SignInAsync(user, false);
                    return RedirectToPage("Index");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return Page();
        }
    }
}