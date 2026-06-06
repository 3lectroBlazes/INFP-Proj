using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class BloodWork
    {
        public int BloodWorkID { get; set; }
        public int PatientID { get; set; }
        public DateTime TestDateTime { get; set; }
        public string BloodType { get; set; } = string.Empty;
        public string Cholesterol { get; set; } = string.Empty;
        public string BloodSugar { get; set; } = string.Empty;
        public string Hemoglobin { get; set; } = string.Empty;
        public string TotalBloodCount { get; set; } = string.Empty;

        [ForeignKey("PatientID")]
        public Patients? Patient { get; set; }
    }
}
