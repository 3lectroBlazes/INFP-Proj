using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class AllergyList
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AllergyListID { get; set; }
        public int PatientID { get; set; }
        public int AllergyID { get; set; }

        [ForeignKey("PatientID")]
        public Patients? Patients { get; set; }
        [ForeignKey("AllergyID")]
        public Allergies? Allergies { get; set; }
    }
}
