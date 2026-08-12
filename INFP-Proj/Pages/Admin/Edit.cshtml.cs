using INFP_Proj.Data;
using INFP_Proj.Services;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin
{
    public class EditModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly AdminLogService _adminLogService;

        public EditModel(AppDbContext context, AdminLogService adminLogService)
        {
            _context = context;
            _adminLogService = adminLogService;
        }

        [BindProperty]
        public PatientEditViewModel Patient { get; set; } = new();

        public SelectList MedicationOptions { get; set; } = default!;
        public DoctorRequest DoctorRequest { get; set; } = new();
        public List<DoctorRequest> DoctorRequests { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int? id)
        {
            if (id == null) return NotFound();

            var loaded = await LoadPatientAsync(id.Value);
            if (loaded == null) return NotFound();

            Patient = loaded;
            await PopulateMedicationOptionsAsync();

            DoctorRequest = await _context.DoctorRequests
                .FirstOrDefaultAsync(dr => dr.PatientID == id.Value)
                ?? new DoctorRequest { PatientID = id.Value };

            DoctorRequests = await _context.DoctorRequests
                .Where(dr => dr.PatientID == id.Value)
                .OrderByDescending(dr => dr.DoctorRequestID)
                .ToListAsync();

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
            await _adminLogService.AddLogAsync($"Medications updated for patient #{id}");
            await AddPatientLogIfLinkedAsync(id, "Your medication schedule was updated");
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

            Medications medication = await _context.Medications.FindAsync(Patient.NewMedicationID.Value);
            if (medication == null || string.IsNullOrWhiteSpace(Patient.NewDosage))
            {
                TempData["Error"] = "Select a medication and enter a dosage to add.";
                return RedirectToPage(new { id });
            }

            var alreadyExists = await _context.MedicationLists
                .AnyAsync(m => m.PatientID == id && m.MedicationID == Patient.NewMedicationID.Value);
            if (alreadyExists)
            {
                TempData["Error"] = "This medication is already assigned to this patient.";
                return RedirectToPage(new { id });
            }

            var newMedicationList = new MedicationList
            {
                PatientID = id,
                MedicationID = Patient.NewMedicationID.Value,
                Dosage = Patient.NewDosage.Trim(),
                Approved = !medication.Approval
            };
            _context.MedicationLists.Add(newMedicationList);
            await _context.SaveChangesAsync();

            if (!medication.Approval)
            {
                await _adminLogService.AddLogAsync(
                    $"Medication requested for patient #{id}",
                    true,
                    null,
                    id,
                    newMedicationList.MedicationListID);
                TempData["Message"] = "Medication Requested.";
            }
            else
            {
                await _adminLogService.AddLogAsync($"Medication added for patient #{id}");
                await AddPatientLogIfLinkedAsync(id, "A new medication was added to your schedule");
                TempData["Message"] = "Medication added.";
            }

            return RedirectToPage(new { id });
        }
        public async Task<IActionResult> OnPostRemoveMedicationAsync(int id, int medicationListId)
        {
            var entry = await _context.MedicationLists.FindAsync(medicationListId);
            if (entry != null)
            {
                _context.MedicationLists.Remove(entry);
                await _context.SaveChangesAsync();
                await _adminLogService.AddLogAsync($"Medication removed for patient #{id}");
                await AddPatientLogIfLinkedAsync(id, "A medication was removed from your schedule");
                TempData["Message"] = "Medication removed.";
            }
            return RedirectToPage(new { id });
        }

        public async Task<IActionResult> OnPostDischargeAsync(int id, string dischargeReason)
        {
            return await DischargeWithReasonAsync(id, dischargeReason);
        }

        public async Task<IActionResult> OnPostDischargeDeceasedAsync(int id)
        {
            if (!User.IsInRole("Doctor"))
            {
                TempData["Error"] = "Only a doctor can record a discharge as Deceased.";
                return RedirectToPage(new { id });
            }

            return await DischargeWithReasonAsync(id, "Deceased");
        }

        private async Task<IActionResult> DischargeWithReasonAsync(int id, string reason)
        {
            if (reason == "Deceased" && !User.IsInRole("Doctor"))
            {
                TempData["Error"] = "Only a doctor can record a discharge as Deceased.";
                return RedirectToPage(new { id });
            }

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientID == id);
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

            if (reason == "Deceased")
            {
                return RedirectToPage("/Admin/DeclareDeath", new { patientId = id, recordId = record.RecordID });
            }

            record.DischargeDateTime = DateTime.UtcNow;
            record.DischargeReason = reason;
            patient.Status = "Discharged";

            await _context.SaveChangesAsync();

            var patientName = patient.User != null
                ? $"{patient.User.FirstName} {patient.User.LastName}"
                : $"patient #{id}";
            await _adminLogService.AddLogAsync($"{patientName} discharged");

            if (!string.IsNullOrEmpty(patient.UserID))
            {
                await _adminLogService.AddLogAsync(
                    "You have been discharged from the hospital",
                    userId: patient.UserID);
            }

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

            var record = await _context.Records
                .Where(r => r.PatientID == patientId)
                .OrderByDescending(r => r.AdmissionDateTime)
                .FirstOrDefaultAsync();

            // Only show medications for the current admission (medications attached to,
            // or added after, the latest record's medication list).
            var medications = await _context.MedicationLists
                .Where(m => m.PatientID == patientId
                    && (record == null || m.MedicationListID >= record.MedicationListID))
                .OrderBy(m => m.MedicationListID)
                .ToListAsync();

            return new PatientEditViewModel
            {
                PatientId = patient.PatientID,
                PatientName = patient.User != null
                    ? $"{patient.User.FirstName} {patient.User.LastName}"
                    : $"Patient #{patient.PatientID}",
                Status = patient.Status,
                AdmissionDateTime = record?.AdmissionDateTime,
                DischargeDateTime = record?.DischargeDateTime,
                DischargeReason = record?.DischargeReason,
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

            MedicationOptions = new SelectList(medications, "MedicationID", "MedicationName", "Approval");
        }

        private async Task AddPatientLogIfLinkedAsync(int patientId, string message)
        {
            var userId = await _context.Patients
                .Where(p => p.PatientID == patientId)
                .Select(p => p.UserID)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(userId))
            {
                await _adminLogService.AddLogAsync(message, userId: userId);
            }
        }
        public async Task<IActionResult> OnPostSaveDoctorRequestAsync(int id)
        {
            var request = await _context.DoctorRequests.FirstOrDefaultAsync(dr => dr.PatientID == id);

            request = new DoctorRequest { PatientID = id, RequestMessage = Request.Form["RequestMessage"], ByAdmin = true };
            _context.DoctorRequests.Add(request);
            TempData["Message"] = "Doctor request sent!";
            await _context.SaveChangesAsync();

            return RedirectToPage(new { id });
        }
        public async Task<IActionResult> OnPostReplyDoctorRequestAsync(int id, int requestId)
        {
            var request = await _context.DoctorRequests.FindAsync(requestId);
            if (request == null) return NotFound();

            request.ReplyMessage = Request.Form["ReplyMessage"];
            request.Completed = true;

            await _context.SaveChangesAsync();
            TempData["Message"] = "Reply sent!";
            return RedirectToPage(new { id });
        }
    }
}