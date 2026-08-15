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
        public bool IsViewingOwnPatient { get; set; }
        public bool CanRequestHelp { get; set; }
        public bool HelpRequested { get; set; }

        public async Task OnGetAsync()
        {
            string? currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId)) return;

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser != null)
                CurrentUserName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();

            var ownPatient = await _context.Patients
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserID == currentUserId);

            OwnRelationCode = ownPatient?.RelationCode;

            await LoadAccessiblePatientsAsync(currentUserId);

            var patient = await GetLinkedPatientAsync(currentUserId);

            if (patient == null)
            {
                DashboardData.HasPatientRecord = false;
                return;
            }

            SelectedPatientId = patient.PatientID;
            IsViewingOwnPatient = patient.UserID == currentUserId;
            HelpRequested = patient.RequestHelp;

            CanRequestHelp =
                IsViewingOwnPatient &&
                IsHelpEligibleStatus(patient.Status);

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
                .Include(r => r.MedicationList)
                    .ThenInclude(ml => ml.Medications)
                .Where(r => r.PatientID == patient.PatientID)
                .OrderByDescending(r => r.AdmissionDateTime)
                .FirstOrDefaultAsync();

            var currentMedications = await _context.MedicationLists
                .Include(ml => ml.Medications)
                .Where(ml =>
                    ml.PatientID == patient.PatientID &&
                    (record == null || ml.MedicationListID >= record.MedicationListID))
                .OrderBy(ml =>
                    ml.Medications != null
                        ? ml.Medications.ConsumptionTime
                        : TimeOnly.MinValue)
                .Select(ml => new UserMedicationItem
                {
                    MedicationName = ml.Medications != null
                        ? ml.Medications.MedicationName
                        : "Unknown medication",
                    Dosage = ml.Dosage,
                    ConsumptionTime = ml.Medications != null
                        ? ml.Medications.ConsumptionTime
                        : null
                })
                .ToListAsync();

            var latestDoctorRequest = await _context.DoctorRequests
                .Where(dr => dr.PatientID == patient.PatientID && !dr.ByAdmin)
                .OrderByDescending(dr => dr.RequestDate)
                .FirstOrDefaultAsync();

            bool hasUnresolvedEmergency = await _context.Logs
                .AnyAsync(l =>
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
                CurrentMedications = currentMedications,
                RecordDescription = record?.Description ?? "No record description",
                AdmissionDateTime = record?.AdmissionDateTime,
                DischargeDateTime = record?.DischargeDateTime,

                HasUnresolvedEmergency = hasUnresolvedEmergency,
                AlertMessage = BuildAlertMessage(latestVitals, bracelet, hasUnresolvedEmergency),

                HasDoctorRequest = latestDoctorRequest != null,
                LatestDoctorRequestId = latestDoctorRequest?.DoctorRequestID,
                LatestDoctorRequestMessage =
                    latestDoctorRequest?.RequestMessage ?? "No doctor update available.",
                LatestDoctorReply =
                    string.IsNullOrWhiteSpace(latestDoctorRequest?.ReplyMessage)
                        ? "No reply yet."
                        : latestDoctorRequest.ReplyMessage,
                LatestDoctorRequestCompleted =
                    latestDoctorRequest?.Completed ?? false,
                LatestDoctorRequestDate =
                    latestDoctorRequest?.RequestDate
            };
        }

        // =========================================================
        // RELATION CODE
        // =========================================================

        public async Task<IActionResult> OnPostConnectRelationAsync(string? relationCode)
        {
            string? currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(currentUserId))
                return RedirectToPage("/Login");

            // CHANGED: Users only need the first 8 characters of the GUID.
            string code = (relationCode ?? "").Trim().Replace("-", "").ToUpper();

            if (code.Length != 8 || !code.All(Uri.IsHexDigit))
            {
                TempData["ErrorMessage"] = "Please enter a valid 8-character Relation Code.";
                return RedirectToPage();
            }

            // CHANGED: Keep the full GUID in database, but match using its first 8 characters.
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

            // Safety check in case two GUIDs somehow share the same first 8 characters.
            if (matches.Count > 1)
            {
                TempData["ErrorMessage"] = "This Relation Code is not unique. Please contact hospital staff.";
                return RedirectToPage();
            }

            var patient = matches[0];

            if (patient.UserID == currentUserId)
            {
                TempData["Message"] = "This patient record already belongs to your account.";
                return RedirectToPage();
            }

            bool exists = await _context.Relationships.AnyAsync(r =>
                r.PatientID == patient.PatientID &&
                r.UserID == currentUserId);

            if (exists)
            {
                TempData["Message"] = "You are already connected to this patient.";
                return RedirectToPage();
            }

            _context.Relationships.Add(new Relationships
            {
                PatientID = patient.PatientID,
                UserID = currentUserId
            });

            await _context.SaveChangesAsync();

            TempData["Message"] = "Patient connected successfully.";
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostSelectPatientAsync(int patientId)
        {
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
                return RedirectToPage("/Login");

            if (!await CanAccessPatientAsync(currentUserId, patientId))
                return Forbid();

            HttpContext.Session.SetInt32(SelectedPatientSessionKey, patientId);

            return RedirectToPage();
        }

        // =========================================================
        // ?? NURSE CALL / CANCEL NURSE CALL
        // =========================================================

        public async Task<IActionResult> OnPostRequestHelpAsync()
        {
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
                return RedirectToPage("/Login");

            var patient = await GetLinkedPatientAsync(currentUserId);

            if (patient == null || patient.UserID != currentUserId)
            {
                TempData["ErrorMessage"] =
                    "Only the patient can request nurse help.";
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
                TempData["Message"] =
                    "A nurse help request is already active.";
                return RedirectToPage();
            }

            patient.RequestHelp = true;
            await _context.SaveChangesAsync();

            TempData["Message"] = "Nurse help requested successfully.";
            return RedirectToPage();
        }

        // ADDED: Patient can cancel their own active nurse call.
        public async Task<IActionResult> OnPostCancelHelpAsync()
        {
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
                return RedirectToPage("/Login");

            var patient = await GetLinkedPatientAsync(currentUserId);

            if (patient == null || patient.UserID != currentUserId)
            {
                TempData["ErrorMessage"] =
                    "Only the patient can cancel this nurse call.";
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
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(currentUserId))
                return RedirectToPage("/Login");

            var patient = await GetLinkedPatientAsync(currentUserId);
            if (patient == null) return RedirectToPage();

            var doctorUpdate = await _context.DoctorRequests
                .FirstOrDefaultAsync(dr =>
                    dr.DoctorRequestID == doctorRequestId &&
                    dr.PatientID == patient.PatientID);

            if (doctorUpdate == null) return RedirectToPage();

            doctorUpdate.Completed = true;
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        // =========================================================
        // ACCESSIBLE PATIENTS
        // =========================================================

        private async Task LoadAccessiblePatientsAsync(string currentUserId)
        {
            var relatedIds = await _context.Relationships
                .AsNoTracking()
                .Where(r => r.UserID == currentUserId)
                .Select(r => r.PatientID)
                .ToListAsync();

            var patients = await _context.Patients
                .AsNoTracking()
                .Include(p => p.User)
                .Where(p =>
                    p.UserID == currentUserId ||
                    relatedIds.Contains(p.PatientID))
                .OrderBy(p => p.PatientID)
                .ToListAsync();

            var patientIds = patients.Select(p => p.PatientID).ToList();

            if (patientIds.Count == 0)
            {
                Patients = new List<PatientListItem>();
                return;
            }

            var medicationLists = await _context.MedicationLists
                .AsNoTracking()
                .Include(m => m.Medications)
                .Where(m => patientIds.Contains(m.PatientID))
                .ToListAsync();

            var records = await _context.Records
                .AsNoTracking()
                .Where(r => patientIds.Contains(r.PatientID))
                .ToListAsync();

            Patients = patients.Select(p =>
            {
                var latestRecord = records
                    .Where(r => r.PatientID == p.PatientID)
                    .OrderByDescending(r => r.AdmissionDateTime)
                    .FirstOrDefault();

                var patientMeds = medicationLists
                    .Where(m =>
                        m.PatientID == p.PatientID &&
                        (latestRecord == null ||
                         m.MedicationListID >= latestRecord.MedicationListID))
                    .ToList();

                string medSummary = patientMeds.Count == 0
                    ? "None"
                    : string.Join(", ", patientMeds.Select(m =>
                        $"{m.Medications?.MedicationName ?? "Unknown"} ({m.Dosage})"));

                return new PatientListItem
                {
                    PatientId = p.PatientID,
                    PatientName = p.User != null
                        ? $"{p.User.FirstName} {p.User.LastName}"
                        : $"Patient #{p.PatientID}",
                    Status = p.Status,
                    MedicationsSummary = medSummary,
                    AdmissionDateTime = latestRecord?.AdmissionDateTime,
                    DischargeDateTime = latestRecord?.DischargeDateTime,

                    // Nurse call status shown in patient list.
                    RequestHelp = p.RequestHelp
                };
            }).ToList();
        }

        // =========================================================
        // SELECTED PATIENT / SECURITY
        // =========================================================

        private async Task<Patients?> GetLinkedPatientAsync(string currentUserId)
        {
            int? selectedPatientId =
                HttpContext.Session.GetInt32(SelectedPatientSessionKey);

            if (selectedPatientId.HasValue)
            {
                if (await CanAccessPatientAsync(currentUserId, selectedPatientId.Value))
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

        private async Task<bool> CanAccessPatientAsync(string currentUserId, int patientId)
        {
            bool ownsPatient = await _context.Patients
                .AsNoTracking()
                .AnyAsync(p =>
                    p.PatientID == patientId &&
                    p.UserID == currentUserId);

            if (ownsPatient) return true;

            return await _context.Relationships
                .AsNoTracking()
                .AnyAsync(r =>
                    r.PatientID == patientId &&
                    r.UserID == currentUserId);
        }

        private static bool IsHelpEligibleStatus(string? status)
        {
            return
                string.Equals(status, "Admitted", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(status, "Observed", StringComparison.OrdinalIgnoreCase);
        }

        // =========================================================
        // DASHBOARD ALERT
        // =========================================================

        private static string BuildAlertMessage(
            Vitals? latestVitals,
            Bracelet? bracelet,
            bool hasUnresolvedEmergency)
        {
            if (hasUnresolvedEmergency)
                return "There is an unresolved emergency log. Please review your activity log.";

            if (bracelet?.Battery != null && bracelet.Battery <= 20)
                return "Bracelet battery is low. Please inform hospital staff.";

            if (latestVitals?.HeartRate != null &&
                (latestVitals.HeartRate < 60 || latestVitals.HeartRate > 100))
                return "Latest heart rate is outside the normal range.";

            return "No urgent alerts. Latest readings appear normal.";
        }

        // =========================================================
        // LIVE VITALS
        // =========================================================

        public async Task<IActionResult> OnGetLatestVitalsAsync()
        {
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
                return new JsonResult(new
                {
                    success = false,
                    message = "User is not logged in."
                });

            var patient = await GetLinkedPatientAsync(currentUserId);

            if (patient == null)
                return new JsonResult(new
                {
                    success = false,
                    message = "No patient has been selected."
                });

            var latestVitals = await _context.Vitals
                .AsNoTracking()
                .Where(v => v.PatientID == patient.PatientID)
                .OrderByDescending(v => v.RecordedAt)
                .FirstOrDefaultAsync();

            var bracelet = await _context.BraceletRelations
                .AsNoTracking()
                .Where(br => br.PatientID == patient.PatientID)
                .Select(br => br.Bracelet)
                .FirstOrDefaultAsync();

            bool hasEmergency = await _context.Logs
                .AsNoTracking()
                .AnyAsync(l =>
                    l.PatientID == patient.PatientID &&
                    l.Emergency &&
                    !l.Resolved);

            return new JsonResult(new
            {
                success = true,

                heartRate =
                    (latestVitals?.HeartRate ?? bracelet?.HeartRate)?.ToString("0") ?? "N/A",

                respiration =
                    (latestVitals?.RespiratoryRate ?? bracelet?.RespiratoryRate)?.ToString("0") ?? "N/A",

                systolicBloodPressure =
                    (latestVitals?.SystolicBloodPressure ?? bracelet?.SystolicBloodPressure)?.ToString("0") ?? "N/A",

                diastolicBloodPressure =
                    (latestVitals?.DiastolicBloodPressure ?? bracelet?.DiastolicBloodPressure)?.ToString("0") ?? "N/A",

                movement =
                    bracelet?.Movement?.ToString("0.0") ?? "N/A",

                battery =
                    bracelet?.Battery?.ToString("0.0") ?? "N/A",

                updatedAt =
                    latestVitals == null
                        ? "No reading"
                        : ToSingaporeTime(latestVitals.RecordedAt)
                            .ToString("dd MMM yyyy, hh:mm tt"),

                hasUnresolvedEmergency = hasEmergency,

                alertMessage =
                    BuildAlertMessage(latestVitals, bracelet, hasEmergency)
            });
        }

        private static DateTime ToSingaporeTime(DateTime recordedAt)
        {
            DateTime utcTime = recordedAt.Kind switch
            {
                DateTimeKind.Utc => recordedAt,
                DateTimeKind.Local => recordedAt.ToUniversalTime(),
                _ => DateTime.SpecifyKind(recordedAt, DateTimeKind.Utc)
            };

            try
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById(
                    "Singapore Standard Time");

                return TimeZoneInfo.ConvertTimeFromUtc(utcTime, zone);
            }
            catch (TimeZoneNotFoundException)
            {
                var zone = TimeZoneInfo.FindSystemTimeZoneById(
                    "Asia/Singapore");

                return TimeZoneInfo.ConvertTimeFromUtc(utcTime, zone);
            }
        }
    }
}