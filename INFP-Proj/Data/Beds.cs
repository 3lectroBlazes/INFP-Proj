using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public bool buttonPressed { get; set; } = false;

        [ForeignKey("PatientID")]
        public Patients? Patients { get; set; }
        [ForeignKey("WardID")]
        public virtual Wards? Wards { get; set; }
    }
}
