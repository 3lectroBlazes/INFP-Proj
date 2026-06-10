using INFP_Proj.Data;
using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Models
{
    public class AppointmentRequest
    {
        public int AppointmentRequestID { get; set; }

        public int PatientID { get; set; }
        public Patients? Patient { get; set; }

        [Required]
        public DateTime PreferredDateTime { get; set; }

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [Required]
        public string Urgency { get; set; } = "Normal";

        public string Status { get; set; } = "Pending";

        public string? DoctorResponse { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    }
}