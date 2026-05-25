using INFP_Proj.Data;
using INFP_Proj.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;

        public EditModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public PatientEditViewModel Patient { get; set; } = new();

        public SelectList MedicationOptions { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var loaded = await LoadPatientAsync(id.Value);
            if (loaded == null)
            {
                return NotFound();
            }

            Patient = loaded;
            await PopulateMedicationOptionsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateMedicationsAsync(int id)
        {
            var exists = await _context.Patients.AnyAsync(p => p.PatientID == id);
            if (!exists)
            {
                return NotFound();
            }

            Patient.PatientId = id;

            var medicationListIds = Patient.MedicationLists
                .Select(m => m.MedicationListID)
                .ToList();

            var existing = await _context.MedicationLists
                .Where(m => m.PatientID == id && medicationListIds.Contains(m.MedicationListID))
                .ToListAsync();

            foreach (var item in Patient.MedicationLists)
            {
                var record = existing.FirstOrDefault(m => m.MedicationListID == item.MedicationListID);
                if (record == null)
                {
                    continue;
                }

                record.MedicationID = item.MedicationID;
                record.Dosage = item.Dosage;
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Medications updated successfully.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostAddMedicationAsync(int id)
        {
            var exists = await _context.Patients.AnyAsync(p => p.PatientID == id);
            if (!exists)
            {
                return NotFound();
            }

            if (!Patient.NewMedicationID.HasValue || string.IsNullOrWhiteSpace(Patient.NewDosage))
            {
                TempData["Error"] = "Select a medication and enter a dosage to add.";
                return RedirectToPage(new { id });
            }

            var nextId = await _context.MedicationLists.AnyAsync()
                ? await _context.MedicationLists.MaxAsync(m => m.MedicationListID) + 1
                : 1;

            _context.MedicationLists.Add(new MedicationList
            {
                MedicationListID = nextId,
                PatientID = id,
                MedicationID = Patient.NewMedicationID.Value,
                Dosage = Patient.NewDosage.Trim()
            });

            await _context.SaveChangesAsync();
            TempData["Message"] = "Medication added.";
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDischargeAsync(int id)
        {
            var patient = await _context.Patients.FindAsync(id);
            if (patient == null)
            {
                return NotFound();
            }

            var record = await _context.Records
                .Where(r => r.PatientID == id && r.DischargeDateTime == null)
                .OrderByDescending(r => r.AdmissionDateTime)
                .FirstOrDefaultAsync();

            if (record == null)
            {
                TempData["Error"] = "No active admission record found for this patient.";
                return RedirectToPage(new { id });
            }

            record.DischargeDateTime = DateTime.UtcNow;
            patient.Status = "Discharged";

            await _context.SaveChangesAsync();
            TempData["Message"] = "Patient discharged successfully.";
            return RedirectToPage(new { id });
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
