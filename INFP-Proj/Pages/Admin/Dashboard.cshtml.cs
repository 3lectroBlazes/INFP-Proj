using INFP_Proj.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin
{
    public class DashboardModel : PageModel
    {
        private const int MaxEmergencyLogs = 10;

        private readonly AppDbContext _context;

        public DashboardModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<DoctorRequestItem> DoctorRequests { get; set; } = new List<DoctorRequestItem>();
        public IList<EmergencyLogItem> EmergencyLogs { get; set; } = new List<EmergencyLogItem>();

        public async Task OnGetAsync()
        {
            DoctorRequests = await _context.DoctorRequests
                .Where(dr => !dr.Completed)
                .OrderByDescending(dr => dr.RequestDate)
                .Select(dr => new DoctorRequestItem
                {
                    DoctorRequestId = dr.DoctorRequestID,
                    PatientId = dr.PatientID,
                    PatientName = dr.Patient != null && dr.Patient.User != null
                        ? $"{dr.Patient.User.FirstName} {dr.Patient.User.LastName}"
                        : $"Patient #{dr.PatientID}",
                    RequestMessage = dr.RequestMessage,
                    RequestDate = dr.RequestDate
                })
                .ToListAsync();

            EmergencyLogs = await _context.Logs
                .Where(l => l.Emergency && !l.Resolved)
                .OrderByDescending(l => l.Timestamp)
                .Take(MaxEmergencyLogs)
                .Select(l => new EmergencyLogItem
                {
                    Event = l.Event,
                    UserName = l.User != null
                        ? $"{l.User.FirstName} {l.User.LastName}"
                        : $"User #{l.UserID}",
                    Timestamp = l.Timestamp
                })
                .ToListAsync();
        }
    }

    public class DoctorRequestItem
    {
        public int DoctorRequestId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string RequestMessage { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
    }

    public class EmergencyLogItem
    {
        public string Event { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }
}
