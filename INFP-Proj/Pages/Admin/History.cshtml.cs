using INFP_Proj.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin
{
    public class HistoryModel : PageModel
    {
        private readonly AppDbContext _context;

        public HistoryModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<AdmissionHistoryItem> Admissions { get; set; } = new List<AdmissionHistoryItem>();
        public SelectList PatientOptions { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        public int? PatientId { get; set; }

        public async Task OnGetAsync()
        {
            await PopulatePatientOptionsAsync();

            var query = _context.Records
                .Include(r => r.Patients)!.ThenInclude(p => p!.User)
                .Include(r => r.Wards)
                .Include(r => r.Beds)
                .Include(r => r.Diagnoses)
                .Include(r => r.Hospitals)
                .AsQueryable();

            if (PatientId is > 0)
            {
                query = query.Where(r => r.PatientID == PatientId);
            }

            Admissions = await query
                .OrderByDescending(r => r.AdmissionDateTime)
                .Select(r => new AdmissionHistoryItem
                {
                    RecordId = r.RecordID,
                    PatientId = r.PatientID,
                    PatientName = r.Patients != null && r.Patients.User != null
                        ? $"{r.Patients.User.FirstName} {r.Patients.User.LastName}"
                        : $"Patient #{r.PatientID}",
                    Ward = r.Wards != null ? r.Wards.WardName : $"Ward #{r.WardID}",
                    Bed = r.Beds != null ? $"Bed #{r.Beds.BedID} (Room {r.Beds.Room}, Sector {r.Beds.Sector})" : $"Bed #{r.BedID}",
                    Diagnosis = r.Diagnoses != null ? r.Diagnoses.DiagnosisName : $"Diagnosis #{r.DiagnosisID}",
                    Hospital = r.Hospitals != null ? r.Hospitals.HospitalName : $"Hospital #{r.HospitalID}",
                    Description = r.Description,
                    AdmissionDateTime = r.AdmissionDateTime,
                    DischargeDateTime = r.DischargeDateTime
                })
                .ToListAsync();
        }

        private async Task PopulatePatientOptionsAsync()
        {
            var patients = await _context.Patients
                .Include(p => p.User)
                .OrderBy(p => p.PatientID)
                .Select(p => new
                {
                    p.PatientID,
                    Name = p.User != null
                        ? p.User.FirstName + " " + p.User.LastName
                        : "Patient #" + p.PatientID
                })
                .ToListAsync();

            PatientOptions = new SelectList(patients, "PatientID", "Name", PatientId);
        }
    }

    public class AdmissionHistoryItem
    {
        public int RecordId { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;
        public string Ward { get; set; } = string.Empty;
        public string Bed { get; set; } = string.Empty;
        public string Diagnosis { get; set; } = string.Empty;
        public string Hospital { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime AdmissionDateTime { get; set; }
        public DateTime? DischargeDateTime { get; set; }

        public bool IsActive => DischargeDateTime == null;
    }
}
