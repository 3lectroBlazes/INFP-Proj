using INFP_Proj.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace INFP_Proj.Data
{
    public class Log
    {
        [Key]
        public int LogID { get; set; }
        public string UserID { get; set; }
        public required string Event { get; set; }
        public bool Emergency { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserID")]
        public AppUser? User { get; set; }
    }
}