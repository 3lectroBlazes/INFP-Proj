using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Data
{
    public class MedicationList
    {
        [Key]
        public int MedicationListID { get; set; }
        public int PatientID { get; set; }
        public int MedicationID { get; set; }
        public required string Dosage { get; set; }
        public Patients? Patients { get; set; }
        public Medications? Medications { get; set; }
    }
}
