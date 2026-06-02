using INFP_Proj.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Razor.Language.Intermediate;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Model
{
    public class AuthLogin : IdentityDbContext<AppUser>
    {
        private readonly IConfiguration _configuration;

        // public AuthLogin(DbContextOptions<AuthLogin> options) : base(options) { }

        public AuthLogin(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            string connectionString = _configuration.GetConnectionString("AuthConnectionString");
            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}