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

        /*
         * Appointments accepted or assigned by Reception.
         *
         * This includes:
         * - Awaiting Patient
         * - Confirmed
         */
        public List<AppointmentDisplayItem>
            ConfirmedAppointments
        { get; set; } = new();

        /*
         * Appointment dates proposed by the patient
         * and awaiting Reception.
         */
        public List<AppointmentDisplayItem>
            PendingAppointments
        { get; set; } = new();

        public List<AppointmentDisplayItem>
            AppointmentHistory
        { get; set; } = new();

        /*
         * Retained temporarily so the existing cshtml does
         * not fail to compile.
         *
         * The single-table workflow no longer keeps a separate
         * date-change history, so this list remains empty.
         */
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
         * Patient acknowledges a date already accepted
         * or assigned by Reception.
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

            /*
             * Also recognises old Scheduled, Approved and
             * Rescheduled records for compatibility.
             */
            if (!IsReceptionAcknowledged(appointment))
            {
                SetError(
                    "This appointment is still awaiting " +
                    "Reception review.");

                return RedirectToPage();
            }

            /*
             * Correct older records where the status indicated
             * Reception approval but DocAcknowledged was false.
             */
            appointment.DocAcknowledged = true;

            if (appointment.PatientAcknowledged)
            {
                if (!string.Equals(
                        appointment.Status,
                        "Confirmed",
                        StringComparison.OrdinalIgnoreCase))
                {
                    appointment.Status = "Confirmed";
                    await _context.SaveChangesAsync();
                }

                SetMessage(
                    "This appointment has already been confirmed.");

                return RedirectToPage();
            }

            appointment.PatientAcknowledged = true;
            appointment.Status = "Confirmed";

            await _context.SaveChangesAsync();

            SetMessage(
                "The appointment has been confirmed.");

            return RedirectToPage();
        }

        /*
         * Patient requests another date using the same
         * Appointments row.
         *
         * Under the team's chosen workflow, the previous date
         * is replaced immediately and Reception must accept the
         * newly proposed date.
         */
        public async Task<IActionResult>
            OnPostRequestDateChangeAsync(
                int appointmentId,
                DateTime? requestedDateTime)
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

            /*
             * Patient may only change an appointment that
             * Reception previously accepted or assigned.
             */
            if (!IsReceptionAcknowledged(appointment))
            {
                SetError(
                    "This appointment is already awaiting " +
                    "Reception review.");

                return RedirectToPage();
            }

            if (appointment.AppointmentDate == requestedTime)
            {
                SetError(
                    "The requested date is the same as the " +
                    "current appointment date.");

                return RedirectToPage();
            }

            /*
             * Patient accepts the date they have proposed.
             * Reception must now review it.
             */
            appointment.AppointmentDate = requestedTime;
            appointment.PatientAcknowledged = true;
            appointment.DocAcknowledged = false;
            appointment.Status = "Awaiting Reception";

            /*
             * Remove an old Reception response because it may
             * relate to the previous date.
             */
            appointment.DoctorResponse = null;

            await _context.SaveChangesAsync();

            SetMessage(
                "Your preferred date has been sent to " +
                "Reception for confirmation.");

            return RedirectToPage();
        }

        /*
         * Patient proposes a new future appointment.
         *
         * Since the patient selected this date:
         * PatientAcknowledged = true
         *
         * Reception has not accepted it:
         * DocAcknowledged = false
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
                string.Equals(
                    NewAppointmentRequest.Urgency,
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
                        appointment.AppointmentDate ==
                            preferredDateTime &&
                        appointment.Status != "Rejected" &&
                        appointment.Status != "Cancelled" &&
                        appointment.Status != "Completed");

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

                    Reason =
                        reason,

                    Urgency =
                        urgency,

                    /*
                     * Patient selected this date and is waiting
                     * for Reception to accept it.
                     */
                    Status =
                        "Awaiting Reception",

                    DoctorResponse =
                        null,

                    DocAcknowledged =
                        false,

                    PatientAcknowledged =
                        true,

                    AppointmentDate =
                        preferredDateTime,

                    RequestedAt =
                        DateTime.UtcNow
                };

            _context.Appointments.Add(
                appointment);

            await _context.SaveChangesAsync();

            SetMessage(
                "Your appointment request was submitted " +
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

                    ReplyMessage =
                        null,

                    Completed =
                        false
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
                        appointment.AppointmentDate)
                    .ToListAsync();

            List<AppointmentDisplayItem> displayItems =
                appointments
                    .Select(appointment =>
                    {
                        bool isClosed =
                            IsClosedStatus(
                                appointment.Status);

                        bool receptionAcknowledged =
                            IsReceptionAcknowledged(
                                appointment);

                        string workflowStatus =
                            GetAppointmentWorkflowStatus(
                                appointment);

                        return new AppointmentDisplayItem
                        {
                            AppointmentRequestID =
                                appointment
                                    .AppointmentRequestID,

                            /*
                             * The display item keeps the name
                             * DateTime so your existing cshtml
                             * continues to compile.
                             */
                            DateTime =
                                appointment.AppointmentDate,

                            Reason =
                                appointment.Reason,

                            Urgency =
                                appointment.Urgency,

                            Status =
                                workflowStatus,

                            DoctorResponse =
                                appointment.DoctorResponse,

                            DocAcknowledged =
                                receptionAcknowledged,

                            PatientAcknowledged =
                                appointment
                                    .PatientAcknowledged,

                            RequestedAt =
                                appointment.RequestedAt,

                            /*
                             * Here, IsConfirmed means Reception
                             * has accepted or assigned the date.
                             *
                             * The final status may still be
                             * Awaiting Patient.
                             */
                            IsConfirmed =
                                receptionAcknowledged,

                            IsClosed =
                                isClosed,

                            /*
                             * Compatibility properties for the
                             * existing cshtml. There is no separate
                             * date-change entity anymore.
                             */
                            PendingChangeRequest =
                                null,

                            LatestChangeRequest =
                                null
                        };
                    })
                    .ToList();

            DateTime singaporeNow =
                GetSingaporeNow();

            /*
             * Reception has assigned or accepted these dates.
             *
             * They may be:
             * - Awaiting Patient
             * - Confirmed
             */
            ConfirmedAppointments =
                displayItems
                    .Where(item =>
                        item.DateTime >= singaporeNow &&
                        item.DocAcknowledged &&
                        !item.IsClosed)
                    .OrderBy(item =>
                        item.DateTime)
                    .ToList();

            /*
             * Reception has not yet accepted these patient
             * proposed dates.
             */
            PendingAppointments =
                displayItems
                    .Where(item =>
                        item.DateTime >= singaporeNow &&
                        !item.DocAcknowledged &&
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

            /*
             * Separate date-change history no longer exists.
             */
            ChangeRequestHistory =
                new List<ChangeRequestDisplayItem>();

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

            /*
             * First check whether the logged-in account is
             * the patient.
             */
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

            /*
             * Otherwise check whether the account is linked
             * to a patient through Relationships.
             */
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

        /*
         * Supports older status values while the team
         * transitions to acknowledgement-based workflow.
         */
        private static bool
            IsReceptionAcknowledged(
                Appointment appointment)
        {
            if (appointment.DocAcknowledged)
            {
                return true;
            }

            return string.Equals(
                       appointment.Status,
                       "Approved",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       appointment.Status,
                       "Scheduled",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       appointment.Status,
                       "Rescheduled",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       appointment.Status,
                       "Confirmed",
                       StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(
                       appointment.Status,
                       "Awaiting Patient",
                       StringComparison.OrdinalIgnoreCase);
        }

        private static string
            GetAppointmentWorkflowStatus(
                Appointment appointment)
        {
            if (IsClosedStatus(appointment.Status))
            {
                return appointment.Status;
            }

            bool receptionAcknowledged =
                IsReceptionAcknowledged(
                    appointment);

            if (receptionAcknowledged &&
                appointment.PatientAcknowledged)
            {
                return "Confirmed";
            }

            if (receptionAcknowledged &&
                !appointment.PatientAcknowledged)
            {
                return "Awaiting Patient";
            }

            if (!receptionAcknowledged &&
                appointment.PatientAcknowledged)
            {
                return "Awaiting Reception";
            }

            return "Pending";
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

            /*
             * Kept as DateTime for compatibility with the
             * current CareUpdates.cshtml.
             */
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

            /*
             * Compatibility properties only.
             * They are always null under the single-table design.
             */
            public DateChangeDisplayItem?
                PendingChangeRequest
            {
                get;
                set;
            }

            public DateChangeDisplayItem?
                LatestChangeRequest
            {
                get;
                set;
            }

            public bool CanAcknowledge =>
                DocAcknowledged &&
                !PatientAcknowledged &&
                !IsClosed;

            public bool CanRequestDateChange =>
                DocAcknowledged &&
                !IsClosed;
        }

        /*
         * Compatibility display class for the existing cshtml.
         * It is not an EF entity and does not create a table.
         */
        public sealed class DateChangeDisplayItem
        {
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
        }

        /*
         * Compatibility class for the existing date-change
         * history section.
         *
         * ChangeRequestHistory is always empty because the
         * separate table has been removed.
         */
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