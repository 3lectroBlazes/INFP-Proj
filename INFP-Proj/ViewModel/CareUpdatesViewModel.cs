namespace INFP_Proj.ViewModel
{
    public class CareUpdatesViewModel
    {
        public bool HasPatientRecord { get; set; }

        public int PatientId { get; set; }
        public string PatientName { get; set; } = "Unknown patient";

        public AppointmentPreview UpcomingAppointment { get; set; } = new();

        public bool AppointmentAcknowledged { get; set; }
        public string? AppointmentChangeRequest { get; set; }

        public List<DoctorCommunicationItem> DoctorCommunications { get; set; } = new();
    }

    public class AppointmentPreview
    {
        public string Title { get; set; } = "Follow-up Review";
        public string DoctorName { get; set; } = "Dr Xavier Wee";
        public DateTime AppointmentDateTime { get; set; }
        public string Location { get; set; } = "General Ward Consultation Room";
        public string Status { get; set; } = "Scheduled";
        public string Purpose { get; set; } = "Review patient condition, medication, and latest vitals.";
    }

    public class DoctorCommunicationItem
    {
        public int DoctorRequestId { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ReplyMessage { get; set; } = string.Empty;
        public bool Completed { get; set; }
        public DateTime RequestDate { get; set; }
    }
}