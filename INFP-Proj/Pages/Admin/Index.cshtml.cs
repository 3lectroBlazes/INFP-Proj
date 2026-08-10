using INFP_Proj.Data;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;


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
        public IList<PatientListItem> Patients { get; set; } = new List<PatientListItem>();

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
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isUserRole = User.IsInRole("User");

            List<Patients> patients;

            if (isUserRole && userId != null)
            {
                var relatedPatientIds = await _context.Relationships
                    .Where(r => r.UserID == userId)
                    .Select(r => r.PatientID)
                    .ToListAsync();

                patients = await _context.Patients
                    .Include(p => p.User)
                    .Where(p => p.UserID == userId || relatedPatientIds.Contains(p.PatientID))
                    .OrderBy(p => p.PatientID)
                    .ToListAsync();
            }
            else
            {
                patients = await _context.Patients
                    .Include(p => p.User)
                    .OrderBy(p => p.PatientID)
                    .ToListAsync();
            }

            var patientIds = patients.Select(p => p.PatientID).ToList();

            var medicationLists = await _context.MedicationLists
                .Include(m => m.Medications)
                .Where(m => patientIds.Contains(m.PatientID))
                .ToListAsync();

            var records = await _context.Records
                .Where(r => patientIds.Contains(r.PatientID))
                .ToListAsync();

            Patients = patients.Select(p =>
            {
                var latestRecord = records
                    .Where(r => r.PatientID == p.PatientID)
                    .OrderByDescending(r => r.AdmissionDateTime)
                    .FirstOrDefault();

                var patientMeds = medicationLists
                    .Where(m => m.PatientID == p.PatientID
                        && (latestRecord == null || m.MedicationListID >= latestRecord.MedicationListID))
                    .ToList();

                var medSummary = patientMeds.Count == 0
                    ? "None"
                    : string.Join(", ", patientMeds.Select(m =>
                        $"{m.Medications?.MedicationName ?? "Unknown"} ({m.Dosage})"));

                return new PatientListItem
                {
                    PatientId = p.PatientID,
                    PatientName = p.User != null
                        ? $"{p.User.FirstName} {p.User.LastName}"
                        : $"Patient #{p.PatientID}",
                    Status = p.Status,
                    MedicationsSummary = medSummary,
                    AdmissionDateTime = latestRecord?.AdmissionDateTime,
                    DischargeDateTime = latestRecord?.DischargeDateTime
                };
            }).ToList();
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
