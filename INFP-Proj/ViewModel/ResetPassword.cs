using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.ViewModel
{
    public class ResetPassword
    {
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "New password")]
        public required string NewPassword { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare(nameof(NewPassword), ErrorMessage = "Password and confirmation password does not match")]
        public required string ConfirmPassword { get; set; }
    }
}
