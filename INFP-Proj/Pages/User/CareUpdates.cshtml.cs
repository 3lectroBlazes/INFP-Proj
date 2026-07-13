using System.ComponentModel.DataAnnotations;
using INFP_Proj.Data;
using INFP_Proj.Models;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.User
{
    [Authorize]
    public class CareUpdatesModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public CareUpdatesModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public CareUpdatesViewModel CareData { get; set; } = new();

        public List<Appointment> Appointments { get; set; } = new();

        [BindProperty]
        public string QuestionMessage { get; set; } = string.Empty;

        [BindProperty]
        public string AppointmentChangeReason { get; set; } = string.Empty;

        [BindProperty]
        public AppointmentRequestInput NewAppointmentRequest { get; set; } = new();

        public class AppointmentRequestInput
        {
            [Required(ErrorMessage = "Please choose a preferred date and time.")]
            public DateTime PreferredDateTime { get; set; }

            [Required(ErrorMessage = "Please enter a reason for the appointment.")]
            [StringLength(500, ErrorMessage = "Reason cannot be more than 500 characters.")]
            public string Reason { get; set; } = string.Empty;

            [Required]
            public string Urgency { get; set; } = "Normal";
        }

        public async Task OnGetAsync()
        {
            await LoadCareUpdatesAsync();
        }

        public async Task<IActionResult> OnPostAskDoctorAsync()
        {
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Login");
            }

            Patients? patient = await GetLinkedPatientAsync(currentUserId);

            if (patient == null)
            {
                TempData["CareUpdateMessage"] = "No patient record is linked to your account.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(QuestionMessage))
            {
                TempData["CareUpdateMessage"] = "Please enter a question before submitting.";
                return RedirectToPage();
            }

            var doctorRequest = new DoctorRequest
            {
                PatientID = patient.PatientID,
                RequestMessage = QuestionMessage.Trim(),
                RequestDate = DateTime.UtcNow,
                ReplyMessage = null,
                Completed = false
            };

            _context.DoctorRequests.Add(doctorRequest);
            await _context.SaveChangesAsync();

            TempData["CareUpdateMessage"] = "Your question has been sent to the doctor.";
            return RedirectToPage();
        }

        public IActionResult OnPostAcknowledgeAppointment()
        {
            TempData["AppointmentAcknowledged"] = "true";
            TempData["CareUpdateMessage"] = "Appointment acknowledged. The care team will be informed.";
            return RedirectToPage();
        }

        public IActionResult OnPostRequestAppointmentChange()
        {
            if (string.IsNullOrWhiteSpace(AppointmentChangeReason))
            {
                TempData["CareUpdateMessage"] = "Please enter a reason or preferred timing before submitting.";
                return RedirectToPage();
            }

            TempData["AppointmentChangeRequest"] = AppointmentChangeReason.Trim();
            TempData["CareUpdateMessage"] = "Your appointment change request has been submitted.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostRequestNewAppointmentAsync()
        {
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Login");
            }

            Patients? patient = await GetLinkedPatientAsync(currentUserId);

            if (patient == null)
            {
                TempData["CareUpdateMessage"] = "No patient record is linked to your account.";
                return RedirectToPage();
            }

            if (NewAppointmentRequest.PreferredDateTime == default)
            {
                TempData["CareUpdateMessage"] = "Please choose a preferred date and time.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(NewAppointmentRequest.Reason))
            {
                TempData["CareUpdateMessage"] = "Please enter a reason for the appointment.";
                return RedirectToPage();
            }

            if (string.IsNullOrWhiteSpace(NewAppointmentRequest.Urgency))
            {
                NewAppointmentRequest.Urgency = "Normal";
            }

            if (NewAppointmentRequest.PreferredDateTime < DateTime.Now)
            {
                TempData["CareUpdateMessage"] = "Please select a future date and time for the appointment.";
                return RedirectToPage();
            }

            var appointmentRequest = new Appointment
            {
                PatientID = patient.PatientID,
                DateTime = NewAppointmentRequest.PreferredDateTime,
                Reason = NewAppointmentRequest.Reason.Trim(),
                Urgency = NewAppointmentRequest.Urgency,
                Status = "Pending",
                DoctorResponse = null,
                RequestedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointmentRequest);
            await _context.SaveChangesAsync();

            TempData["CareUpdateMessage"] = "Your appointment request has been submitted. Please wait for the care team to confirm.";
            return RedirectToPage();
        }

        private async Task LoadCareUpdatesAsync()
        {
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return;
            }

            Patients? patient = await GetLinkedPatientAsync(currentUserId);

            if (patient == null)
            {
                CareData.HasPatientRecord = false;
                return;
            }

            var communications = await _context.DoctorRequests
                .Where(dr => dr.PatientID == patient.PatientID)
                .OrderByDescending(dr => dr.RequestDate)
                .Select(dr => new DoctorCommunicationItem
                {
                    DoctorRequestId = dr.DoctorRequestID,
                    Message = dr.RequestMessage,
                    ReplyMessage = string.IsNullOrWhiteSpace(dr.ReplyMessage)
                        ? "No reply yet."
                        : dr.ReplyMessage,
                    Completed = dr.Completed,
                    RequestDate = dr.RequestDate
                })
                .ToListAsync();

            Appointments = await _context.Appointments
                .Where(ar => ar.PatientID == patient.PatientID)
                .OrderByDescending(ar => ar.RequestedAt)
                .ToListAsync();

            CareData = new CareUpdatesViewModel
            {
                HasPatientRecord = true,
                PatientId = patient.PatientID,
                PatientName = patient.User != null
                    ? $"{patient.User.FirstName} {patient.User.LastName}"
                    : $"Patient #{patient.PatientID}",

                UpcomingAppointment = GetDummyAppointment(),

                AppointmentAcknowledged = TempData["AppointmentAcknowledged"]?.ToString() == "true",
                AppointmentChangeRequest = TempData["AppointmentChangeRequest"]?.ToString(),

                DoctorCommunications = communications
            };

            TempData.Keep("AppointmentAcknowledged");
            TempData.Keep("AppointmentChangeRequest");
        }

        private AppointmentPreview GetDummyAppointment()
        {
            return new AppointmentPreview
            {
                Title = "Follow-up Review",
                DoctorName = "Dr Xavier Wee",
                AppointmentDateTime = DateTime.Today.AddDays(5).AddHours(10).AddMinutes(30),
                Location = "General Ward Consultation Room",
                Status = "Scheduled",
                Purpose = "Review patient condition, medication, and latest vitals."
            };
        }

        private async Task<Patients?> GetLinkedPatientAsync(string currentUserId)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserID == currentUserId);

            if (patient != null)
            {
                return patient;
            }

            int? linkedPatientId = await _context.Relationships
                .Where(r => r.UserID == currentUserId)
                .Select(r => (int?)r.PatientID)
                .FirstOrDefaultAsync();

            if (!linkedPatientId.HasValue)
            {
                return null;
            }

            return await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientID == linkedPatientId.Value);
        }
    }
}