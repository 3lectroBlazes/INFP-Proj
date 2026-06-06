using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class BraceletRelation
    {
        public int PatientID { get; set; }
        public int BraceletID { get; set; }

        [ForeignKey("PatientID")]
        public Patients Patient { get; set; }
        [ForeignKey("BraceletID")]
        public Bracelet Bracelet { get; set; }
    }
}
