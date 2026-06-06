using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class MedicationList
    {
        [Key]
        public int MedicationListID { get; set; }
        public int PatientID { get; set; }
        public int MedicationID { get; set; }
        public required string Dosage { get; set; }

        [ForeignKey("PatientID")]
        public Patients? Patients { get; set; }
        [ForeignKey("MedicationID")]
        public Medications? Medications { get; set; }
    }
}
