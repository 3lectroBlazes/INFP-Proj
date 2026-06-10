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
        private RoleManager<AppRole> roleManager { get; }

        [BindProperty]
        public Register RModel { get; set; }

        public RegisterModel(
            UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            RoleManager<AppRole> roleManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.roleManager = roleManager;
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
                    PhoneNumber = RModel.PhoneNumber,
                    FirstName = RModel.FirstName,
                    MiddleName = string.IsNullOrWhiteSpace(RModel.MiddleName) ? null : RModel.MiddleName.Trim(),
                    LastName = RModel.LastName
                };
                var result = await userManager.CreateAsync(user, RModel.Password);
                if (result.Succeeded)
                {
                    // Public registration only ever creates a plain "User".
                    // Patient and admin roles are assigned elsewhere (admission flow / seeder).
                    if (!await roleManager.RoleExistsAsync("User"))
                    {
                        await roleManager.CreateAsync(new AppRole { Name = "User", IsAdmin = false });
                    }
                    await userManager.AddToRoleAsync(user, "User");

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