using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.ViewModel
{
    public class RegisterPatientViewModel
    {
        [Required(ErrorMessage = "First Name is required.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = string.Empty;

        [Display(Name = "Middle Name (Optional)")]
        public string? MiddleName { get; set; }

        [Required(ErrorMessage = "Last Name is required.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Patient Email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select an available bracelet.")]
        [Display(Name = "Assigned Bracelet")]
        public int BraceletID { get; set; }

        [Required(ErrorMessage = "Please select an available bed.")]
        [Display(Name = "Assigned Bed")]
        public int BedID { get; set; }

        [Required(ErrorMessage = "Reason for admission is required.")]
        [Display(Name = "Reason for Admission")]
        public string Description { get; set; } = string.Empty;
    }
}
