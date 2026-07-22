using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Data
{
    public class Appointment
    {
        [Key]
        public int AppointmentRequestID { get; set; }

        public int PatientID { get; set; }

        public string Reason { get; set; } = string.Empty;

        public string Urgency { get; set; } = "Normal";

        public string Status { get; set; } = "Pending";

        public string? DoctorResponse { get; set; }

        public bool DocAcknowledged { get; set; }

        public bool PatientAcknowledged { get; set; }

        public DateTime AppointmentDate { get; set; }

        public DateTime RequestedAt { get; set; }

        [ForeignKey("PatientID")]
        public Patients? Patient { get; set; }
    }
}