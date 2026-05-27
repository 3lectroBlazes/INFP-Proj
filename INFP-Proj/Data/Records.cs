using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Data
{
    public class Records
    {
        [Key]
        public int RecordID { get; set; }
        public int PatientID { get; set; }
        public int BedID { get; set; }
        public int WardID { get; set; }
        public int HospitalID { get; set; }
        public int DiagnosisID { get; set; }
        public int MedicationListID { get; set; }
        public required string Description { get; set; }
        public required DateTime AdmissionDateTime { get; set; } = DateTime.UtcNow;
        public DateTime? DischargeDateTime { get; set; }
        public Patients? Patients { get; set; }
        public Beds? Beds { get; set; }
        public MedicationList? MedicationList { get; set; }
        public Wards? Wards { get; set; }
        public Hospitals? Hospitals { get; set; }
        public Diagnoses? Diagnoses { get; set; }
    }
}
