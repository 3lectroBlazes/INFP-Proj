using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.ViewModel
{
    public class Register
    {
        [Required]
        [Display(Name = "First name")]
        public required string FirstName { get; set; }

        [Display(Name = "Middle name")]
        public string? MiddleName { get; set; }

        [Required]
        [Display(Name = "Last name")]
        public required string LastName { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        public required string Email { get; set; }

        [Required]
        [Phone]
        [Display(Name = "Phone number")]
        public required string PhoneNumber { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public required string Password { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Password and confirmation password does not match")]
        public required string ConfirmPassword { get; set; }
    }
}