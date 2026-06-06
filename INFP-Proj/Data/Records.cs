using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        [ForeignKey("PatientID")]
        public Patients? Patients { get; set; }
        [ForeignKey("BedID")]
        public Beds? Beds { get; set; }
        [ForeignKey("MedicationListID")]
        public MedicationList? MedicationList { get; set; }
        [ForeignKey("WardID")]
        public Wards? Wards { get; set; }
        [ForeignKey("HospitalID")]
        public Hospitals? Hospitals { get; set; }
        [ForeignKey("DiagnosisID")]
        public Diagnoses? Diagnoses { get; set; }
    }
}
