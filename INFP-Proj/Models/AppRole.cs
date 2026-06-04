using Microsoft.AspNetCore.Identity;

namespace INFP_Proj.Models
{
    public class AppRole : IdentityRole
    {
        public bool IsAdmin { get; set; } = false;
    }
}
