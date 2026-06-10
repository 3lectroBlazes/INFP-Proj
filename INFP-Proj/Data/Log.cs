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
        public int? MedicationID { get; set; }
        public string? Dosage { get; set; }
        public bool Emergency { get; set; } = false;
        public bool Resolved { get; set; } = false;
        public DateTime Timestamp { get; set; } = DateTime.Now;

        [ForeignKey("UserID")]
        public AppUser? User { get; set; }
        [ForeignKey("PatientID")]
        public Patients? Patient { get; set; }
        [ForeignKey("MedicationID")]
        public Medications? Medication { get; set; }
    }
}