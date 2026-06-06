using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class Bracelet
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BraceletID { get; set; }
        public float? Battery { get; set; }
        public float? Respiration { get; set; }
        public string? Location { get; set; }
        public float? Movement { get; set; }
        public float? BloodPressure { get; set; }
        public float? HeartRate { get; set; }

        public ICollection<BraceletRelation> BraceletRelations { get; set; }
    }
}
