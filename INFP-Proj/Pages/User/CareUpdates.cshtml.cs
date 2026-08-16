using INFP_Proj.Data;
using INFP_Proj.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public string PatientName { get; set; } = "";

        public List<SelectListItem> DoctorsList { get; set; } = new();
        public List<AppointmentDisplayItem> ConfirmedAppointments { get; set; } = new();
        public List<AppointmentDisplayItem> PendingAppointments { get; set; } = new();
        public List<AppointmentDisplayItem> AppointmentHistory { get; set; } = new();
        public List<ChangeRequestDisplayItem> ChangeRequestHistory { get; set; } = new();
        public List<DoctorRequest> DoctorRequests { get; set; } = new();

        [BindProperty]
        public NewAppointmentInput NewAppointmentRequest { get; set; } = new();

        [BindProperty]
        public string? QuestionMessage { get; set; }

        public string MinimumDateValue => GetSingaporeNow().ToString("yyyy-MM-dd");

        public async Task OnGetAsync()
        {
            await LoadDoctorsAsync();
            await LoadPageDataAsync();
        }

        // =========================================================
        // ACKNOWLEDGE RECEPTION APPOINTMENT
        // D1P0 -> D1P1
        // =========================================================

        public async Task<IActionResult> OnPostAcknowledgeAppointmentAsync(int appointmentId)
        {
            var patient = await GetCurrentLinkedPatientAsync();
            if (patient == null) return Error("No patient has been selected.");

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a =>
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

            appointment.DocAcknowledged = true;
            appointment.PatientAcknowledged = true;
            appointment.Status = "Scheduled";

            await _context.SaveChangesAsync();

            return Message("Appointment acknowledged successfully. The appointment is now scheduled.");
        }

        // =========================================================
        // REQUEST ANOTHER DATE
        // Patient request = D0P1
        // =========================================================

        public async Task<IActionResult> OnPostRequestDateChangeAsync(
            int appointmentId,
            DateTime? requestedDateTime,
            string? changeReason)
        {
            var patient = await GetCurrentLinkedPatientAsync();

            if (patient == null)
                return Error("No patient has been selected.");

            if (!requestedDateTime.HasValue)
                return Error("Select your preferred new date and time.");

            DateTime requestedTime = DateTime.SpecifyKind(
                requestedDateTime.Value,
                DateTimeKind.Unspecified);

            if (requestedTime <= GetSingaporeNow())
                return Error("The requested appointment date must be in the future.");

            if (!IsValidAppointmentSlot(requestedTime))
                return Error("Select a time between 9:00 AM and 5:00 PM on the hour.");

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a =>
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

            // Keep same assigned doctor and check availability.
            if (!string.IsNullOrWhiteSpace(appointment.DoctorID))
            {
                bool booked = await _context.Appointments.AsNoTracking().AnyAsync(a =>
                    a.DoctorID == appointment.DoctorID &&
                    a.DateTime == requestedTime &&
                    a.AppointmentRequestID != appointmentId &&
                    a.Status != "Rejected" &&
                    a.Status != "Cancelled");

                if (booked)
                    return Error("The assigned doctor is already booked at the requested time.");
            }

            string reason = changeReason?.Trim() ?? "";

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

            _context.AppointmentChangeRequests.Add(new AppointmentChangeRequest
            {
                AppointmentRequestID = appointment.AppointmentRequestID,
                PatientID = patient.PatientID,
                RequestedDateTime = requestedTime,
                Reason = reason,
                Status = "Pending",
                RequestedAt = DateTime.UtcNow
            });

            appointment.DocAcknowledged = false;
            appointment.PatientAcknowledged = true;
            appointment.Status = "Pending";

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
        // CREATE APPOINTMENT REQUEST
        // D0P1
        // =========================================================

        public async Task<IActionResult> OnPostRequestNewAppointmentAsync()
        {
            var patient = await GetCurrentLinkedPatientAsync();

            if (patient == null)
                return Error("No patient has been selected.");

            if (!NewAppointmentRequest.PreferredDateTime.HasValue)
                return Error("Select a preferred appointment date and time.");

            DateTime dateTime = DateTime.SpecifyKind(
                NewAppointmentRequest.PreferredDateTime.Value,
                DateTimeKind.Unspecified);

            if (dateTime <= GetSingaporeNow())
                return Error("The preferred appointment date must be in the future.");

            if (!IsValidAppointmentSlot(dateTime))
                return Error("Select a time between 9:00 AM and 5:00 PM on the hour.");

            if (string.IsNullOrWhiteSpace(NewAppointmentRequest.DoctorID) ||
                !await IsValidDoctorAsync(NewAppointmentRequest.DoctorID))
                return Error("Select a valid doctor for the appointment.");

            string reason = NewAppointmentRequest.Reason?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(reason))
                return Error("Enter a reason for the appointment.");

            if (reason.Length > 500)
                return Error("The appointment reason cannot exceed 500 characters.");

            string urgency = NewAppointmentRequest.Urgency?.ToLowerInvariant() switch
            {
                "urgent" => "Urgent",
                "emergency" => "Emergency",
                _ => "Normal"
            };

            bool doctorBooked = await _context.Appointments.AsNoTracking().AnyAsync(a =>
                a.DoctorID == NewAppointmentRequest.DoctorID &&
                a.DateTime == dateTime &&
                a.Status != "Rejected" &&
                a.Status != "Cancelled");

            if (doctorBooked)
                return Error("This doctor is already booked at the selected time.");

            bool patientBooked = await _context.Appointments.AsNoTracking().AnyAsync(a =>
                a.PatientID == patient.PatientID &&
                a.DateTime == dateTime &&
                a.Status != "Rejected" &&
                a.Status != "Cancelled");

            if (patientBooked)
                return Error("You already have an appointment or request at that date and time.");

            _context.Appointments.Add(new Appointment
            {
                PatientID = patient.PatientID,
                DoctorID = NewAppointmentRequest.DoctorID,
                Reason = reason,
                Urgency = urgency,
                Status = "Pending",
                DoctorResponse = null,
                DocAcknowledged = false,
                PatientAcknowledged = true,
                DateTime = dateTime,
                RequestedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            // Emergency appointment also creates an Emergency Log.
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
        // CANCEL APPOINTMENT
        // =========================================================

        public async Task<IActionResult> OnPostCancelAppointmentAsync(int appointmentId)
        {
            var patient = await GetCurrentLinkedPatientAsync();
            if (patient == null) return Error("No patient has been selected.");

            var appointment = await _context.Appointments.FirstOrDefaultAsync(a =>
                a.AppointmentRequestID == appointmentId &&
                a.PatientID == patient.PatientID);

            if (appointment == null)
                return Error("The selected appointment could not be found.");

            if (IsClosedStatus(appointment.Status))
                return Error("This appointment is already closed.");

            var pendingChange = await _context.AppointmentChangeRequests
                .Where(r =>
                    r.AppointmentRequestID == appointmentId &&
                    r.Status == "Pending")
                .OrderByDescending(r => r.RequestedAt)
                .FirstOrDefaultAsync();

            DateTime appointmentTime =
                pendingChange?.RequestedDateTime ?? appointment.DateTime;

            if (appointmentTime <= GetSingaporeNow())
                return Error("Past appointments cannot be cancelled.");

            appointment.Status = "Cancelled";

            // Close any pending reschedule request too.
            if (pendingChange != null)
            {
                pendingChange.Status = "Cancelled";
                pendingChange.ReviewedAt = DateTime.UtcNow;
                pendingChange.ReviewMessage = "Appointment cancelled by patient.";
            }

            await _context.SaveChangesAsync();

            return Message("Appointment cancelled successfully.");
        }

        // =========================================================
        // ASK DOCTOR
        // =========================================================

        public async Task<IActionResult> OnPostAskDoctorAsync()
        {
            var patient = await GetCurrentLinkedPatientAsync();
            if (patient == null) return Error("No patient has been selected.");

            string question = QuestionMessage?.Trim() ?? "";

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
        // DOCTOR AVAILABILITY
        // Used by User appointment time dropdown
        // =========================================================

        public async Task<JsonResult> OnGetAvailableTimesAsync(
            string? doctorId,
            string? date,
            int? appointmentId)
        {
            if (string.IsNullOrWhiteSpace(doctorId) ||
                !DateTime.TryParse(date, out DateTime selectedDate))
            {
                return new JsonResult(new
                {
                    success = false,
                    bookedTimes = Array.Empty<string>()
                });
            }

            DateTime start = selectedDate.Date;
            DateTime end = start.AddDays(1);

            var query = _context.Appointments.AsNoTracking().Where(a =>
                a.DoctorID == doctorId &&
                a.DateTime >= start &&
                a.DateTime < end &&
                a.Status != "Rejected" &&
                a.Status != "Cancelled");

            // Don't block the current appointment's own slot during rescheduling.
            if (appointmentId.HasValue)
                query = query.Where(a =>
                    a.AppointmentRequestID != appointmentId.Value);

            var dates = await query.Select(a => a.DateTime).ToListAsync();

            return new JsonResult(new
            {
                success = true,
                bookedTimes = dates
                    .Select(d => d.ToString("HH:mm"))
                    .Distinct()
                    .ToList()
            });
        }

        // =========================================================
        // DOCTORS
        // =========================================================

        private async Task LoadDoctorsAsync()
        {
            var role = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name == "Doctor");

            if (role == null) return;

            var doctors = await _context.UserRoles
                .Where(r => r.RoleId == role.Id)
                .Join(_context.Users,
                    r => r.UserId,
                    u => u.Id,
                    (r, u) => u)
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            DoctorsList = doctors.Select(d => new SelectListItem
            {
                Value = d.Id,
                Text = $"Dr. {d.FirstName} {d.LastName}"
            }).ToList();
        }

        private async Task<bool> IsValidDoctorAsync(string doctorId)
        {
            var role = await _context.Roles
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Name == "Doctor");

            return role != null &&
                   await _context.UserRoles.AsNoTracking().AnyAsync(r =>
                       r.RoleId == role.Id &&
                       r.UserId == doctorId);
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

            var doctorIds = appointments
                .Where(a => !string.IsNullOrWhiteSpace(a.DoctorID))
                .Select(a => a.DoctorID!)
                .Distinct()
                .ToList();

            var doctorNames = await _context.Users
                .AsNoTracking()
                .Where(u => doctorIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => $"Dr. {u.FirstName} {u.LastName}");

            var changes = await _context.AppointmentChangeRequests
                .AsNoTracking()
                .Where(r => r.PatientID == patient.PatientID)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();

            var latestChanges = changes
                .GroupBy(r => r.AppointmentRequestID)
                .ToDictionary(g => g.Key, g => g.First());

            var pendingChanges = changes
                .Where(r => r.Status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
                .GroupBy(r => r.AppointmentRequestID)
                .ToDictionary(g => g.Key, g => g.First());

            var items = appointments.Select(a =>
            {
                pendingChanges.TryGetValue(a.AppointmentRequestID, out var pending);
                latestChanges.TryGetValue(a.AppointmentRequestID, out var latest);

                string doctorName = "Not assigned";

                if (!string.IsNullOrWhiteSpace(a.DoctorID) &&
                    doctorNames.TryGetValue(a.DoctorID, out var name))
                    doctorName = name;

                return new AppointmentDisplayItem
                {
                    AppointmentRequestID = a.AppointmentRequestID,
                    DateTime = a.DateTime,
                    DoctorID = a.DoctorID ?? "",
                    DoctorName = doctorName,
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

            ConfirmedAppointments = items
                .Where(a => a.DateTime >= now && a.IsConfirmed && !a.IsClosed)
                .OrderBy(a => a.DateTime)
                .ToList();

            PendingAppointments = items
                .Where(a =>
                    !a.IsConfirmed &&
                    !a.IsClosed &&
                    (a.DateTime >= now || a.PendingChangeRequest != null))
                .OrderBy(a => a.PendingChangeRequest?.RequestedDateTime ?? a.DateTime)
                .ToList();

            AppointmentHistory = items
                .Where(a =>
                    (a.DateTime < now && a.PendingChangeRequest == null) ||
                    a.IsClosed)
                .OrderByDescending(a => a.DateTime)
                .ToList();

            var lookup = appointments.ToDictionary(a => a.AppointmentRequestID);

            ChangeRequestHistory = changes.Select(r =>
            {
                lookup.TryGetValue(r.AppointmentRequestID, out var appointment);

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
                .Where(r =>
                    r.PatientID == patient.PatientID &&
                    !r.ByAdmin)
                .OrderByDescending(r => r.RequestDate)
                .ToListAsync();
        }

        // =========================================================
        // SELECTED PATIENT
        // =========================================================

        private async Task<Patients?> GetCurrentLinkedPatientAsync()
        {
            string? userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return null;

            int? selectedId =
                HttpContext.Session.GetInt32(SelectedPatientSessionKey);

            if (selectedId.HasValue)
            {
                bool owns = await _context.Patients.AsNoTracking().AnyAsync(p =>
                    p.PatientID == selectedId.Value &&
                    p.UserID == userId);

                bool related = await _context.Relationships.AsNoTracking().AnyAsync(r =>
                    r.PatientID == selectedId.Value &&
                    r.UserID == userId);

                if (owns || related)
                {
                    return await _context.Patients
                        .Include(p => p.User)
                        .FirstOrDefaultAsync(p =>
                            p.PatientID == selectedId.Value);
                }

                HttpContext.Session.Remove(SelectedPatientSessionKey);
            }

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserID == userId);

            if (patient != null)
                HttpContext.Session.SetInt32(
                    SelectedPatientSessionKey,
                    patient.PatientID);

            return patient;
        }

        // =========================================================
        // HELPERS
        // =========================================================

        public string FormatAppointmentDate(DateTime date) =>
            date.ToString("dd MMM yyyy, hh:mm tt");

        public string FormatUtcDate(DateTime date) =>
            ToSingaporeTime(date).ToString("dd MMM yyyy, hh:mm tt");

        private IActionResult Message(string message)
        {
            TempData["CareUpdateMessage"] = message;
            return RedirectToPage();
        }

        private IActionResult Error(string message)
        {
            TempData["CareUpdateError"] = message;
            return RedirectToPage();
        }

        private static bool IsValidAppointmentSlot(DateTime date) =>
            date.Hour >= 9 &&
            date.Hour <= 17 &&
            date.Minute == 0 &&
            date.Second == 0;

        private static bool IsReceptionAcknowledged(Appointment appointment)
        {
            if (appointment.DocAcknowledged) return true;
            if (string.IsNullOrWhiteSpace(appointment.Status)) return false;

            string[] statuses =
            {
                "Approved", "Scheduled", "Rescheduled",
                "Confirmed", "Awaiting Patient"
            };

            return statuses.Contains(
                appointment.Status,
                StringComparer.OrdinalIgnoreCase);
        }

        private static bool IsClosedStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status)) return false;

            string[] statuses = { "Rejected", "Cancelled", "Completed" };

            return statuses.Contains(
                status,
                StringComparer.OrdinalIgnoreCase);
        }

        private static DateTime GetSingaporeNow() =>
            TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                GetSingaporeTimeZone());

        private static DateTime ToSingaporeTime(DateTime date)
        {
            DateTime utc = date.Kind switch
            {
                DateTimeKind.Utc => date,
                DateTimeKind.Local => date.ToUniversalTime(),
                _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
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
            public string DoctorID { get; set; } = "";
            public string? Reason { get; set; }
            public string Urgency { get; set; } = "Normal";
        }

        public sealed class AppointmentDisplayItem
        {
            public int AppointmentRequestID { get; set; }
            public DateTime DateTime { get; set; }
            public string DoctorID { get; set; } = "";
            public string DoctorName { get; set; } = "Not assigned";
            public string Reason { get; set; } = "";
            public string Urgency { get; set; } = "";
            public string Status { get; set; } = "";
            public string? DoctorResponse { get; set; }
            public bool DocAcknowledged { get; set; }
            public bool PatientAcknowledged { get; set; }
            public DateTime RequestedAt { get; set; }
            public bool IsConfirmed { get; set; }
            public bool IsClosed { get; set; }
            public AppointmentChangeRequest? PendingChangeRequest { get; set; }
            public AppointmentChangeRequest? LatestChangeRequest { get; set; }

            public bool CanAcknowledge =>
                IsConfirmed && !IsClosed && !PatientAcknowledged;

            public bool CanRequestDateChange =>
                IsConfirmed && !IsClosed && PendingChangeRequest == null;
        }

        public sealed class ChangeRequestDisplayItem
        {
            public int AppointmentChangeRequestID { get; set; }
            public int AppointmentRequestID { get; set; }
            public DateTime? CurrentAppointmentDate { get; set; }
            public DateTime RequestedDateTime { get; set; }
            public string? Reason { get; set; }
            public string Status { get; set; } = "";
            public DateTime RequestedAt { get; set; }
            public DateTime? ReviewedAt { get; set; }
            public string? ReviewMessage { get; set; }
        }
    }
}