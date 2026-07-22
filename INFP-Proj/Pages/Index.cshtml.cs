using INFP_Proj.Data;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace INFP_Proj.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        private readonly AppDbContext _context;

        public IndexModel(ILogger<IndexModel> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public IList<PatientListItem> Patients { get; set; } = new List<PatientListItem>();

        public async Task OnGetAsync()
        { 
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
}