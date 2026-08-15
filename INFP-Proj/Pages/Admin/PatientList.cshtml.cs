using INFP_Proj.Data;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private const int UnassignedWardId = -1;

        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<PatientListItem> Patients { get; set; } = new List<PatientListItem>();
        public List<IGrouping<int, PatientListItem>> PatientsByWard { get; set; } = new();
        public Dictionary<int, Wards> WardsById { get; set; } = new();
        public Dictionary<int, int> OccupancyByWard { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        public List<SelectListItem> StatusOptions { get; set; } = new()
        {
            new SelectListItem("All statuses", ""),
            new SelectListItem("Admitted", "Admitted"),
            new SelectListItem("Observed", "Observed"),
            new SelectListItem("Discharged", "Discharged")
        };

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
                .Include(r => r.Wards)
                .Where(r => patientIds.Contains(r.PatientID))
                .ToListAsync();

            var wards = await _context.Wards.ToListAsync();
            WardsById = wards.ToDictionary(w => w.WardID, w => w);

            // Occupancy comes from actual bed assignments, same logic as the Reception ward/bed dashboard.
            var allBeds = await _context.Beds.ToListAsync();
            OccupancyByWard = allBeds
                .Where(b => b.PatientID != null)
                .GroupBy(b => b.WardID)
                .ToDictionary(g => g.Key, g => g.Count());

            Patients = patients.Select(p =>
            {
                var latestRecord = records
                    .Where(r => r.PatientID == p.PatientID)
                    .OrderByDescending(r => r.AdmissionDateTime)
                    .FirstOrDefault();

                var patientMeds = medicationLists
                    .Where(m => m.PatientID == p.PatientID
                        && latestRecord != null
                        && m.RecordID == latestRecord.RecordID)
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
                    DischargeDateTime = latestRecord?.DischargeDateTime,
                    RequestHelp = p.RequestHelp,
                    WardId = latestRecord?.WardID ?? UnassignedWardId
                };
            })
            .Where(p => string.IsNullOrEmpty(StatusFilter)
                || string.Equals(p.Status, StatusFilter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => WardsById.TryGetValue(p.WardId, out var w) ? w.WardName : "Unassigned")
            .ThenBy(p => p.PatientName)
            .ToList();

            PatientsByWard = Patients
                .GroupBy(p => p.WardId)
                .OrderBy(g => g.Key == UnassignedWardId ? int.MaxValue : g.Key)
                .ToList();
        }
    }
}