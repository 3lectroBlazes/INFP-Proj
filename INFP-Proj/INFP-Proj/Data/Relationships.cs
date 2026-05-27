using INFP_Proj.Model;
using INFP_Proj.Models;

namespace INFP_Proj.Data
{
    public class Relationships
    {
        public int PatientID { get; set; }
        public string UserID { get; set; } 
        public Patients? Patient { get; set; }
        public AppUser? User { get; set; } 
    }
}