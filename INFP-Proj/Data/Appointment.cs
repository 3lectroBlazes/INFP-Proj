using INFP_Proj.Models;
using Microsoft.AspNetCore.Identity;
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
        [StringLength(500)]
        public string DoctorID { get; set; } = null!;
        public required string Reason { get; set; } = string.Empty;
        public string Urgency { get; set; } = "Normal";
        public string Status { get; set; } = "Pending";
        public string? DoctorResponse { get; set; }
        public bool DocAcknowledged { get; set; } = false;
        public bool PatientAcknowledged { get; set; } = false;
        public required DateTime DateTime { get; set; }
        public DateTime RequestedAt { get; set; } = DateTime.Now;


        [ForeignKey("PatientID")]
        public Patients? Patient { get; set; }
        [ForeignKey("DoctorID")]
        public AppUser? User { get; set; }
    }
}
