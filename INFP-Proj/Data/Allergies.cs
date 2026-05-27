using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Data
{
    public class Allergies
    {
        [Key]
        public int AllergyID { get; set; }
        public required string Allergy { get; set; }
    }
}
