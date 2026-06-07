namespace INFP_Proj.Models
{
    public class LogListItem
    {
        public int LogId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Event { get; set; } = string.Empty;
        public bool Emergency { get; set; }
        public bool Resolved { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
