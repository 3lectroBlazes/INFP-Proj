using INFP_Proj.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class Log
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int LogID { get; set; }
        public string UserID { get; set; }
        public int? PatientID { get; set; }
        public required string Event { get; set; }
        public int? MedicationListID { get; set; }
        public bool Emergency { get; set; } = false;
        public bool Resolved { get; set; } = false;
        public bool selfAcknowledged { get; set; } = false;
        public bool relativeAcknowledged { get; set; } = false;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [ForeignKey("UserID")]
        public AppUser? User { get; set; }
        [ForeignKey("PatientID")]
        public Patients? Patient { get; set; }
        [ForeignKey("MedicationListID")]
        public MedicationList? MedicationList { get; set; }
    }
}