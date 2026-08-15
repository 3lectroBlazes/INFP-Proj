using Microsoft.AspNetCore.Identity;

namespace INFP_Proj.Models
{
    public class AppUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public bool RequiresPasswordReset { get; set; } = false;
    }
}
