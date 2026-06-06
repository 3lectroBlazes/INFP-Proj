using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class Allergies
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int AllergyID { get; set; }
        public required string Allergy { get; set; }
    }
}
