using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Data
{
    public class User
    {
        [Key]
        public int UserID { get; set; }
        public required string FirstName { get; set; }
        public string? MiddleName { get; set; }
        public required string LastName { get; set; }

        public required string Email { get; set; }

        public required string Role { get; set; }

        public required string PasswordHash { get; set; }
        public List<Relationships> Relationships { get; set; } = new();
    }
}
