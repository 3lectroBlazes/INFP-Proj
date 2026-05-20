using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Data
{
    public class Hospitals
    {
        [Key]
        public int HospitalID { get; set; }
        public required string HospitalName { get; set; }
        public required string HospitalAddress { get; set; }
    }
}
