using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.ViewModel
{
    public class RegisterPatientViewModel
    {
        [Display(Name = "Registration Mode")]
        public string Mode { get; set; } = "New";

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; }

        [EmailAddress]
        [Display(Name = "Patient Email")]
        public string? Email { get; set; }

        [Display(Name = "Registered User")]
        public string? ExistingUserId { get; set; }

        [Required(ErrorMessage = "Please select an available bracelet.")]
        [Display(Name = "Assigned Bracelet")]
        public int BraceletID { get; set; }

        [Required(ErrorMessage = "Please select an available bed.")]
        [Display(Name = "Assigned Bed")]
        public int BedID { get; set; }

        [Required(ErrorMessage = "Please select a diagnosis.")]
        [Display(Name = "Diagnosis")]
        public int DiagnosisID { get; set; }

        [Required(ErrorMessage = "Admission notes are required.")]
        [Display(Name = "Admission Notes")]
        public string AdmissionNotes { get; set; } = string.Empty;
    }
}
