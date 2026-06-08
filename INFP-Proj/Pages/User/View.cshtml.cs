using INFP_Proj.Data;
using INFP_Proj.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.User
{
    public class ViewModel : PageModel
    {
        private readonly AppDbContext _context;

        public ViewModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public PatientEditViewModel Patient { get; set; } = new();

        public SelectList MedicationOptions { get; set; } = default!;
        public bool IsRelated { get; set; }
        public bool IsOwner { get; set; }

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var IsRelated = await _context.Relationships
                .Where(r => r.UserID == userId && r.PatientID == id.Value)
                .AnyAsync();

            var IsOwner = await _context.Patients
                .AnyAsync(p => p.PatientID == id.Value && p.UserID == userId);

            if (!IsRelated && !IsOwner)
                return Forbid();

            var loaded = await LoadPatientAsync(id.Value);

            if (loaded == null) return NotFound();
            Patient = loaded;

            await PopulateMedicationOptionsAsync();
            return Page();
        }

        private async Task<PatientEditViewModel?> LoadPatientAsync(int patientId)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientID == patientId);

            if (patient == null)
            {
                return null;
            }

            var medications = await _context.MedicationLists
                .Where(m => m.PatientID == patientId)
                .OrderBy(m => m.MedicationListID)
                .ToListAsync();

            var record = await _context.Records
                .Where(r => r.PatientID == patientId)
                .OrderByDescending(r => r.AdmissionDateTime)
                .FirstOrDefaultAsync();

            return new PatientEditViewModel
            {
                PatientId = patient.PatientID,
                PatientName = patient.User != null
                    ? $"{patient.User.FirstName} {patient.User.LastName}"
                    : $"Patient #{patient.PatientID}",
                Status = patient.Status,
                AdmissionDateTime = record?.AdmissionDateTime,
                DischargeDateTime = record?.DischargeDateTime,
                MedicationLists = medications.Select(m => new MedicationListEditItem
                {
                    MedicationListID = m.MedicationListID,
                    MedicationID = m.MedicationID,
                    Dosage = m.Dosage
                }).ToList()
            };
        }

        private async Task PopulateMedicationOptionsAsync()
        {
            var medications = await _context.Medications
                .OrderBy(m => m.MedicationName)
                .ToListAsync();

            MedicationOptions = new SelectList(medications, "MedicationID", "MedicationName");
        }
    }
}
