using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Data
{
    public class Beds
    {
        [Key]
        public int BedID { get; set; }
        public int? PatientID { get; set; }
        public int WardID { get; set; }
        public required string Sector { get; set; }
        public required string Floor { get; set; }
        public required string Room { get; set; }
        public float Temperature { get; set; }
        public float Weight { get; set; }
        public string? Location { get; set; }  
        public Patients? Patients { get; set; }
        public Wards? Wards { get; set; }
    }
}
