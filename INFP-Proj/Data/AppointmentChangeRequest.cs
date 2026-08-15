namespace INFP_Proj.Data
{
    public class AppointmentChangeRequest
    {
        public int AppointmentChangeRequestID { get; set; }

        public int AppointmentRequestID { get; set; }

        public int PatientID { get; set; }

        public DateTime RequestedDateTime { get; set; }

        public string? Reason { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime RequestedAt { get; set; }

        public DateTime? ReviewedAt { get; set; }

        public string? ReviewMessage { get; set; }

        public string? ReviewedByUserID { get; set; }

        public Appointment? Appointment { get; set; }

        public Patients? Patient { get; set; }
    }
}