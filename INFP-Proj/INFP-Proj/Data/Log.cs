using INFP_Proj.Models;
using System.ComponentModel.DataAnnotations;
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
        public AppUser? User { get; set; } 
    }
}