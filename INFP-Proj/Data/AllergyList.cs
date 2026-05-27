using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Data
{
    public class AllergyList
    {
        [Key]
        public int AllergyListID { get; set; }
        public int PatientID { get; set; }
        public int AllergyID { get; set; }
        public Patients? Patients { get; set; }
        public Allergies? Allergies { get; set; }
    }
}
