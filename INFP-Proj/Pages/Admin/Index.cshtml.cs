using INFP_Proj.Data;
using INFP_Proj.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<PatientListItem> Patients { get; set; } = new List<PatientListItem>();

        public async Task OnGetAsync()
        {
            var patients = await _context.Patients
                .Include(p => p.User)
                .OrderBy(p => p.PatientID)
                .ToListAsync();

            var patientIds = patients.Select(p => p.PatientID).ToList();

            var medicationLists = await _context.MedicationLists
                .Include(m => m.Medications)
                .Where(m => patientIds.Contains(m.PatientID))
                .ToListAsync();

            var records = await _context.Records
                .Where(r => patientIds.Contains(r.PatientID))
                .ToListAsync();

            var beds = await _context.Beds
                .Where(b => patientIds.Contains((int)b.PatientID))
                .Select(b => new { b.PatientID, b.buttonPressed })
                .ToListAsync();

            Patients = patients.Select(p =>
            {
                var patientMeds = medicationLists.Where(m => m.PatientID == p.PatientID).ToList();
                var latestRecord = records
                    .Where(r => r.PatientID == p.PatientID)
                    .OrderByDescending(r => r.AdmissionDateTime)
                    .FirstOrDefault();

                var medSummary = patientMeds.Count == 0
                    ? "None"
                    : string.Join(", ", patientMeds.Select(m =>
                        $"{m.Medications?.MedicationName ?? "Unknown"} ({m.Dosage})"));

                var nurseCall = beds
                    .Any(b => b.PatientID == p.PatientID && b.buttonPressed);

                return new PatientListItem
                {
                    PatientId = p.PatientID,
                    PatientName = p.User != null
                        ? $"{p.User.FirstName} {p.User.LastName}"
                        : $"Patient #{p.PatientID}",
                    Status = p.Status,
                    MedicationsSummary = medSummary,
                    AdmissionDateTime = latestRecord?.AdmissionDateTime,
                    DischargeDateTime = latestRecord?.DischargeDateTime,
                    NurseCall = nurseCall
                };
            }).ToList();
        }
    }
}
