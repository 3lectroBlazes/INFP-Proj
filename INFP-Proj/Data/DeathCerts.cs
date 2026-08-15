using INFP_Proj.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class DeathCerts
    {
        [Key]
        public int DeathCertID { get; set; }

        public int PatientID { get; set; }
        public string DoctorID { get; set; }
        public int RecordID { get; set; }

        [Required]
        public string PatientName { get; set; } = string.Empty;

        [Required]
        public string FileName { get; set; } = string.Empty;

        [Required]
        public string ContentType { get; set; } = "application/pdf";

        [Required]
        public byte[] PdfData { get; set; } = Array.Empty<byte>();

        public string? RecordedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [ForeignKey("PatientID")]
        public Patients? Patient { get; set; }
        [ForeignKey("DoctorID")]
        public AppUser? User { get; set; }

    }
}