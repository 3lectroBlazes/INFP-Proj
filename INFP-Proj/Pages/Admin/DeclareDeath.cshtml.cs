using INFP_Proj.Data;
using INFP_Proj.Services;
using iText.Forms;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Globalization;

namespace INFP_Proj.Pages.Admin
{
    public class DeclareDeath : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly AdminLogService _adminLogService;

        public DeclareDeath(AppDbContext context, IWebHostEnvironment env, AdminLogService adminLogService)
        {
            _context = context;
            _env = env;
            _adminLogService = adminLogService;
        }

        public Patients? Patient { get; set; }
        public Records? Record { get; set; }

        public async Task<IActionResult> OnGetAsync(int patientId, int? recordId)
        {
            if (!User.IsInRole("Doctor"))
            {
                TempData["Error"] = "Only a doctor can complete a cause-of-death record.";
                return RedirectToPage("/Admin/Edit", new { id = patientId });
            }

            Patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientID == patientId);

            if (Patient == null)
            {
                return NotFound();
            }

            Record = recordId.HasValue
                ? await _context.Records.FirstOrDefaultAsync(r => r.RecordID == recordId.Value)
                : await _context.Records
                    .Where(r => r.PatientID == patientId)
                    .OrderByDescending(r => r.AdmissionDateTime)
                    .FirstOrDefaultAsync();

            if (Record != null && Record.DischargeDateTime != null)
            {
                TempData["Message"] = "This patient's discharge has already been recorded.";
                return RedirectToPage("/Admin/Edit", new { id = patientId });
            }

            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAsync(
            int patientId,
            int recordId,
            string Sex,
            string DateOfBirth,
            string DateOfDeath,
            string TimeOfDeath,
            string ConditionReason)
        {
            if (!User.IsInRole("Doctor"))
            {
                TempData["Error"] = "Only a doctor can complete a cause-of-death record.";
                return RedirectToPage("/Admin/Edit", new { id = patientId });
            }

            Patients patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientID == patientId);

            Records record = await _context.Records
                .FirstOrDefaultAsync(r => r.RecordID == recordId
                    && r.PatientID == patientId
                    && r.DischargeDateTime == null);

            if (patient == null || record == null)
            {
                TempData["Error"] = "No active admission record found for this patient. It may have already been discharged.";
                return RedirectToPage("/Admin/Edit", new { id = patientId });
            }

            Hospitals hospital = await _context.Hospitals.FirstOrDefaultAsync(h => h.HospitalID == record.HospitalID);

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var doctorUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (string.IsNullOrWhiteSpace(DateOfBirth)
                || string.IsNullOrWhiteSpace(DateOfDeath)
                || string.IsNullOrWhiteSpace(TimeOfDeath)
                || hospital == null
                || string.IsNullOrWhiteSpace(ConditionReason)
                || doctorUser == null)
            {
                TempData["Error"] = "Please fill in all required fields before generating the record.";
                Patient = patient;
                Record = record;
                return Page();
            }
            var dateOfDeathInput = $"{DateOfDeath} {TimeOfDeath}";
            if (!DateTime.TryParseExact(
            dateOfDeathInput,
            "yyyy-MM-dd HH:mm",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var dateOfDeathParsed))
            {
                TempData["Error"] = "Date/time of death is invalid.";
                Patient = patient;
                Record = record;
                return Page();
            }

            if (dateOfDeathParsed > DateTime.Now)
            {
                TempData["Error"] = "Date and time of death cannot be in the future.";
                Patient = patient;
                Record = record;
                return Page();
            }
            string recordedBy = $"Doctor {doctorUser.FirstName} {doctorUser.LastName}";
            string dateOfRecord = DateTime.UtcNow.ToString("dd-MM-yyyy");
            string location = $"{hospital.HospitalName}, {hospital.HospitalAddress}";
            
            var templatePath = Path.Combine(_env.WebRootPath, "js", "templates", "DeathCertFAKE.pdf");

            byte[] pdfBytes;
            using (MemoryStream ms = new MemoryStream())
            {
                using (PdfReader reader = new PdfReader(templatePath))
                using (PdfDocument pdf = new PdfDocument(reader, new PdfWriter(ms)))
                {
                    PdfAcroForm form = PdfAcroForm.GetAcroForm(pdf, true);

                    string patientName = patient.User != null
                        ? $"{patient.User.FirstName} {patient.User.LastName}"
                        : $"Patient #{patient.PatientID}";

                    SetIfPresent(form, "full_name", patientName);
                    SetIfPresent(form, "patient_id", patient.PatientID.ToString());
                    SetIfPresent(form, "sex", Sex);
                    SetIfPresent(form, "date_of_birth", DateOfBirth);
                    SetIfPresent(form, "admission_date", record.AdmissionDateTime.ToString("dd-MM-yyyy"));
                    SetIfPresent(form, "date_of_death", DateOfDeath);
                    SetIfPresent(form, "time_of_death", TimeOfDeath);
                    SetIfPresent(form, "location", location);
                    SetIfPresent(form, "condition_reason", ConditionReason);
                    SetIfPresent(form, "recorded_by", recordedBy);
                    SetIfPresent(form, "date_of_record", dateOfRecord);

                    form.FlattenFields();
                }

                pdfBytes = ms.ToArray();
            }

            record.DischargeDateTime = dateOfDeathParsed;
            record.DischargeReason = "Deceased";
            patient.Status = "Discharged";

            await _context.SaveChangesAsync();

            var loggedPatientName = patient.User != null
                ? $"{patient.User.FirstName} {patient.User.LastName}"
                : $"patient #{patient.PatientID}";

            try
            {
                await _adminLogService.AddLogAsync($"{loggedPatientName} discharged (Deceased) — death record recorded by {recordedBy}");

                if (!string.IsNullOrEmpty(patient.UserID))
                {
                    await _adminLogService.AddLogAsync(
                        "You have been discharged from the hospital",
                        userId: patient.UserID);
                }

                // FIX: guard patient.User, same as loggedPatientName above
                string fileName = $"Death Certificate {loggedPatientName} {DateTime.UtcNow:ddMMyyyy}.pdf";

                var deathCert = new DeathCerts
                {
                    PatientID = patient.PatientID,
                    RecordID = record.RecordID,
                    PatientName = loggedPatientName,
                    FileName = fileName,
                    ContentType = "application/pdf",
                    PdfData = pdfBytes,
                    RecordedBy = recordedBy,
                    CreatedAt = DateTime.UtcNow
                };

                _context.DeathCerts.Add(deathCert);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Discharge was recorded, but the death certificate file could not be saved: " + ex.Message;
                Patient = patient;
                Record = record;
                return Page();
            }

            return RedirectToPage("/Admin/Index");
        }

        private static void SetIfPresent(PdfAcroForm form, string fieldName, string value)
        {
            var field = form.GetField(fieldName);
            field?.SetValue(value ?? string.Empty);
        }
    }
}