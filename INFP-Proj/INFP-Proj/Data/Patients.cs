using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Data
{
    public class Patients
    {
        [Key]
        public int PatientID { get; set; }
        public int BraceletID { get; set; }
        public int UserID { get; set; }
        public required string Status { get; set; }

        public Bracelet? Bracelet { get; set; }
        public User? User { get; set; }
        public List<AllergyList>? AllergyLists { get; set; } = new();
        public List<Relationships> Relationships { get; set; } = new();
    }
}
