using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.ViewModel
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
        public bool NurseCall { get; set; } = false;
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

    public class PatientAdmissionInput
    {
        [Required(ErrorMessage = "Please select a user to admit.")]
        [Display(Name = "User")]
        public string? UserId { get; set; }

        [Display(Name = "Bracelet")]
        public int? BraceletID { get; set; }

        [Required(ErrorMessage = "Please select a bed.")]
        [Display(Name = "Bed")]
        public int BedID { get; set; }

        [Required(ErrorMessage = "Please select a ward.")]
        [Display(Name = "Ward")]
        public int WardID { get; set; }

        [Required(ErrorMessage = "Please select a diagnosis.")]
        [Display(Name = "Diagnosis")]
        public int DiagnosisID { get; set; }

        [Display(Name = "Medications")]
        public List<int> MedicationIDs { get; set; } = new();

        [Display(Name = "Admission notes")]
        public string? Description { get; set; }
    }
}
