using INFP_Proj.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class Patients
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PatientID { get; set; }
        public string UserID { get; set; }
        public required string Status { get; set; }
        public List<AllergyList>? AllergyLists { get; set; } = new();
        public List<Relationships> Relationships { get; set; } = new();
        public string? Notes { get; set; }

        [ForeignKey("UserID")]
        public AppUser? User { get; set; }
        public ICollection<BraceletRelation> BraceletRelations { get; set; }
    }
}