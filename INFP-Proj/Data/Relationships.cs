using INFP_Proj.Models;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class Relationships
    {
        public int PatientID { get; set; }
        public string UserID { get; set; }

        [ForeignKey("PatientID")]
        public Patients? Patient { get; set; }
    }
}