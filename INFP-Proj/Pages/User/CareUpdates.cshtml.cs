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
        private const string SelectedPatientSessionKey =
            "SelectedPatientId";

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


        // =========================================================
        // PAGE LOAD
        // =========================================================

        public async Task OnGetAsync()
        {
            await LoadPageDataAsync();
        }


        // =========================================================
        // PATIENT ACKNOWLEDGES APPOINTMENT
        // =========================================================
        //
        // Expected Reception-created/rescheduled state:
        //
        // DocAcknowledged     = true
        // PatientAcknowledged = false
        //
        // D1P0
        //
        // Once patient agrees:
        //
        // D1P1 -> Scheduled
        // =========================================================

        public async Task<IActionResult>
            OnPostAcknowledgeAppointmentAsync(
                int appointmentId)
        {
            Patients? patient =
                await GetCurrentLinkedPatientAsync();

            if (patient == null)
            {
                SetError(
                    "No patient has been selected.");

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

            if (IsClosedStatus(
                    appointment.Status))
            {
                SetError(
                    "A cancelled, rejected or completed " +
                    "appointment cannot be acknowledged.");

                return RedirectToPage();
            }

            /*
             * Reception must have acknowledged
             * the appointment first.
             *
             * Old Approved / Scheduled / Rescheduled rows
             * are recognised for compatibility.
             */
            if (!IsReceptionAcknowledged(
                    appointment))
            {
                SetError(
                    "This appointment is still awaiting Reception approval.");

                return RedirectToPage();
            }

            if (appointment.PatientAcknowledged)
            {
                SetMessage(
                    "This appointment has already been acknowledged.");

                return RedirectToPage();
            }

            /*
             * Some older rows may have Approved,
             * Scheduled or Rescheduled status but
             * DocAcknowledged was never written.
             *
             * Backfill it here so the new D/P workflow
             * becomes consistent.
             */
            if (!appointment.DocAcknowledged)
            {
                appointment.DocAcknowledged =
                    true;
            }

            appointment.PatientAcknowledged =
                true;

            /*
             * Requirement:
             *
             * D1P1 = Scheduled
             */
            if (
                appointment.DocAcknowledged &&
                appointment.PatientAcknowledged
            )
            {
                appointment.Status =
                    "Scheduled";
            }

            await _context.SaveChangesAsync();

            SetMessage(
                "Appointment acknowledged successfully. " +
                "The appointment is now scheduled.");

            return RedirectToPage();
        }


        // =========================================================
        // PATIENT REQUESTS A DIFFERENT DATE
        // =========================================================
        //
        // Patient reschedule requirement:
        //
        // D0P1
        //
        // IMPORTANT:
        //
        // We DO NOT overwrite Appointment.DateTime yet.
        //
        // The requested date stays inside the existing
        // AppointmentChangeRequest table so Reception's
        // approve/reject workflow is preserved.
        //
        // This protects your teammate's existing code.
        // =========================================================

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
                    "No patient has been selected.");

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
                    "The requested appointment date must be in the future.");

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

            if (IsClosedStatus(
                    appointment.Status))
            {
                SetError(
                    "A cancelled, rejected or completed " +
                    "appointment cannot be changed.");

                return RedirectToPage();
            }

            /*
             * Only an appointment already accepted/
             * created by Reception can be rescheduled
             * from this section.
             */
            if (!IsReceptionAcknowledged(
                    appointment))
            {
                SetError(
                    "This appointment is still awaiting Reception approval.");

                return RedirectToPage();
            }

            if (appointment.DateTime ==
                requestedTime)
            {
                SetError(
                    "The requested date and time is the same " +
                    "as the current appointment.");

                return RedirectToPage();
            }

            string reason =
                changeReason?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    reason))
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


            // -----------------------------------------------------
            // Prevent multiple pending date-change requests
            // -----------------------------------------------------

            bool pendingRequestExists =
                await _context
                    .AppointmentChangeRequests
                    .AsNoTracking()
                    .AnyAsync(request =>
                        request.AppointmentRequestID ==
                            appointmentId &&
                        request.Status ==
                            "Pending");

            if (pendingRequestExists)
            {
                SetError(
                    "A date-change request is already pending " +
                    "for this appointment.");

                return RedirectToPage();
            }


            // -----------------------------------------------------
            // Preserve existing change-request system
            // -----------------------------------------------------

            var changeRequest =
                new AppointmentChangeRequest
                {
                    AppointmentRequestID =
                        appointment
                            .AppointmentRequestID,

                    PatientID =
                        patient.PatientID,

                    RequestedDateTime =
                        requestedTime,

                    Reason =
                        reason,

                    Status =
                        "Pending",

                    RequestedAt =
                        DateTime.UtcNow,

                    ReviewedAt =
                        null,

                    ReviewMessage =
                        null,

                    ReviewedByUserID =
                        null
                };


            /*
             * Requirement from recommendation:
             *
             * Patient makes/reschedules:
             *
             * DocAcknowledged     = 0
             * PatientAcknowledged = 1
             *
             * D0P1
             *
             * Appointment.DateTime is deliberately
             * NOT changed yet.
             */
            appointment.DocAcknowledged =
                false;

            appointment.PatientAcknowledged =
                true;

            appointment.Status =
                "Pending";


            _context
                .AppointmentChangeRequests
                .Add(changeRequest);


            try
            {
                await _context
                    .SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                SetError(
                    "A pending date-change request already exists " +
                    "for this appointment.");

                return RedirectToPage();
            }


            SetMessage(
                "Your preferred new date has been submitted. " +
                "It is now awaiting Reception acknowledgement.");

            return RedirectToPage();
        }


        // =========================================================
        // PATIENT CREATES NEW APPOINTMENT
        // =========================================================
        //
        // Requirement:
        //
        // Patient-created appointment = D0P1
        //
        // DocAcknowledged     = false
        // PatientAcknowledged = true
        //
        // Reception must agree before it becomes Scheduled.
        // =========================================================

        public async Task<IActionResult>
            OnPostRequestNewAppointmentAsync()
        {
            Patients? patient =
                await GetCurrentLinkedPatientAsync();

            if (patient == null)
            {
                SetError(
                    "No patient has been selected.");

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

            if (preferredDateTime <=
                GetSingaporeNow())
            {
                SetError(
                    "The preferred appointment date must be in the future.");

                return RedirectToPage();
            }


            string reason =
                NewAppointmentRequest
                    .Reason?
                    .Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    reason))
            {
                SetError(
                    "Enter a reason for the appointment.");

                return RedirectToPage();
            }

            if (reason.Length > 500)
            {
                SetError(
                    "The appointment reason cannot exceed 500 characters.");

                return RedirectToPage();
            }


            // -----------------------------------------------------
            // Urgency:
            //
            // Normal
            // Urgent
            // Emergency
            // -----------------------------------------------------

            string urgency =
                NewAppointmentRequest
                    .Urgency?
                    .Trim()
                    .ToLowerInvariant()
                switch
                {
                    "urgent" =>
                        "Urgent",

                    "emergency" =>
                        "Emergency",

                    _ =>
                        "Normal"
                };


            // -----------------------------------------------------
            // Avoid duplicate exact appointment requests
            // -----------------------------------------------------

            bool duplicateRequestExists =
                await _context.Appointments
                    .AsNoTracking()
                    .AnyAsync(appointment =>
                        appointment.PatientID ==
                            patient.PatientID &&

                        appointment.DateTime ==
                            preferredDateTime &&

                        appointment.Status !=
                            "Rejected" &&

                        appointment.Status !=
                            "Cancelled");

            if (duplicateRequestExists)
            {
                SetError(
                    "An appointment or request already exists " +
                    "at that exact date and time.");

                return RedirectToPage();
            }


            // -----------------------------------------------------
            // D0P1
            // -----------------------------------------------------

            var appointment =
                new Appointment
                {
                    PatientID =
                        patient.PatientID,

                    Reason =
                        reason,

                    Urgency =
                        urgency,

                    Status =
                        "Pending",

                    DoctorResponse =
                        null,

                    // D0
                    DocAcknowledged =
                        false,

                    // P1
                    PatientAcknowledged =
                        true,

                    DateTime =
                        preferredDateTime,

                    RequestedAt =
                        DateTime.UtcNow
                };


            _context.Appointments.Add(
                appointment);

            await _context.SaveChangesAsync();


            SetMessage(
                "Your appointment request was submitted. " +
                "It is now awaiting Reception acknowledgement.");

            return RedirectToPage();
        }


        // =========================================================
        // ASK DOCTOR
        // =========================================================

        public async Task<IActionResult>
            OnPostAskDoctorAsync()
        {
            Patients? patient =
                await GetCurrentLinkedPatientAsync();

            if (patient == null)
            {
                SetError(
                    "No patient has been selected.");

                return RedirectToPage();
            }


            string question =
                QuestionMessage?.Trim()
                ?? string.Empty;

            if (string.IsNullOrWhiteSpace(
                    question))
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
                        false,

                    /*
                     * This is a User-side request,
                     * not an Admin-created request.
                     */
                    ByAdmin =
                        false
                };


            _context.DoctorRequests.Add(
                doctorRequest);

            await _context.SaveChangesAsync();


            SetMessage(
                "Your question was submitted to the doctor.");

            return RedirectToPage();
        }


        // =========================================================
        // LOAD PAGE DATA
        // =========================================================

        private async Task LoadPageDataAsync()
        {
            Patients? patient =
                await GetCurrentLinkedPatientAsync();

            if (patient == null)
            {
                HasPatientRecord =
                    false;

                return;
            }


            HasPatientRecord =
                true;

            PatientId =
                patient.PatientID;

            PatientName =
                patient.User == null
                    ? $"Patient #{patient.PatientID}"
                    : $"{patient.User.FirstName} {patient.User.LastName}"
                        .Trim();


            // -----------------------------------------------------
            // Appointments
            // -----------------------------------------------------

            List<Appointment> appointments =
                await _context.Appointments
                    .AsNoTracking()
                    .Where(appointment =>
                        appointment.PatientID ==
                            patient.PatientID)
                    .OrderBy(appointment =>
                        appointment.DateTime)
                    .ToListAsync();


            // -----------------------------------------------------
            // Existing shared AppointmentChangeRequest records
            // -----------------------------------------------------

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


            // -----------------------------------------------------
            // Latest change request for each appointment
            // -----------------------------------------------------

            Dictionary<int, AppointmentChangeRequest>
                latestRequestByAppointment =
                    changeRequests
                        .GroupBy(request =>
                            request
                                .AppointmentRequestID)
                        .ToDictionary(
                            group =>
                                group.Key,

                            group =>
                                group
                                    .OrderByDescending(
                                        request =>
                                            request
                                                .RequestedAt)
                                    .First());


            // -----------------------------------------------------
            // Pending change request for each appointment
            // -----------------------------------------------------

            Dictionary<int, AppointmentChangeRequest>
                pendingRequestByAppointment =
                    changeRequests
                        .Where(request =>
                            request.Status.Equals(
                                "Pending",
                                StringComparison
                                    .OrdinalIgnoreCase))
                        .GroupBy(request =>
                            request
                                .AppointmentRequestID)
                        .ToDictionary(
                            group =>
                                group.Key,

                            group =>
                                group
                                    .OrderByDescending(
                                        request =>
                                            request
                                                .RequestedAt)
                                    .First());


            // -----------------------------------------------------
            // Convert database rows into page display items
            // -----------------------------------------------------

            List<AppointmentDisplayItem>
                displayItems =
                    appointments
                        .Select(appointment =>
                        {
                            pendingRequestByAppointment
                                .TryGetValue(
                                    appointment
                                        .AppointmentRequestID,

                                    out
                                    AppointmentChangeRequest?
                                        pendingRequest);


                            latestRequestByAppointment
                                .TryGetValue(
                                    appointment
                                        .AppointmentRequestID,

                                    out
                                    AppointmentChangeRequest?
                                        latestRequest);


                            bool isConfirmed =
                                IsReceptionAcknowledged(
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
                                    appointment
                                        .DoctorResponse,

                                DocAcknowledged =
                                    appointment
                                        .DocAcknowledged,

                                PatientAcknowledged =
                                    appointment
                                        .PatientAcknowledged,

                                RequestedAt =
                                    appointment
                                        .RequestedAt,

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


            // -----------------------------------------------------
            // Reception has acknowledged
            //
            // D1P0 or D1P1
            // -----------------------------------------------------

            ConfirmedAppointments =
                displayItems
                    .Where(item =>
                        item.DateTime >=
                            singaporeNow &&

                        item.IsConfirmed &&

                        !item.IsClosed)
                    .OrderBy(item =>
                        item.DateTime)
                    .ToList();


            // -----------------------------------------------------
            // Waiting for Reception
            //
            // Patient-created/rescheduled = D0P1
            // -----------------------------------------------------

            PendingAppointments =
                displayItems
                    .Where(item =>
                        !item.IsConfirmed &&

                        !item.IsClosed &&

                        (
                            item.DateTime >=
                                singaporeNow
                            ||
                            item.PendingChangeRequest !=
                                null
                        ))
                    .OrderBy(item =>
                        item.PendingChangeRequest?
                            .RequestedDateTime
                        ??
                        item.DateTime)
                    .ToList();


            // -----------------------------------------------------
            // Appointment history
            // -----------------------------------------------------

            AppointmentHistory =
                displayItems
                    .Where(item =>
                        (
                            item.DateTime <
                                singaporeNow &&

                            item.PendingChangeRequest ==
                                null
                        )
                        ||
                        item.IsClosed)
                    .OrderByDescending(item =>
                        item.DateTime)
                    .ToList();


            // -----------------------------------------------------
            // Change request history
            // -----------------------------------------------------

            Dictionary<int, Appointment>
                appointmentLookup =
                    appointments
                        .ToDictionary(
                            appointment =>
                                appointment
                                    .AppointmentRequestID);


            ChangeRequestHistory =
                changeRequests
                    .Select(request =>
                    {
                        appointmentLookup
                            .TryGetValue(
                                request
                                    .AppointmentRequestID,

                                out
                                Appointment?
                                    appointment);


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
                                request
                                    .RequestedDateTime,

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


            // -----------------------------------------------------
            // Doctor communication
            //
            // Only User-side requests.
            // -----------------------------------------------------

            DoctorRequests =
                await _context.DoctorRequests
                    .AsNoTracking()
                    .Where(request =>
                        request.PatientID ==
                            patient.PatientID &&

                        !request.ByAdmin)
                    .OrderByDescending(request =>
                        request.RequestDate)
                    .ToListAsync();
        }


        // =========================================================
        // GET SELECTED PATIENT
        // =========================================================
        //
        // This now follows the patient selected on Dashboard.
        //
        // We DO NOT automatically take:
        //
        // Relationships.FirstOrDefault()
        //
        // anymore.
        // =========================================================

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


            int? selectedPatientId =
                HttpContext.Session
                    .GetInt32(
                        SelectedPatientSessionKey);


            // -----------------------------------------------------
            // Existing patient selection
            // -----------------------------------------------------

            if (selectedPatientId.HasValue)
            {
                bool ownsPatient =
                    await _context.Patients
                        .AsNoTracking()
                        .AnyAsync(patient =>
                            patient.PatientID ==
                                selectedPatientId.Value &&

                            patient.UserID ==
                                currentUserId);


                bool isRelated =
                    await _context
                        .Relationships
                        .AsNoTracking()
                        .AnyAsync(relationship =>
                            relationship.PatientID ==
                                selectedPatientId.Value &&

                            relationship.UserID ==
                                currentUserId);


                if (ownsPatient || isRelated)
                {
                    return await _context
                        .Patients
                        .Include(patient =>
                            patient.User)
                        .FirstOrDefaultAsync(patient =>
                            patient.PatientID ==
                                selectedPatientId.Value);
                }


                /*
                 * Selection is no longer valid.
                 */
                HttpContext.Session.Remove(
                    SelectedPatientSessionKey);
            }


            // -----------------------------------------------------
            // Default patient's own account to itself
            // -----------------------------------------------------

            Patients? ownPatient =
                await _context.Patients
                    .Include(patient =>
                        patient.User)
                    .FirstOrDefaultAsync(patient =>
                        patient.UserID ==
                            currentUserId);


            if (ownPatient != null)
            {
                HttpContext.Session.SetInt32(
                    SelectedPatientSessionKey,
                    ownPatient.PatientID);

                return ownPatient;
            }


            /*
             * Relative must choose a patient on
             * the Dashboard first.
             */
            return null;
        }


        // =========================================================
        // DISPLAY HELPERS
        // =========================================================

        public string FormatAppointmentDate(
            DateTime dateTime)
        {
            return dateTime.ToString(
                "dd MMM yyyy, hh:mm tt");
        }


        public string FormatUtcDate(
            DateTime dateTime)
        {
            return ToSingaporeTime(
                    dateTime)
                .ToString(
                    "dd MMM yyyy, hh:mm tt");
        }


        public string FormatOptionalUtcDate(
            DateTime? dateTime)
        {
            return dateTime.HasValue
                ? FormatUtcDate(
                    dateTime.Value)
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


        // =========================================================
        // RECEPTION ACKNOWLEDGEMENT CHECK
        // =========================================================
        //
        // New workflow uses DocAcknowledged.
        //
        // Older status strings are retained for compatibility
        // with existing teammate data.
        // =========================================================

        private static bool
            IsReceptionAcknowledged(
                Appointment appointment)
        {
            if (appointment.DocAcknowledged)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(
                    appointment.Status))
            {
                return false;
            }

            return
                appointment.Status.Equals(
                    "Approved",
                    StringComparison.OrdinalIgnoreCase)
                ||
                appointment.Status.Equals(
                    "Scheduled",
                    StringComparison.OrdinalIgnoreCase)
                ||
                appointment.Status.Equals(
                    "Rescheduled",
                    StringComparison.OrdinalIgnoreCase)
                ||
                appointment.Status.Equals(
                    "Confirmed",
                    StringComparison.OrdinalIgnoreCase)
                ||
                appointment.Status.Equals(
                    "Awaiting Patient",
                    StringComparison.OrdinalIgnoreCase);
        }


        // =========================================================
        // CLOSED APPOINTMENT CHECK
        // =========================================================

        private static bool IsClosedStatus(
            string? status)
        {
            if (string.IsNullOrWhiteSpace(
                    status))
            {
                return false;
            }

            return
                status.Equals(
                    "Rejected",
                    StringComparison.OrdinalIgnoreCase)
                ||
                status.Equals(
                    "Cancelled",
                    StringComparison.OrdinalIgnoreCase)
                ||
                status.Equals(
                    "Completed",
                    StringComparison.OrdinalIgnoreCase);
        }


        // =========================================================
        // TIME
        // =========================================================

        private static DateTime
            GetSingaporeNow()
        {
            return TimeZoneInfo
                .ConvertTimeFromUtc(
                    DateTime.UtcNow,
                    GetSingaporeTimeZone());
        }


        private static DateTime
            ToSingaporeTime(
                DateTime dateTime)
        {
            DateTime utcDateTime =
                dateTime.Kind switch
                {
                    DateTimeKind.Utc =>
                        dateTime,

                    DateTimeKind.Local =>
                        dateTime
                            .ToUniversalTime(),

                    _ =>
                        DateTime.SpecifyKind(
                            dateTime,
                            DateTimeKind.Utc)
                };


            return TimeZoneInfo
                .ConvertTimeFromUtc(
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


        // =========================================================
        // NEW APPOINTMENT INPUT
        // =========================================================

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


        // =========================================================
        // APPOINTMENT DISPLAY ITEM
        // =========================================================

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


            /*
             * Reception = 1
             * Patient = 0
             *
             * Patient can acknowledge.
             */
            public bool CanAcknowledge =>
                IsConfirmed &&
                !IsClosed &&
                !PatientAcknowledged;


            /*
             * Only request another date when
             * Reception has already acknowledged
             * the current appointment and there isn't
             * another pending request.
             */
            public bool CanRequestDateChange =>
                IsConfirmed &&
                !IsClosed &&
                PendingChangeRequest == null;
        }


        // =========================================================
        // CHANGE REQUEST DISPLAY ITEM
        // =========================================================

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