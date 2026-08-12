using INFP_Proj.Data;
using iText.Forms;
using iText.Kernel.Pdf;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;

namespace INFP_Proj.Pages.Admin
{
    public class DeclareDeath : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DeclareDeath(AppDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
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

            return Page();
        }

        public async Task<IActionResult> OnPostGenerateAsync(
            int patientId,
            int recordId,
            string Sex,
            string DateOfBirth,
            string DateOfDeath,
            string TimeOfDeath,
            string Location,
            string ConditionReason,
            string RecordedBy,
            string DateOfRecord)
        {
            if (!User.IsInRole("Doctor"))
            {
                TempData["Error"] = "Only a doctor can complete a cause-of-death record.";
                return RedirectToPage("/Admin/Edit", new { id = patientId });
            }

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientID == patientId);
            var record = await _context.Records
                .FirstOrDefaultAsync(r => r.RecordID == recordId && r.PatientID == patientId);

            if (patient == null || record == null)
            {
                return NotFound();
            }

            var templatePath = Path.Combine(_env.WebRootPath, "js", "templates", "DeathCertFAKE.pdf");

            using var ms = new MemoryStream();
            using (var reader = new PdfReader(templatePath))
            using (var pdf = new PdfDocument(reader, new PdfWriter(ms)))
            {
                var form = PdfAcroForm.GetAcroForm(pdf, true);

                var patientName = patient.User != null
                    ? $"{patient.User.FirstName} {patient.User.LastName}"
                    : $"Patient #{patient.PatientID}";

                SetIfPresent(form, "full_name", patientName);
                SetIfPresent(form, "patient_id", patient.PatientID.ToString());
                SetIfPresent(form, "sex", Sex);
                SetIfPresent(form, "date_of_birth", DateOfBirth);
                SetIfPresent(form, "admission_date", record.AdmissionDateTime.ToString("dd-MM-yyyy"));
                SetIfPresent(form, "date_of_death", DateOfDeath);
                SetIfPresent(form, "time_of_death", TimeOfDeath);
                SetIfPresent(form, "location", Location);
                SetIfPresent(form, "condition_reason", ConditionReason);
                SetIfPresent(form, "recorded_by", RecordedBy);
                SetIfPresent(form, "date_of_record", DateOfRecord);

                form.FlattenFields();
            }

            var fileName = $"discharge-record-{patient.PatientID}-{DateTime.UtcNow:yyyyMMdd}.pdf";
            return File(ms.ToArray(), "application/pdf", fileName);
        }

        private static void SetIfPresent(PdfAcroForm form, string fieldName, string value)
        {
            var field = form.GetField(fieldName);
            field?.SetValue(value ?? string.Empty);
        }
    }
}