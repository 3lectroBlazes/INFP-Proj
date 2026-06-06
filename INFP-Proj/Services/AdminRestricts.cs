using INFP_Proj.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace INFP_Proj.Services
{
    public class AdminRestricts : UserClaimsPrincipalFactory<AppUser, AppRole>
    {
        public AdminRestricts(             
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            IOptions<IdentityOptions> optionsAccessor)
            : base(userManager, roleManager, optionsAccessor) { }

        protected override async Task<ClaimsIdentity> GenerateClaimsAsync(AppUser user)
        {
            var identity = await base.GenerateClaimsAsync(user);
            var roles = await UserManager.GetRolesAsync(user);

            bool isAdmin = false;    
            foreach (var roleName in roles)                  
            {
                var role = await RoleManager.FindByNameAsync(roleName);
                if (role != null && role.IsAdmin)
                {
                    isAdmin = true;
                    break;
                }
            }

            identity.AddClaim(new Claim("IsAdmin", isAdmin.ToString()));
            return identity;
        }
    }
}