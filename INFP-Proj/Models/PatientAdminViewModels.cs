using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Models
{
    public class PatientListItem
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string MedicationsSummary { get; set; } = string.Empty;
        public DateTime? AdmissionDateTime { get; set; }
        public DateTime? DischargeDateTime { get; set; }
        public bool IsDischarged => DischargeDateTime.HasValue;
    }

    public class MedicationListEditItem
    {
        public int MedicationListID { get; set; }

        [Required]
        [Display(Name = "Medication")]
        public int MedicationID { get; set; }

        [Required]
        [Display(Name = "Dosage")]
        public string Dosage { get; set; } = string.Empty;
    }

    public class PatientEditViewModel
    {
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime? AdmissionDateTime { get; set; }
        public DateTime? DischargeDateTime { get; set; }
        public bool IsDischarged => DischargeDateTime.HasValue;
        public List<MedicationListEditItem> MedicationLists { get; set; } = new();

        [Display(Name = "Medication")]
        public int? NewMedicationID { get; set; }

        [Display(Name = "Dosage")]
        public string? NewDosage { get; set; }
    }

    public class RegisterPatientInput
    {
        /// <summary>existing or new</summary>
        public string AccountMode { get; set; } = "existing";

        [Display(Name = "Existing patient account")]
        public string? ExistingUserId { get; set; }

        [Display(Name = "First name")]
        public string? NewFirstName { get; set; }

        [Display(Name = "Middle name")]
        public string? NewMiddleName { get; set; }

        [Display(Name = "Last name")]
        public string? NewLastName { get; set; }

        [Display(Name = "Email")]
        [EmailAddress]
        public string? NewEmail { get; set; }

        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string? NewPassword { get; set; }

        [Display(Name = "Bracelet")]
        public int BraceletID { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = "Admitted";
    }
}
