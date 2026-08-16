using INFP_Proj.Data;
using INFP_Proj.Models;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using static INFP_Proj.ViewModel.UserDashboardViewModel;

namespace INFP_Proj.Pages.User
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private const string SelectedPatientSessionKey = "SelectedPatientId";
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public UserDashboardViewModel DashboardData { get; set; } = new();
        public IList<PatientListItem> Patients { get; set; } = new List<PatientListItem>();

        public string CurrentUserName { get; set; } = "User";
        public int? SelectedPatientId { get; set; }
        public Guid? OwnRelationCode { get; set; }
        public string? OwnRelationCodeShort =>
            OwnRelationCode?.ToString("N").Substring(0, 8).ToUpperInvariant();

        public bool IsViewingOwnPatient { get; set; }
        public bool CanRequestHelp { get; set; }
        public bool HelpRequested { get; set; }

        // ADDED: Upcoming appointment
        public NextAppointmentItem? NextAppointment { get; set; }

        // =========================================================
        // LOAD DASHBOARD
        // =========================================================

        public async Task OnGetAsync()
        {
            string? userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId)) return;

            var user = await _userManager.GetUserAsync(User);
            if (user != null)
                CurrentUserName = $"{user.FirstName} {user.LastName}".Trim();

            var ownPatient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserID == userId);

            OwnRelationCode = ownPatient?.RelationCode;

            await LoadAccessiblePatientsAsync(userId);

            var patient = await GetLinkedPatientAsync(userId);

            if (patient == null)
            {
                DashboardData.HasPatientRecord = false;
                return;
            }

            SelectedPatientId = patient.PatientID;
            IsViewingOwnPatient = patient.UserID == userId;
            HelpRequested = patient.RequestHelp;
            CanRequestHelp = IsViewingOwnPatient && IsHelpEligibleStatus(patient.Status);

            await LoadNextAppointmentAsync(patient.PatientID);

            var bed = await _context.Beds
                .Include(b => b.Wards)
                .FirstOrDefaultAsync(b => b.PatientID == patient.PatientID);

            var bracelet = await _context.BraceletRelations
                .Include(br => br.Bracelet)
                .Where(br => br.PatientID == patient.PatientID)
                .Select(br => br.Bracelet)
                .FirstOrDefaultAsync();

            var latestVitals = await _context.Vitals
                .Where(v => v.PatientID == patient.PatientID)
                .OrderByDescending(v => v.RecordedAt)
                .FirstOrDefaultAsync();

            var record = await _context.Records
                .Include(r => r.Hospitals)
                .Include(r => r.Wards)
                .Include(r => r.Beds)
                .Include(r => r.Diagnoses)
                .Where(r => r.PatientID == patient.PatientID)
                .OrderByDescending(r => r.AdmissionDateTime)
                .FirstOrDefaultAsync();

            var medications = await _context.MedicationLists
                .Include(m => m.Medications)
                .Where(m =>
                    m.PatientID == patient.PatientID &&
                    record != null &&
                    m.RecordID == record.RecordID)
                .OrderBy(m => m.Medications != null
                    ? m.Medications.ConsumptionTime
                    : TimeOnly.MinValue)
                .Select(m => new UserMedicationItem
                {
                    MedicationName = m.Medications != null
                        ? m.Medications.MedicationName
                        : "Unknown medication",

                    Dosage = m.Dosage,

                    ConsumptionTime = m.Medications != null
                        ? m.Medications.ConsumptionTime
                        : null
                })
                .ToListAsync();

            var doctorRequest = await _context.DoctorRequests
                .Where(d => d.PatientID == patient.PatientID && !d.ByAdmin)
                .OrderByDescending(d => d.RequestDate)
                .FirstOrDefaultAsync();

            bool hasEmergency = await _context.Logs.AnyAsync(l =>
                l.PatientID == patient.PatientID &&
                l.Emergency &&
                !l.Resolved);

            DashboardData = new UserDashboardViewModel
            {
                HasPatientRecord = true,
                PatientId = patient.PatientID,
                PatientName = patient.User != null
                    ? $"{patient.User.FirstName} {patient.User.LastName}"
                    : $"Patient #{patient.PatientID}",

                PatientStatus = patient.Status,
                PatientNotes = string.IsNullOrWhiteSpace(patient.Notes)
                    ? "No notes recorded."
                    : patient.Notes,

                HospitalName = record?.Hospitals?.HospitalName ?? "Not assigned",
                HospitalAddress = record?.Hospitals?.HospitalAddress ?? "Not assigned",
                WardName = bed?.Wards?.WardName ?? record?.Wards?.WardName ?? "Not assigned",
                Room = bed?.Room ?? record?.Beds?.Room ?? "Not assigned",
                Floor = bed?.Floor ?? record?.Beds?.Floor ?? "Not assigned",
                Sector = bed?.Sector ?? record?.Beds?.Sector ?? "Not assigned",
                BedLocation = bed?.Location ?? record?.Beds?.Location ?? "Not assigned",
                Weight = bed?.Weight ?? record?.Beds?.Weight,

                HeartRate = bracelet?.HeartRate ?? latestVitals?.HeartRate,
                SystolicBloodPressure = bracelet?.SystolicBloodPressure ?? latestVitals?.SystolicBloodPressure,
                DiastolicBloodPressure = bracelet?.DiastolicBloodPressure ?? latestVitals?.DiastolicBloodPressure,
                RespiratoryRate = bracelet?.RespiratoryRate ?? latestVitals?.RespiratoryRate,
                LatestVitalsRecordedAt = latestVitals?.RecordedAt,

                BraceletBattery = bracelet?.Battery,
                Movement = bracelet?.Movement,
                BraceletLocation = bracelet?.Location ?? "Unknown",

                Diagnosis = record?.Diagnoses?.DiagnosisName ?? "No diagnosis recorded",
                CurrentMedications = medications,
                RecordDescription = record?.Description ?? "No record description",
                AdmissionDateTime = record?.AdmissionDateTime,
                DischargeDateTime = record?.DischargeDateTime,

                HasUnresolvedEmergency = hasEmergency,
                AlertMessage = BuildAlertMessage(latestVitals, bracelet, hasEmergency),

                HasDoctorRequest = doctorRequest != null,
                LatestDoctorRequestId = doctorRequest?.DoctorRequestID,
                LatestDoctorRequestMessage = doctorRequest?.RequestMessage ?? "No doctor update available.",
                LatestDoctorReply = string.IsNullOrWhiteSpace(doctorRequest?.ReplyMessage)
                    ? "No reply yet."
                    : doctorRequest.ReplyMessage,
                LatestDoctorRequestCompleted = doctorRequest?.Completed ?? false,
                LatestDoctorRequestDate = doctorRequest?.RequestDate
            };
        }

        // =========================================================
        // NEXT APPOINTMENT
        // =========================================================

        private async Task LoadNextAppointmentAsync(int patientId)
        {
            DateTime now = GetSingaporeNow();

            var appointment = await _context.Appointments
                .AsNoTracking()
                .Where(a =>
                    a.PatientID == patientId &&
                    a.DateTime >= now &&
                    a.Status != "Rejected" &&
                    a.Status != "Cancelled" &&
                    a.Status != "Completed")
                .OrderBy(a => a.DateTime)
                .FirstOrDefaultAsync();

            if (appointment == null) return;

            string doctorName = "Not assigned";

            if (!string.IsNullOrWhiteSpace(appointment.DoctorID))
            {
                var doctor = await _context.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == appointment.DoctorID);

                if (doctor != null)
                    doctorName = $"Dr. {doctor.FirstName} {doctor.LastName}";
            }

            NextAppointment = new NextAppointmentItem
            {
                AppointmentRequestID = appointment.AppointmentRequestID,
                DateTime = appointment.DateTime,
                DoctorName = doctorName,
                Reason = appointment.Reason,
                Urgency = appointment.Urgency,
                Status = appointment.Status,
                TimeUntil = BuildTimeUntil(appointment.DateTime, now)
            };
        }

        private static string BuildTimeUntil(DateTime date, DateTime now)
        {
            int days = (date.Date - now.Date).Days;

            if (days == 0) return "Today";
            if (days == 1) return "Tomorrow";
            return $"In {days} days";
        }

        // =========================================================
        // RELATION CODE
        // =========================================================

        public async Task<IActionResult> OnPostConnectRelationAsync(string? relationCode)
        {
            string? userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return RedirectToPage("/Login");

            string code = (relationCode ?? "")
                .Trim()
                .Replace("-", "")
                .ToUpperInvariant();

            if (code.Length != 8 || !code.All(Uri.IsHexDigit))
            {
                TempData["ErrorMessage"] = "Please enter a valid 8-character Relation Code.";
                return RedirectToPage();
            }

            var patients = await _context.Patients
                .Select(p => new { p.PatientID, p.UserID, p.RelationCode })
                .ToListAsync();

            var matches = patients
                .Where(p => p.RelationCode.ToString("N")
                    .StartsWith(code, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                TempData["ErrorMessage"] = "No patient was found with this Relation Code.";
                return RedirectToPage();
            }

            if (matches.Count > 1)
            {
                TempData["ErrorMessage"] = "This Relation Code is not unique. Please contact hospital staff.";
                return RedirectToPage();
            }

            var patient = matches[0];

            if (patient.UserID == userId)
            {
                TempData["Message"] = "This patient record already belongs to your account.";
                return RedirectToPage();
            }

            bool exists = await _context.Relationships.AnyAsync(r =>
                r.PatientID == patient.PatientID &&
                r.UserID == userId);

            if (exists)
            {
                TempData["Message"] = "You are already connected to this patient.";
                return RedirectToPage();
            }

            _context.Relationships.Add(new Relationships
            {
                PatientID = patient.PatientID,
                UserID = userId
            });

            await _context.SaveChangesAsync();

            TempData["Message"] = "Patient connected successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSelectPatientAsync(int patientId)
        {
            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return RedirectToPage("/Login");

            if (!await CanAccessPatientAsync(userId, patientId))
                return Forbid();

            HttpContext.Session.SetInt32(SelectedPatientSessionKey, patientId);
            return RedirectToPage();
        }

        // =========================================================
        // NURSE CALL
        // =========================================================

        public async Task<IActionResult> OnPostRequestHelpAsync()
        {
            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return RedirectToPage("/Login");

            var patient = await GetLinkedPatientAsync(userId);

            if (patient == null || patient.UserID != userId)
            {
                TempData["ErrorMessage"] = "Only the patient can request nurse help.";
                return RedirectToPage();
            }

            if (!IsHelpEligibleStatus(patient.Status))
            {
                TempData["ErrorMessage"] =
                    "Nurse help can only be requested while the patient is Admitted or Observed.";
                return RedirectToPage();
            }

            if (patient.RequestHelp)
            {
                TempData["Message"] = "A nurse help request is already active.";
                return RedirectToPage();
            }

            patient.RequestHelp = true;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Nurse help requested successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostCancelHelpAsync()
        {
            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return RedirectToPage("/Login");

            var patient = await GetLinkedPatientAsync(userId);

            if (patient == null || patient.UserID != userId)
            {
                TempData["ErrorMessage"] = "Only the patient can cancel this nurse call.";
                return RedirectToPage();
            }

            if (!patient.RequestHelp)
            {
                TempData["Message"] = "There is no active nurse call.";
                return RedirectToPage();
            }

            patient.RequestHelp = false;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Nurse call cancelled.";
            return RedirectToPage();
        }

        // =========================================================
        // DOCTOR UPDATE
        // =========================================================

        public async Task<IActionResult> OnPostAcknowledgeDoctorUpdateAsync(int doctorRequestId)
        {
            string? userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
                return RedirectToPage("/Login");

            var patient = await GetLinkedPatientAsync(userId);
            if (patient == null) return RedirectToPage();

            var update = await _context.DoctorRequests.FirstOrDefaultAsync(d =>
                d.DoctorRequestID == doctorRequestId &&
                d.PatientID == patient.PatientID);

            if (update == null) return RedirectToPage();

            update.Completed = true;
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        // =========================================================
        // ACCESSIBLE PATIENTS
        // =========================================================

        private async Task LoadAccessiblePatientsAsync(string userId)
        {
            var relatedIds = await _context.Relationships
                .AsNoTracking()
                .Where(r => r.UserID == userId)
                .Select(r => r.PatientID)
                .ToListAsync();

            var patients = await _context.Patients
                .AsNoTracking()
                .Include(p => p.User)
                .Where(p => p.UserID == userId || relatedIds.Contains(p.PatientID))
                .OrderBy(p => p.PatientID)
                .ToListAsync();

            var ids = patients.Select(p => p.PatientID).ToList();

            if (ids.Count == 0)
            {
                Patients = new List<PatientListItem>();
                return;
            }

            var meds = await _context.MedicationLists
                .AsNoTracking()
                .Include(m => m.Medications)
                .Where(m => ids.Contains(m.PatientID))
                .ToListAsync();

            var records = await _context.Records
                .AsNoTracking()
                .Where(r => ids.Contains(r.PatientID))
                .ToListAsync();

            Patients = patients.Select(p =>
            {
                var record = records
                    .Where(r => r.PatientID == p.PatientID)
                    .OrderByDescending(r => r.AdmissionDateTime)
                    .FirstOrDefault();

                var patientMeds = meds
                    .Where(m =>
                        m.PatientID == p.PatientID &&
                        record != null &&
                        m.RecordID == record.RecordID)
                    .ToList();

                return new PatientListItem
                {
                    PatientId = p.PatientID,
                    PatientName = p.User != null
                        ? $"{p.User.FirstName} {p.User.LastName}"
                        : $"Patient #{p.PatientID}",

                    Status = p.Status,
                    MedicationsSummary = patientMeds.Count == 0
                        ? "None"
                        : string.Join(", ", patientMeds.Select(m =>
                            $"{m.Medications?.MedicationName ?? "Unknown"} ({m.Dosage})")),

                    AdmissionDateTime = record?.AdmissionDateTime,
                    DischargeDateTime = record?.DischargeDateTime,
                    RequestHelp = p.RequestHelp
                };
            }).ToList();
        }

        // =========================================================
        // PATIENT ACCESS
        // =========================================================

        private async Task<Patients?> GetLinkedPatientAsync(string userId)
        {
            int? selectedId =
                HttpContext.Session.GetInt32(SelectedPatientSessionKey);

            if (selectedId.HasValue)
            {
                if (await CanAccessPatientAsync(userId, selectedId.Value))
                {
                    return await _context.Patients
                        .Include(p => p.User)
                        .FirstOrDefaultAsync(p => p.PatientID == selectedId.Value);
                }

                HttpContext.Session.Remove(SelectedPatientSessionKey);
            }

            var ownPatient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserID == userId);

            if (ownPatient != null)
            {
                HttpContext.Session.SetInt32(
                    SelectedPatientSessionKey,
                    ownPatient.PatientID);
            }

            return ownPatient;
        }

        private async Task<bool> CanAccessPatientAsync(string userId, int patientId)
        {
            if (await _context.Patients.AsNoTracking().AnyAsync(p =>
                p.PatientID == patientId &&
                p.UserID == userId))
                return true;

            return await _context.Relationships.AsNoTracking().AnyAsync(r =>
                r.PatientID == patientId &&
                r.UserID == userId);
        }

        private static bool IsHelpEligibleStatus(string? status) =>
            string.Equals(status, "Admitted", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(status, "Observed", StringComparison.OrdinalIgnoreCase);

        // =========================================================
        // ALERTS + LIVE VITALS
        // =========================================================

        private static string BuildAlertMessage(
            Vitals? vitals,
            Bracelet? bracelet,
            bool emergency)
        {
            if (emergency)
                return "There is an unresolved emergency log. Please review your activity log.";

            if (bracelet?.Battery != null && bracelet.Battery <= 20)
                return "Bracelet battery is low. Please inform hospital staff.";

            if (vitals?.HeartRate != null &&
                (vitals.HeartRate < 60 || vitals.HeartRate > 100))
                return "Latest heart rate is outside the normal range.";

            return "No urgent alerts. Latest readings appear normal.";
        }

        public async Task<IActionResult> OnGetLatestVitalsAsync()
        {
            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(userId))
                return new JsonResult(new { success = false });

            var patient = await GetLinkedPatientAsync(userId);

            if (patient == null)
                return new JsonResult(new { success = false });

            var vitals = await _context.Vitals
                .AsNoTracking()
                .Where(v => v.PatientID == patient.PatientID)
                .OrderByDescending(v => v.RecordedAt)
                .FirstOrDefaultAsync();

            var bracelet = await _context.BraceletRelations
                .AsNoTracking()
                .Where(b => b.PatientID == patient.PatientID)
                .Select(b => b.Bracelet)
                .FirstOrDefaultAsync();

            bool emergency = await _context.Logs
                .AsNoTracking()
                .AnyAsync(l =>
                    l.PatientID == patient.PatientID &&
                    l.Emergency &&
                    !l.Resolved);

            return new JsonResult(new
            {
                success = true,
                heartRate = (vitals?.HeartRate ?? bracelet?.HeartRate)?.ToString("0") ?? "N/A",
                respiration = (vitals?.RespiratoryRate ?? bracelet?.RespiratoryRate)?.ToString("0") ?? "N/A",
                systolicBloodPressure = (vitals?.SystolicBloodPressure ?? bracelet?.SystolicBloodPressure)?.ToString("0") ?? "N/A",
                diastolicBloodPressure = (vitals?.DiastolicBloodPressure ?? bracelet?.DiastolicBloodPressure)?.ToString("0") ?? "N/A",
                movement = bracelet?.Movement?.ToString("0.0") ?? "N/A",
                battery = bracelet?.Battery?.ToString("0.0") ?? "N/A",
                updatedAt = vitals == null
                    ? "No reading"
                    : ToSingaporeTime(vitals.RecordedAt).ToString("dd MMM yyyy, hh:mm tt"),

                hasUnresolvedEmergency = emergency,
                alertMessage = BuildAlertMessage(vitals, bracelet, emergency)
            });
        }

        // =========================================================
        // TIME
        // =========================================================

        private static DateTime GetSingaporeNow() =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, GetSingaporeTimeZone());

        private static DateTime ToSingaporeTime(DateTime date)
        {
            DateTime utc = date.Kind switch
            {
                DateTimeKind.Utc => date,
                DateTimeKind.Local => date.ToUniversalTime(),
                _ => DateTime.SpecifyKind(date, DateTimeKind.Utc)
            };

            return TimeZoneInfo.ConvertTimeFromUtc(utc, GetSingaporeTimeZone());
        }

        private static TimeZoneInfo GetSingaporeTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                return TimeZoneInfo.FindSystemTimeZoneById("Asia/Singapore");
            }
        }

        // =========================================================
        // VIEW MODEL
        // =========================================================

        public sealed class NextAppointmentItem
        {
            public int AppointmentRequestID { get; set; }
            public DateTime DateTime { get; set; }
            public string DoctorName { get; set; } = "Not assigned";
            public string Reason { get; set; } = string.Empty;
            public string Urgency { get; set; } = "Normal";
            public string Status { get; set; } = string.Empty;
            public string TimeUntil { get; set; } = string.Empty;
        }
    }
}