using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using INFP_Proj.Models;
using INFP_Proj.Services;

namespace INFP_Proj.Pages.Admin.Reception
{
    public class AdminManagementModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly IEmailService _emailService;

        public AdminManagementModel(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, IEmailService emailService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _emailService = emailService;
        }

        public List<AdminViewModel> AdminUsers { get; set; } = new List<AdminViewModel>();

        public class AdminViewModel
        {
            public string Id { get; set; }
            public string FirstName { get; set; }
            public string MiddleName { get; set; }
            public string LastName { get; set; }
            public string Email { get; set; }
            public string Role { get; set; }
        }

        public async Task OnGetAsync()
        {
            var adminRoles = await _roleManager.Roles
                .Where(r => r.IsAdmin)
                .ToListAsync();

            var adminRoleNames = adminRoles.Select(r => r.Name).ToList();
            var users = await _userManager.Users.ToListAsync();

            foreach (var user in users)
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var assignedAdminRole = userRoles.FirstOrDefault(r => adminRoleNames.Contains(r));

                if (assignedAdminRole != null)
                {
                    AdminUsers.Add(new AdminViewModel
                    {
                        Id = user.Id,
                        FirstName = user.FirstName,
                        MiddleName = user.MiddleName,
                        LastName = user.LastName,
                        Email = user.Email,
                        Role = assignedAdminRole
                    });
                }
            }
        }

        public async Task<IActionResult> OnPostCreateAsync(string firstName, string middleName, string lastName, string email, string roleName)
        {
            var newUser = new AppUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                MiddleName = middleName,
                LastName = lastName,
                EmailConfirmed = true,
                RequiresPasswordReset = true
            };

            string temporaryPassword = Guid.NewGuid().ToString() + "A1!";

            var result = await _userManager.CreateAsync(newUser, temporaryPassword);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(newUser, roleName);

                string subject = "Admin Account Created - Temporary Password";
                string message = $"Hello {firstName},\n\n" +
                                 $"An admin account has been created for you.\n" +
                                 $"Your temporary password is: {temporaryPassword}\n\n" +
                                 $"Please set your actual password using this webpage: https://localhost/ResetPassword";

                await _emailService.SendEmailAsync(email, subject, message);

                await _emailService.SendEmailAsync("elsw185@gmail.com", subject, message);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync(string id, string firstName, string middleName, string lastName, string email, string roleName)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return RedirectToPage();
            }

            user.FirstName = firstName;
            user.MiddleName = middleName;
            user.LastName = lastName;
            user.Email = email;
            user.UserName = email;

            var updateResult = await _userManager.UpdateAsync(user);

            if (updateResult.Succeeded)
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                await _userManager.AddToRoleAsync(user, roleName);
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                await _userManager.DeleteAsync(user);
            }

            return RedirectToPage();
        }
    }
}