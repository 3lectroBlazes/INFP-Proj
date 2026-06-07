using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace INFP_Proj.Data
{
    public class DoctorRequest
    {
        public int DoctorRequestID { get; set; }
        public int PatientID { get; set; }
        public string RequestMessage { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.UtcNow;
        public string? ReplyMessage { get; set; }
        public bool Completed { get; set; } = false;

        [ForeignKey("PatientID")]
        public Patients? Patient { get; set; }
    }
}
