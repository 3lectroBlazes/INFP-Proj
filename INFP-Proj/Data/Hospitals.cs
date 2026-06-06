using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class Hospitals
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int HospitalID { get; set; }
        public required string HospitalName { get; set; }
        public required string HospitalAddress { get; set; }
    }
}
