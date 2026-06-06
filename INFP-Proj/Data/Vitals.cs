using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class Vitals
    {
        [Key]
        public int VitalsID { get; set; }
        public int PatientID { get; set; }
        public float? BloodPressure { get; set; }
        public float? HeartRate { get; set; }
        public float? RespiratoryRate { get; set; }
        public float? Temperature { get; set; }
        public DateTime RecordedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("PatientID")]
        public Patients? Patients { get; set; }
    }
}
