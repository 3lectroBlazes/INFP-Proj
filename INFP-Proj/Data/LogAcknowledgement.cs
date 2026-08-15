namespace INFP_Proj.Data
{
    public class LogAcknowledgement
    {
        public int LogAcknowledgementID { get; set; }

        public int LogID { get; set; }

        public string UserID { get; set; } =
            string.Empty;

        public DateTime AcknowledgedAt { get; set; }
    }
}