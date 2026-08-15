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
        public bool RequestHelp { get; set; }
        public int WardId { get; set; }
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
        public string? DischargeReason { get; set; }
        public string? AdmissionNotes { get; set; }
        public bool IsDischarged => DischargeDateTime.HasValue;
        public List<MedicationListEditItem> MedicationLists { get; set; } = new();
        [Display(Name = "Medication")]
        public int? NewMedicationID { get; set; }
        [Display(Name = "Dosage")]
        public string? NewDosage { get; set; }
        public bool RequestHelp { get; set; }
    }
}
