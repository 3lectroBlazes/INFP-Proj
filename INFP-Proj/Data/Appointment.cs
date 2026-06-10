using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class Appointment
    {
        [Key]
        public int AppointmentRequestID { get; set; }

        public int PatientID { get; set; }

        public required DateTime PreferredDateTime { get; set; }

        [StringLength(500)]
        public required string Reason { get; set; } = string.Empty;

        public string Urgency { get; set; } = "Normal";

        public string Status { get; set; } = "Pending";

        public string? DoctorResponse { get; set; }

        public DateTime RequestedAt { get; set; } = DateTime.Now;


        [ForeignKey("PatientID")]
        public Patients? Patient { get; set; }
    }
}
