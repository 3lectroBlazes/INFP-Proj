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
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public CareUpdatesModel(
            AppDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public bool HasPatientRecord { get; set; }

        public int PatientId { get; set; }

        public string PatientName { get; set; } =
            string.Empty;

        public List<AppointmentDisplayItem>
            ConfirmedAppointments
        { get; set; } = new();

        public List<AppointmentDisplayItem>
            PendingAppointments
        { get; set; } = new();

        public List<AppointmentDisplayItem>
            AppointmentHistory
        { get; set; } = new();

        public List<ChangeRequestDisplayItem>
            ChangeRequestHistory
        { get; set; } = new();

        public List<DoctorRequest>
            DoctorRequests
        { get; set; } = new();

        [BindProperty]
        public NewAppointmentInput
            NewAppointmentRequest
        { get; set; } = new();

        [BindProperty]
        public string? QuestionMessage { get; set; }

        public string MinimumDateTimeValue =>
            GetSingaporeNow()
                .AddMinutes(5)
                .ToString("yyyy-MM-ddTHH:mm");

        public async Task OnGetAsync()
        {
            await LoadPageDataAsync();
        }

        /*
         * The patient acknowledges a confirmed appointment.
         * This does not change the appointment date or status.
         */
        public async Task<IActionResult>
            OnPostAcknowledgeAppointmentAsync(
                int appointmentId)
        {
            Patients? patient =
                await GetCurrentLinkedPatientAsync();

            if (patient == null)
            {
                SetError(
                    "No patient record is linked to this account.");

                return RedirectToPage();
            }

            if (appointmentId <= 0)
            {
                SetError(
                    "The appointment was not selected correctly.");

                return RedirectToPage();
            }

            Appointment? appointment =
                await _context.Appointments
                    .FirstOrDefaultAsync(item =>
                        item.AppointmentRequestID ==
                            appointmentId &&
                        item.PatientID ==
                            patient.PatientID);

            if (appointment == null)
            {
                SetError(
                    "The selected appointment could not be found.");

                return RedirectToPage();
            }

            if (IsClosedStatus(appointment.Status))
            {
                SetError(
                    "A cancelled, rejected or completed " +
                    "appointment cannot be acknowledged.");

                return RedirectToPage();
            }

            if (!IsConfirmedAppointment(appointment))
            {
                SetError(
                    "This appointment is still awaiting " +
                    "Reception approval.");

                return RedirectToPage();
            }

            if (appointment.PatientAcknowledged)
            {
                SetMessage(
                    "This appointment has already been acknowledged.");

                return RedirectToPage();
            }

            appointment.PatientAcknowledged = true;

            await _context.SaveChangesAsync();

            SetMessage(
                "The appointment has been acknowledged.");

            return RedirectToPage();
        }

        /*
         * Creates a separate pending date-change request.
         * The confirmed Appointment.DateTime is not changed here.
         */
        public async Task<IActionResult>
            OnPostRequestDateChangeAsync(
                int appointmentId,
                DateTime? requestedDateTime,
                string? changeReason)
        {
            Patients? patient =
                await GetCurrentLinkedPatientAsync();

            if (patient == null)
            {
                SetError(
                    "No patient record is linked to this account.");

                return RedirectToPage();
            }

            if (appointmentId <= 0)
            {
                SetError(
                    "The appointment was not selected correctly.");

                return RedirectToPage();
            }

            if (!requestedDateTime.HasValue)
            {
                SetError(
                    "Select your preferred new date and time.");

                return RedirectToPage();
            }

            DateTime requestedTime =
                DateTime.SpecifyKind(
                    requestedDateTime.Value,
                    DateTimeKind.Unspecified);

            if (requestedTime <= GetSingaporeNow())
            {
                SetError(
                    "The requested appointment date must " +
                    "be in the future.");

                return RedirectToPage();
            }

            Appointment? appointment =
                await _context.Appointments
                    .FirstOrDefaultAsync(item =>
                        item.AppointmentRequestID ==
                            appointmentId &&
                        item.PatientID ==
                            patient.PatientID);

            if (appointment == null)
            {
                SetError(
                    "The selected appointment could not be found.");

                return RedirectToPage();
            }

            if (IsClosedStatus(appointment.Status))
            {
                SetError(
                    "A cancelled, rejected or completed " +
                    "appointment cannot be changed.");

                return RedirectToPage();
            }

            if (!IsConfirmedAppointment(appointment))
            {
                SetError(
                    "A date change can only be requested after " +
                    "Reception confirms the appointment.");

                return RedirectToPage();
            }

            if (appointment.DateTime == requestedTime)
            {
                SetError(
                    "The requested time is the same as the " +
                    "current appointment time.");

                return RedirectToPage();
            }

            string reason =
                changeReason?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(reason))
            {
                SetError(
                    "Enter a reason for requesting another date.");

                return RedirectToPage();
            }

            if (reason.Length > 500)
            {
                SetError(
                    "The reason cannot exceed 500 characters.");

                return RedirectToPage();
            }

            bool pendingRequestExists =
                await _context
                    .AppointmentChangeRequests
                    .AsNoTracking()
                    .AnyAsync(request =>
                        request.AppointmentRequestID ==
                            appointmentId &&
                        request.Status == "Pending");

            if (pendingRequestExists)
            {
                SetError(
                    "A date-change request is already pending " +
                    "for this appointment.");

                return RedirectToPage();
            }

            var changeRequest =
                new AppointmentChangeRequest
                {
                    AppointmentRequestID =
                        appointment.AppointmentRequestID,

                    PatientID =
                        patient.PatientID,

                    RequestedDateTime =
                        requestedTime,

                    Reason = reason,

                    Status = "Pending",

                    RequestedAt =
                        DateTime.UtcNow,

                    ReviewedAt = null,

                    ReviewMessage = null,

                    ReviewedByUserID = null
                };

            _context.AppointmentChangeRequests.Add(
                changeRequest);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                SetError(
                    "A pending date-change request already " +
                    "exists for this appointment.");

                return RedirectToPage();
            }

            SetMessage(
                "Your preferred date has been sent to " +
                "Reception for approval. The current confirmed " +
                "appointment remains unchanged.");

            return RedirectToPage();
        }

        /*
         * Creates another Appointment row.
         * Patients may have multiple future appointment requests.
         */
        public async Task<IActionResult>
            OnPostRequestNewAppointmentAsync()
        {
            Patients? patient =
                await GetCurrentLinkedPatientAsync();

            if (patient == null)
            {
                SetError(
                    "No patient record is linked to this account.");

                return RedirectToPage();
            }

            if (!NewAppointmentRequest
                    .PreferredDateTime
                    .HasValue)
            {
                SetError(
                    "Select a preferred appointment date and time.");

                return RedirectToPage();
            }

            DateTime preferredDateTime =
                DateTime.SpecifyKind(
                    NewAppointmentRequest
                        .PreferredDateTime
                        .Value,
                    DateTimeKind.Unspecified);

            if (preferredDateTime <= GetSingaporeNow())
            {
                SetError(
                    "The preferred appointment date must " +
                    "be in the future.");

                return RedirectToPage();
            }

            string reason =
                NewAppointmentRequest
                    .Reason?
                    .Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(reason))
            {
                SetError(
                    "Enter a reason for the appointment.");

                return RedirectToPage();
            }

            if (reason.Length > 500)
            {
                SetError(
                    "The appointment reason cannot exceed " +
                    "500 characters.");

                return RedirectToPage();
            }

            string urgency =
                NewAppointmentRequest.Urgency
                    .Equals(
                        "Urgent",
                        StringComparison.OrdinalIgnoreCase)
                    ? "Urgent"
                    : "Normal";

            bool duplicateRequestExists =
                await _context.Appointments
                    .AsNoTracking()
                    .AnyAsync(appointment =>
                        appointment.PatientID ==
                            patient.PatientID &&
                        appointment.DateTime ==
                            preferredDateTime &&
                        appointment.Status != "Rejected" &&
                        appointment.Status != "Cancelled");

            if (duplicateRequestExists)
            {
                SetError(
                    "An appointment or request already exists " +
                    "at that exact date and time.");

                return RedirectToPage();
            }

            var appointment =
                new Appointment
                {
                    PatientID =
                        patient.PatientID,

                    Reason = reason,

                    Urgency = urgency,

                    Status = "Pending",

                    DoctorResponse = null,

                    DocAcknowledged = false,

                    PatientAcknowledged = false,

                    DateTime =
                        preferredDateTime,

                    RequestedAt =
                        DateTime.UtcNow
                };

            _context.Appointments.Add(
                appointment);

            await _context.SaveChangesAsync();

            SetMessage(
                "Your future appointment request was submitted " +
                "to Reception for review.");

            return RedirectToPage();
        }

        public async Task<IActionResult>
            OnPostAskDoctorAsync()
        {
            Patients? patient =
                await GetCurrentLinkedPatientAsync();

            if (patient == null)
            {
                SetError(
                    "No patient record is linked to this account.");

                return RedirectToPage();
            }

            string question =
                QuestionMessage?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(question))
            {
                SetError(
                    "Enter a question for the doctor.");

                return RedirectToPage();
            }

            if (question.Length > 1000)
            {
                SetError(
                    "The question cannot exceed 1,000 characters.");

                return RedirectToPage();
            }

            var doctorRequest =
                new DoctorRequest
                {
                    PatientID =
                        patient.PatientID,

                    RequestMessage =
                        question,

                    RequestDate =
                        DateTime.UtcNow,

                    ReplyMessage = null,

                    Completed = false
                };

            _context.DoctorRequests.Add(
                doctorRequest);

            await _context.SaveChangesAsync();

            SetMessage(
                "Your question was submitted to the doctor.");

            return RedirectToPage();
        }

        private async Task LoadPageDataAsync()
        {
            Patients? patient =
                await GetCurrentLinkedPatientAsync();

            if (patient == null)
            {
                HasPatientRecord = false;
                return;
            }

            HasPatientRecord = true;

            PatientId =
                patient.PatientID;

            PatientName =
                patient.User == null
                    ? $"Patient #{patient.PatientID}"
                    : ($"{patient.User.FirstName} " +
                       $"{patient.User.LastName}")
                        .Trim();

            List<Appointment> appointments =
                await _context.Appointments
                    .AsNoTracking()
                    .Where(appointment =>
                        appointment.PatientID ==
                            patient.PatientID)
                    .OrderBy(appointment =>
                        appointment.DateTime)
                    .ToListAsync();

            List<AppointmentChangeRequest>
                changeRequests =
                    await _context
                        .AppointmentChangeRequests
                        .AsNoTracking()
                        .Where(request =>
                            request.PatientID ==
                                patient.PatientID)
                        .OrderByDescending(request =>
                            request.RequestedAt)
                        .ToListAsync();

            Dictionary<int, AppointmentChangeRequest>
                latestRequestByAppointment =
                    changeRequests
                        .GroupBy(request =>
                            request.AppointmentRequestID)
                        .ToDictionary(
                            group => group.Key,
                            group => group
                                .OrderByDescending(request =>
                                    request.RequestedAt)
                                .First());

            Dictionary<int, AppointmentChangeRequest>
                pendingRequestByAppointment =
                    changeRequests
                        .Where(request =>
                            request.Status.Equals(
                                "Pending",
                                StringComparison.OrdinalIgnoreCase))
                        .GroupBy(request =>
                            request.AppointmentRequestID)
                        .ToDictionary(
                            group => group.Key,
                            group => group
                                .OrderByDescending(request =>
                                    request.RequestedAt)
                                .First());

            List<AppointmentDisplayItem> displayItems =
                appointments
                    .Select(appointment =>
                    {
                        pendingRequestByAppointment
                            .TryGetValue(
                                appointment
                                    .AppointmentRequestID,
                                out AppointmentChangeRequest?
                                    pendingRequest);

                        latestRequestByAppointment
                            .TryGetValue(
                                appointment
                                    .AppointmentRequestID,
                                out AppointmentChangeRequest?
                                    latestRequest);

                        bool isConfirmed =
                            IsConfirmedAppointment(
                                appointment);

                        bool isClosed =
                            IsClosedStatus(
                                appointment.Status);

                        return new AppointmentDisplayItem
                        {
                            AppointmentRequestID =
                                appointment
                                    .AppointmentRequestID,

                            DateTime =
                                appointment.DateTime,

                            Reason =
                                appointment.Reason,

                            Urgency =
                                appointment.Urgency,

                            Status =
                                appointment.Status,

                            DoctorResponse =
                                appointment.DoctorResponse,

                            DocAcknowledged =
                                appointment.DocAcknowledged,

                            PatientAcknowledged =
                                appointment
                                    .PatientAcknowledged,

                            RequestedAt =
                                appointment.RequestedAt,

                            IsConfirmed =
                                isConfirmed,

                            IsClosed =
                                isClosed,

                            PendingChangeRequest =
                                pendingRequest,

                            LatestChangeRequest =
                                latestRequest
                        };
                    })
                    .ToList();

            DateTime singaporeNow =
                GetSingaporeNow();

            ConfirmedAppointments =
                displayItems
                    .Where(item =>
                        item.DateTime >= singaporeNow &&
                        item.IsConfirmed &&
                        !item.IsClosed)
                    .OrderBy(item =>
                        item.DateTime)
                    .ToList();

            PendingAppointments =
                displayItems
                    .Where(item =>
                        item.DateTime >= singaporeNow &&
                        !item.IsConfirmed &&
                        !item.IsClosed)
                    .OrderBy(item =>
                        item.DateTime)
                    .ToList();

            AppointmentHistory =
                displayItems
                    .Where(item =>
                        item.DateTime < singaporeNow ||
                        item.IsClosed)
                    .OrderByDescending(item =>
                        item.DateTime)
                    .ToList();

            Dictionary<int, Appointment>
                appointmentLookup =
                    appointments.ToDictionary(
                        appointment =>
                            appointment
                                .AppointmentRequestID);

            ChangeRequestHistory =
                changeRequests
                    .Select(request =>
                    {
                        appointmentLookup.TryGetValue(
                            request.AppointmentRequestID,
                            out Appointment? appointment);

                        return new ChangeRequestDisplayItem
                        {
                            AppointmentChangeRequestID =
                                request
                                    .AppointmentChangeRequestID,

                            AppointmentRequestID =
                                request
                                    .AppointmentRequestID,

                            CurrentAppointmentDate =
                                appointment?.DateTime,

                            RequestedDateTime =
                                request.RequestedDateTime,

                            Reason =
                                request.Reason,

                            Status =
                                request.Status,

                            RequestedAt =
                                request.RequestedAt,

                            ReviewedAt =
                                request.ReviewedAt,

                            ReviewMessage =
                                request.ReviewMessage
                        };
                    })
                    .ToList();

            DoctorRequests =
                await _context.DoctorRequests
                    .AsNoTracking()
                    .Where(request =>
                        request.PatientID ==
                            patient.PatientID)
                    .OrderByDescending(request =>
                        request.RequestDate)
                    .ToListAsync();
        }

        private async Task<Patients?>
            GetCurrentLinkedPatientAsync()
        {
            string? currentUserId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(
                currentUserId))
            {
                return null;
            }

            Patients? patient =
                await _context.Patients
                    .Include(item =>
                        item.User)
                    .FirstOrDefaultAsync(item =>
                        item.UserID ==
                            currentUserId);

            if (patient != null)
            {
                return patient;
            }

            int? linkedPatientId =
                await _context.Relationships
                    .Where(relationship =>
                        relationship.UserID ==
                            currentUserId)
                    .Select(relationship =>
                        (int?)
                        relationship.PatientID)
                    .FirstOrDefaultAsync();

            if (!linkedPatientId.HasValue)
            {
                return null;
            }

            return await _context.Patients
                .Include(item =>
                    item.User)
                .FirstOrDefaultAsync(item =>
                    item.PatientID ==
                        linkedPatientId.Value);
        }

        public string FormatAppointmentDate(
            DateTime dateTime)
        {
            return dateTime.ToString(
                "dd MMM yyyy, hh:mm tt");
        }

        public string FormatUtcDate(
            DateTime dateTime)
        {
            return ToSingaporeTime(dateTime)
                .ToString(
                    "dd MMM yyyy, hh:mm tt");
        }

        public string FormatOptionalUtcDate(
            DateTime? dateTime)
        {
            return dateTime.HasValue
                ? FormatUtcDate(dateTime.Value)
                : "-";
        }

        private void SetMessage(
            string message)
        {
            TempData["CareUpdateMessage"] =
                message;
        }

        private void SetError(
            string message)
        {
            TempData["CareUpdateError"] =
                message;
        }

        private static bool
            IsConfirmedAppointment(
                Appointment appointment)
        {
            if (appointment.DocAcknowledged)
            {
                return true;
            }

            return appointment.Status.Equals(
                       "Approved",
                       StringComparison.OrdinalIgnoreCase) ||
                   appointment.Status.Equals(
                       "Scheduled",
                       StringComparison.OrdinalIgnoreCase) ||
                   appointment.Status.Equals(
                       "Rescheduled",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsClosedStatus(
            string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
            {
                return false;
            }

            return status.Equals(
                       "Rejected",
                       StringComparison.OrdinalIgnoreCase) ||
                   status.Equals(
                       "Cancelled",
                       StringComparison.OrdinalIgnoreCase) ||
                   status.Equals(
                       "Completed",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static DateTime
            GetSingaporeNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.UtcNow,
                GetSingaporeTimeZone());
        }

        private static DateTime ToSingaporeTime(
            DateTime dateTime)
        {
            DateTime utcDateTime =
                dateTime.Kind switch
                {
                    DateTimeKind.Utc =>
                        dateTime,

                    DateTimeKind.Local =>
                        dateTime.ToUniversalTime(),

                    _ =>
                        DateTime.SpecifyKind(
                            dateTime,
                            DateTimeKind.Utc)
                };

            return TimeZoneInfo.ConvertTimeFromUtc(
                utcDateTime,
                GetSingaporeTimeZone());
        }

        private static TimeZoneInfo
            GetSingaporeTimeZone()
        {
            try
            {
                return TimeZoneInfo
                    .FindSystemTimeZoneById(
                        "Singapore Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo
                    .FindSystemTimeZoneById(
                        "Asia/Singapore");
            }
        }

        public sealed class NewAppointmentInput
        {
            public DateTime? PreferredDateTime
            {
                get;
                set;
            }

            public string? Reason
            {
                get;
                set;
            }

            public string Urgency
            {
                get;
                set;
            } = "Normal";
        }

        public sealed class AppointmentDisplayItem
        {
            public int AppointmentRequestID
            {
                get;
                set;
            }

            public DateTime DateTime
            {
                get;
                set;
            }

            public string Reason
            {
                get;
                set;
            } = string.Empty;

            public string Urgency
            {
                get;
                set;
            } = string.Empty;

            public string Status
            {
                get;
                set;
            } = string.Empty;

            public string? DoctorResponse
            {
                get;
                set;
            }

            public bool DocAcknowledged
            {
                get;
                set;
            }

            public bool PatientAcknowledged
            {
                get;
                set;
            }

            public DateTime RequestedAt
            {
                get;
                set;
            }

            public bool IsConfirmed
            {
                get;
                set;
            }

            public bool IsClosed
            {
                get;
                set;
            }

            public AppointmentChangeRequest?
                PendingChangeRequest
            {
                get;
                set;
            }

            public AppointmentChangeRequest?
                LatestChangeRequest
            {
                get;
                set;
            }

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
            public int AppointmentChangeRequestID
            {
                get;
                set;
            }

            public int AppointmentRequestID
            {
                get;
                set;
            }

            public DateTime? CurrentAppointmentDate
            {
                get;
                set;
            }

            public DateTime RequestedDateTime
            {
                get;
                set;
            }

            public string? Reason
            {
                get;
                set;
            }

            public string Status
            {
                get;
                set;
            } = string.Empty;

            public DateTime RequestedAt
            {
                get;
                set;
            }

            public DateTime? ReviewedAt
            {
                get;
                set;
            }

            public string? ReviewMessage
            {
                get;
                set;
            }
        }
    }
}