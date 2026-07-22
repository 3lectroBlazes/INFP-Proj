namespace INFP_Proj.ViewModel
{
    public class LogListItem
    {
        public int LogId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Event { get; set; } = string.Empty;
        public bool Emergency { get; set; }
        public bool Resolved { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsMedicationRequest { get; set; }
        public bool SelfAcknowledged { get; set; }
        public bool RelativeAcknowledged { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? PatientName { get; set; }
        public bool CurrentUserAcknowledged { get; set; }
        public bool CanAcknowledge { get; set; }
        public bool CanResolve => SelfAcknowledged || RelativeAcknowledged;
    }
}