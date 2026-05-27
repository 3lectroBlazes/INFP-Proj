using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Data
{
    public class Bracelet
    {
        [Key]
        public int BraceletID { get; set; }
        public int PatientID { get; set; }
        public float? Battery { get; set; }
        public float? Respiration { get; set; }
        public string? Location { get; set; }
        public float? Movement { get; set; }
        public float? BloodPressure { get; set; }
        public float? HeartRate { get; set; }

        public Patients? Patients { get; set; }   
}
}
