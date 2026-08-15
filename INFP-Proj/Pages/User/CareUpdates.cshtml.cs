using INFP_Proj.Data;
using INFP_Proj.Models;
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
        private const string SelectedPatientSessionKey = "SelectedPatientId";
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public CareUpdatesModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public bool HasPatientRecord { get; set; }
        public int PatientId { get; set; }
        public string PatientName { get; set; } = string.Empty;

        public List<AppointmentDisplayItem> ConfirmedAppointments { get; set; } = new();
        public List<AppointmentDisplayItem> PendingAppointments { get; set; } = new();
        public List<AppointmentDisplayItem> AppointmentHistory { get; set; } = new();
        public List<ChangeRequestDisplayItem> ChangeRequestHistory { get; set; } = new();
        public List<DoctorRequest> DoctorRequests { get; set; } = new();

        [BindProperty]
        public NewAppointmentInput NewAppointmentRequest { get; set; } = new();

        [BindProperty]
        public string? QuestionMessage { get; set; }

        // Kept for compatibility with the current page until the new .cshtml is added.
        public string MinimumDateTimeValue =>
            GetSingaporeNow().AddMinutes(5).ToString("yyyy-MM-ddTHH:mm");

        public string MinimumDateValue =>
            GetSingaporeNow().ToString("yyyy-MM-dd");

        public async Task OnGetAsync()
        {
            await LoadPageDataAsync();
        }

        // =========================================================
        // PATIENT ACKNOWLEDGES RECEPTION APPOINTMENT
        // D1P0 -> D1P1 -> Scheduled
        // =========================================================

        public async Task<IActionResult> OnPostAcknowledgeAppointmentAsync(int appointmentId)
        {
            var patient = await GetCurrentLinkedPatientAsync();

            if (patient == null)
                return Error("No patient has been selected.");

            if (appointmentId <= 0)
                return Error("The appointment was not selected correctly.");

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a =>
                    a.AppointmentRequestID == appointmentId &&
                    a.PatientID == patient.PatientID);

            if (appointment == null)
                return Error("The selected appointment could not be found.");

            if (IsClosedStatus(appointment.Status))
                return Error("A cancelled, rejected or completed appointment cannot be acknowledged.");

            if (!IsReceptionAcknowledged(appointment))
                return Error("This appointment is still awaiting Reception approval.");

            if (appointment.PatientAcknowledged)
                return Message("This appointment has already been acknowledged.");

            // Older rows may have Reception status but DocAcknowledged was not written.
            if (!appointment.DocAcknowledged)
                appointment.DocAcknowledged = true;

            appointment.PatientAcknowledged = true;

            if (appointment.DocAcknowledged && appointment.PatientAcknowledged)
                appointment.Status = "Scheduled";

            await _context.SaveChangesAsync();

            return Message("Appointment acknowledged successfully. The appointment is now scheduled.");
        }

        // =========================================================
        // PATIENT REQUESTS NEW DATE
        // D0P1 - original appointment date stays unchanged
        // =========================================================

        public async Task<IActionResult> OnPostRequestDateChangeAsync(
            int appointmentId,
            DateTime? requestedDateTime,
            string? changeReason)
        {
            var patient = await GetCurrentLinkedPatientAsync();

            if (patient == null)
                return Error("No patient has been selected.");

            if (appointmentId <= 0)
                return Error("The appointment was not selected correctly.");

            if (!requestedDateTime.HasValue)
                return Error("Select your preferred new date and time.");

            DateTime requestedTime =
                DateTime.SpecifyKind(requestedDateTime.Value, DateTimeKind.Unspecified);

            if (requestedTime <= GetSingaporeNow())
                return Error("The requested appointment date must be in the future.");

            // ADDED: User appointments must match Reception's 9AM-5PM hourly slots.
            if (!IsValidAppointmentSlot(requestedTime))
                return Error("Select a time between 9:00 AM and 5:00 PM on the hour.");

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a =>
                    a.AppointmentRequestID == appointmentId &&
                    a.PatientID == patient.PatientID);

            if (appointment == null)
                return Error("The selected appointment could not be found.");

            if (IsClosedStatus(appointment.Status))
                return Error("A cancelled, rejected or completed appointment cannot be changed.");

            if (!IsReceptionAcknowledged(appointment))
                return Error("This appointment is still awaiting Reception approval.");

            if (appointment.DateTime == requestedTime)
                return Error("The requested date and time is the same as the current appointment.");

            string reason = changeReason?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(reason))
                return Error("Enter a reason for requesting another date.");

            if (reason.Length > 500)
                return Error("The reason cannot exceed 500 characters.");

            bool pendingExists = await _context.AppointmentChangeRequests
                .AsNoTracking()
                .AnyAsync(r =>
                    r.AppointmentRequestID == appointmentId &&
                    r.Status == "Pending");

            if (pendingExists)
                return Error("A date-change request is already pending for this appointment.");

            var changeRequest = new AppointmentChangeRequest
            {
                AppointmentRequestID = appointment.AppointmentRequestID,
                PatientID = patient.PatientID,
                RequestedDateTime = requestedTime,
                Reason = reason,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            };

            // Patient requested/rescheduled = D0P1.
            appointment.DocAcknowledged = false;
            appointment.PatientAcknowledged = true;
            appointment.Status = "Pending";

            _context.AppointmentChangeRequests.Add(changeRequest);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Error("A pending date-change request already exists for this appointment.");
            }

            return Message(
                "Your preferred new date has been submitted. It is now awaiting Reception acknowledgement.");
        }

        // =========================================================
        // PATIENT CREATES APPOINTMENT
        // D0P1
        // =========================================================

        public async Task<IActionResult> OnPostRequestNewAppointmentAsync()
        {
            var patient = await GetCurrentLinkedPatientAsync();

            if (patient == null)
                return Error("No patient has been selected.");

            if (!NewAppointmentRequest.PreferredDateTime.HasValue)
                return Error("Select a preferred appointment date and time.");

            DateTime preferredDateTime = DateTime.SpecifyKind(
                NewAppointmentRequest.PreferredDateTime.Value,
                DateTimeKind.Unspecified);

            if (preferredDateTime <= GetSingaporeNow())
                return Error("The preferred appointment date must be in the future.");

            // ADDED: Match Reception's 9AM-5PM whole-hour slots.
            if (!IsValidAppointmentSlot(preferredDateTime))
                return Error("Select a time between 9:00 AM and 5:00 PM on the hour.");

            string reason = NewAppointmentRequest.Reason?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(reason))
                return Error("Enter a reason for the appointment.");

            if (reason.Length > 500)
                return Error("The appointment reason cannot exceed 500 characters.");

            string urgency = NewAppointmentRequest.Urgency?.Trim().ToLowerInvariant() switch
            {
                "urgent" => "Urgent",
                "emergency" => "Emergency",
                _ => "Normal"
            };

            bool duplicateExists = await _context.Appointments
                .AsNoTracking()
                .AnyAsync(a =>
                    a.PatientID == patient.PatientID &&
                    a.DateTime == preferredDateTime &&
                    a.Status != "Rejected" &&
                    a.Status != "Cancelled");

            if (duplicateExists)
                return Error("An appointment or request already exists at that exact date and time.");

            var appointment = new Appointment
            {
                PatientID = patient.PatientID,
                Reason = reason,
                Urgency = urgency,
                Status = "Pending",
                DoctorResponse = null,
                DocAcknowledged = false,
                PatientAcknowledged = true,
                DateTime = preferredDateTime,
                RequestedAt = DateTime.UtcNow
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            // ADDED: Emergency appointment also creates an emergency log
            if (urgency == "Emergency")
            {
                _context.Logs.Add(new Log
                {
                    UserID = patient.UserID,
                    PatientID = patient.PatientID,
                    Event = $"Emergency appointment requested: {reason}",
                    Emergency = true,
                    Resolved = false,
                    selfAcknowledged = false,
                    relativeAcknowledged = false,
                    Timestamp = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }

            return Message(
                "Your appointment request was submitted. It is now awaiting Reception acknowledgement.");
        }

        // =========================================================
        // ASK DOCTOR
        // =========================================================

        public async Task<IActionResult> OnPostAskDoctorAsync()
        {
            var patient = await GetCurrentLinkedPatientAsync();

            if (patient == null)
                return Error("No patient has been selected.");

            string question = QuestionMessage?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(question))
                return Error("Enter a question for the doctor.");

            if (question.Length > 1000)
                return Error("The question cannot exceed 1,000 characters.");

            _context.DoctorRequests.Add(new DoctorRequest
            {
                PatientID = patient.PatientID,
                RequestMessage = question,
                RequestDate = DateTime.UtcNow,
                ReplyMessage = null,
                Completed = false,
                ByAdmin = false
            });

            await _context.SaveChangesAsync();

            return Message("Your question was submitted to the doctor.");
        }

        // =========================================================
        // LOAD PAGE
        // =========================================================

        private async Task LoadPageDataAsync()
        {
            var patient = await GetCurrentLinkedPatientAsync();

            if (patient == null)
            {
                HasPatientRecord = false;
                return;
            }

            HasPatientRecord = true;
            PatientId = patient.PatientID;
            PatientName = patient.User == null
                ? $"Patient #{patient.PatientID}"
                : $"{patient.User.FirstName} {patient.User.LastName}".Trim();

            var appointments = await _context.Appointments
                .AsNoTracking()
                .Where(a => a.PatientID == patient.PatientID)
                .OrderBy(a => a.DateTime)
                .ToListAsync();

            var changeRequests = await _context.AppointmentChangeRequests
                .AsNoTracking()
                .Where(r => r.PatientID == patient.PatientID)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            var latestChanges = changeRequests
                .GroupBy(r => r.AppointmentRequestID)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.RequestedAt).First());

            var pendingChanges = changeRequests
                .Where(r => r.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                .GroupBy(r => r.AppointmentRequestID)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.RequestedAt).First());

            var displayItems = appointments.Select(a =>
            {
                pendingChanges.TryGetValue(a.AppointmentRequestID, out var pending);
                latestChanges.TryGetValue(a.AppointmentRequestID, out var latest);

                return new AppointmentDisplayItem
                {
                    AppointmentRequestID = a.AppointmentRequestID,
                    DateTime = a.DateTime,
                    Reason = a.Reason,
                    Urgency = a.Urgency,
                    Status = a.Status,
                    DoctorResponse = a.DoctorResponse,
                    DocAcknowledged = a.DocAcknowledged,
                    PatientAcknowledged = a.PatientAcknowledged,
                    RequestedAt = a.RequestedAt,
                    IsConfirmed = IsReceptionAcknowledged(a),
                    IsClosed = IsClosedStatus(a.Status),
                    PendingChangeRequest = pending,
                    LatestChangeRequest = latest
                };
            }).ToList();

            DateTime now = GetSingaporeNow();

            ConfirmedAppointments = displayItems
                .Where(a => a.DateTime >= now && a.IsConfirmed && !a.IsClosed)
                .OrderBy(a => a.DateTime)
                .ToList();

            PendingAppointments = displayItems
                .Where(a =>
                    !a.IsConfirmed &&
                    !a.IsClosed &&
                    (a.DateTime >= now || a.PendingChangeRequest != null))
                .OrderBy(a => a.PendingChangeRequest?.RequestedDateTime ?? a.DateTime)
                .ToList();

            AppointmentHistory = displayItems
                .Where(a =>
                    (a.DateTime < now && a.PendingChangeRequest == null) ||
                    a.IsClosed)
                .OrderByDescending(a => a.DateTime)
                .ToList();

            var appointmentLookup =
                appointments.ToDictionary(a => a.AppointmentRequestID);

            ChangeRequestHistory = changeRequests.Select(r =>
            {
                appointmentLookup.TryGetValue(r.AppointmentRequestID, out var appointment);

                return new ChangeRequestDisplayItem
                {
                    AppointmentChangeRequestID = r.AppointmentChangeRequestID,
                    AppointmentRequestID = r.AppointmentRequestID,
                    CurrentAppointmentDate = appointment?.DateTime,
                    RequestedDateTime = r.RequestedDateTime,
                    Reason = r.Reason,
                    Status = r.Status,
                    RequestedAt = r.RequestedAt,
                    ReviewedAt = r.ReviewedAt,
                    ReviewMessage = r.ReviewMessage
                };
            }).ToList();

            DoctorRequests = await _context.DoctorRequests
                .AsNoTracking()
                .Where(r => r.PatientID == patient.PatientID && !r.ByAdmin)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        // =========================================================
        // SELECTED PATIENT
        // =========================================================

        private async Task<Patients?> GetCurrentLinkedPatientAsync()
        {
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
                return null;

            int? selectedPatientId =
                HttpContext.Session.GetInt32(SelectedPatientSessionKey);

            if (selectedPatientId.HasValue)
            {
                bool ownsPatient = await _context.Patients
                    .AsNoTracking()
                    .AnyAsync(p =>
                        p.PatientID == selectedPatientId.Value &&
                        p.UserID == currentUserId);

                bool isRelated = await _context.Relationships
                    .AsNoTracking()
                    .AnyAsync(r =>
                        r.PatientID == selectedPatientId.Value &&
                        r.UserID == currentUserId);

                if (ownsPatient || isRelated)
                {
                    return await _context.Patients
                        .Include(p => p.User)
                        .FirstOrDefaultAsync(p =>
                            p.PatientID == selectedPatientId.Value);
                }

                HttpContext.Session.Remove(SelectedPatientSessionKey);
            }

            var ownPatient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserID == currentUserId);

            if (ownPatient != null)
            {
                HttpContext.Session.SetInt32(
                    SelectedPatientSessionKey,
                    ownPatient.PatientID);

                return ownPatient;
            }

            return null;
        }

        // =========================================================
        // HELPERS
        // =========================================================

        public string FormatAppointmentDate(DateTime dateTime) =>
            dateTime.ToString("dd MMM yyyy, hh:mm tt");

        public string FormatUtcDate(DateTime dateTime) =>
            ToSingaporeTime(dateTime).ToString("dd MMM yyyy, hh:mm tt");

        public string FormatOptionalUtcDate(DateTime? dateTime) =>
            dateTime.HasValue ? FormatUtcDate(dateTime.Value) : "-";

        private IActionResult Message(string message)
        {
            SetMessage(message);
            return RedirectToPage();
        }

        private IActionResult Error(string message)
        {
            SetError(message);
            return RedirectToPage();
        }

        private void SetMessage(string message) =>
            TempData["CareUpdateMessage"] = message;

        private void SetError(string message) =>
            TempData["CareUpdateError"] = message;

        // ADDED: Same appointment times as Reception.
        private static bool IsValidAppointmentSlot(DateTime dateTime) =>
            dateTime.Hour >= 9 &&
            dateTime.Hour <= 17 &&
            dateTime.Minute == 0 &&
            dateTime.Second == 0;

        private static bool IsReceptionAcknowledged(Appointment appointment)
        {
            if (appointment.DocAcknowledged)
                return true;

            if (string.IsNullOrWhiteSpace(appointment.Status))
                return false;

            string[] receptionStatuses =
            {
                "Approved",
                "Scheduled",
                "Rescheduled",
                "Confirmed",
                "Awaiting Patient"
            };

            return receptionStatuses.Contains(
                appointment.Status,
                StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsClosedStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return false;

            string[] closedStatuses =
            {
                "Rejected",
                "Cancelled",
                "Completed"
            };

            return closedStatuses.Contains(
                status,
                StringComparer.OrdinalIgnoreCase);
        }

        private static DateTime GetSingaporeNow() =>
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                GetSingaporeTimeZone());

        private static DateTime ToSingaporeTime(DateTime dateTime)
        {
            DateTime utc = dateTime.Kind switch
            {
                DateTimeKind.Utc => dateTime,
                DateTimeKind.Local => dateTime.ToUniversalTime(),
                _ => DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)
            };

            return TimeZoneInfo.ConvertTimeFromUtc(
                utc,
                GetSingaporeTimeZone());
        }

        private static TimeZoneInfo GetSingaporeTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "Singapore Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById(
                    "Asia/Singapore");
            }
        }

        // =========================================================
        // VIEW MODELS
        // =========================================================

        public sealed class NewAppointmentInput
        {
            public DateTime? PreferredDateTime { get; set; }
            public string? Reason { get; set; }
            public string Urgency { get; set; } = "Normal";
        }

        public sealed class AppointmentDisplayItem
        {
            public int AppointmentRequestID { get; set; }
            public DateTime DateTime { get; set; }
            public string Reason { get; set; } = string.Empty;
            public string Urgency { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public string? DoctorResponse { get; set; }
            public bool DocAcknowledged { get; set; }
            public bool PatientAcknowledged { get; set; }
            public DateTime RequestedAt { get; set; }
            public bool IsConfirmed { get; set; }
            public bool IsClosed { get; set; }

            public AppointmentChangeRequest? PendingChangeRequest { get; set; }
            public AppointmentChangeRequest? LatestChangeRequest { get; set; }

            public bool CanAcknowledge =>
                IsConfirmed &&
                !IsClosed &&
                !PatientAcknowledged;

            public bool CanRequestDateChange =>
                IsConfirmed &&
                !IsClosed &&
                PendingChangeRequest == null;
        }

        public sealed class ChangeRequestDisplayItem
        {
            public int AppointmentChangeRequestID { get; set; }
            public int AppointmentRequestID { get; set; }
            public DateTime? CurrentAppointmentDate { get; set; }
            public DateTime RequestedDateTime { get; set; }
            public string? Reason { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime RequestedAt { get; set; }
            public DateTime? ReviewedAt { get; set; }
            public string? ReviewMessage { get; set; }
        }
    }
}