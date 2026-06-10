using INFP_Proj.Data;
using INFP_Proj.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using static INFP_Proj.Models.UserDashboardViewModel;

namespace INFP_Proj.Pages.User
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public DashboardModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public UserDashboardViewModel DashboardData { get; set; } = new();

        public async Task OnGetAsync()
        {
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return;
            }

            Patients? patient = await GetLinkedPatientAsync(currentUserId);

            if (patient == null)
            {
                DashboardData.HasPatientRecord = false;
                return;
            }

            DashboardData.HasPatientRecord = true;

            var bed = await _context.Beds
                .Include(b => b.Wards)
                .FirstOrDefaultAsync(b => b.PatientID == patient.PatientID);

            var braceletRelation = await _context.BraceletRelations
                .Include(br => br.Bracelet)
                .FirstOrDefaultAsync(br => br.PatientID == patient.PatientID);

            var bracelet = braceletRelation?.Bracelet;

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

            // Only show medications for the current admission (medications attached to,
            // or added after, the latest record's medication list).
            var currentMedications = await _context.MedicationLists
                .Include(ml => ml.Medications)
                .Where(ml => ml.PatientID == patient.PatientID
                    && (record == null || ml.MedicationListID >= record.MedicationListID))
                .OrderBy(ml => ml.Medications != null ? ml.Medications.ConsumptionTime : TimeOnly.MinValue)
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
                .Where(dr => dr.PatientID == patient.PatientID)
                .OrderByDescending(dr => dr.RequestDate)
                .FirstOrDefaultAsync();

            var logUserIds = new List<string> { currentUserId };

            if (!string.IsNullOrEmpty(patient.UserID) && !logUserIds.Contains(patient.UserID))
            {
                logUserIds.Add(patient.UserID);
            }

            bool hasUnresolvedEmergency = await _context.Logs
                .AnyAsync(l => logUserIds.Contains(l.UserID) && l.Emergency && !l.Resolved);

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
                BedTemperature = bed?.Temperature ?? record?.Beds?.Temperature,
                Weight = bed?.Weight ?? record?.Beds?.Weight,

                HeartRate = bracelet?.HeartRate ?? latestVitals?.HeartRate,
                BloodPressure = bracelet?.BloodPressure ?? latestVitals?.BloodPressure,
                Respiration = bracelet?.Respiration ?? latestVitals?.RespiratoryRate,
                Temperature = latestVitals?.Temperature,
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
                LatestDoctorRequestMessage = latestDoctorRequest?.RequestMessage ?? "No doctor update available.",
                LatestDoctorReply = string.IsNullOrWhiteSpace(latestDoctorRequest?.ReplyMessage)
                    ? "No reply yet."
                    : latestDoctorRequest.ReplyMessage,
                LatestDoctorRequestCompleted = latestDoctorRequest?.Completed ?? false,
                LatestDoctorRequestDate = latestDoctorRequest?.RequestDate
            };
        }

        public async Task<IActionResult> OnPostAcknowledgeDoctorUpdateAsync(int doctorRequestId)
        {
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Login");
            }

            Patients? patient = await GetLinkedPatientAsync(currentUserId);

            if (patient == null)
            {
                return RedirectToPage();
            }

            var doctorUpdate = await _context.DoctorRequests
                .FirstOrDefaultAsync(dr =>
                    dr.DoctorRequestID == doctorRequestId &&
                    dr.PatientID == patient.PatientID);

            if (doctorUpdate == null)
            {
                return RedirectToPage();
            }

            doctorUpdate.Completed = true;

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        private async Task<Patients?> GetLinkedPatientAsync(string currentUserId)
        {
            // Case 1: logged-in user is the patient directly
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserID == currentUserId);

            if (patient != null)
            {
                return patient;
            }

            // Case 2: logged-in user is a family/caregiver linked through Relationships
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

        private static string BuildAlertMessage(Vitals? latestVitals, Bracelet? bracelet, bool hasUnresolvedEmergency)
        {
            if (hasUnresolvedEmergency)
            {
                return "There is an unresolved emergency log. Please review your activity log.";
            }

            if (bracelet?.Battery != null && bracelet.Battery <= 20)
            {
                return "Bracelet battery is low. Please inform hospital staff.";
            }

            if (latestVitals?.Temperature != null && latestVitals.Temperature >= 38)
            {
                return "Latest temperature is above the normal range.";
            }

            if (latestVitals?.HeartRate != null && (latestVitals.HeartRate < 60 || latestVitals.HeartRate > 100))
            {
                return "Latest heart rate is outside the normal range.";
            }

            return "No urgent alerts. Latest readings appear normal.";
        }

        public async Task<IActionResult> OnGetLatestVitalsAsync()
        {
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(currentUserId))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "User is not logged in."
                });
            }

            Patients? patient = await GetLinkedPatientAsync(currentUserId);

            if (patient == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "No patient record linked to this account."
                });
            }

            var braceletRelation = await _context.BraceletRelations
                .Include(br => br.Bracelet)
                .FirstOrDefaultAsync(br => br.PatientID == patient.PatientID);

            if (braceletRelation?.Bracelet == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "No bracelet linked to this patient."
                });
            }

            var bracelet = braceletRelation.Bracelet;
            var random = new Random();

            float heartRate = random.Next(65, 101);
            float bloodPressure = random.Next(110, 141);
            float respiration = random.Next(14, 25);
            float temperature = (float)Math.Round(36.1 + random.NextDouble() * 1.7, 1);
            float movement = (float)Math.Round(random.NextDouble() * 2.0, 1);

            float battery = bracelet.Battery ?? 100;
            battery = Math.Max(0, battery - 0.1f);

            bracelet.HeartRate = heartRate;
            bracelet.BloodPressure = bloodPressure;
            bracelet.Respiration = respiration;
            bracelet.Movement = movement;
            bracelet.Battery = battery;

            var newVitals = new Vitals
            {
                PatientID = patient.PatientID,
                HeartRate = heartRate,
                BloodPressure = bloodPressure,
                RespiratoryRate = respiration,
                Temperature = temperature,
                RecordedAt = DateTime.UtcNow
            };

            _context.Vitals.Add(newVitals);
            await _context.SaveChangesAsync();

            bool hasUnresolvedEmergency = await _context.Logs
                .AnyAsync(l =>
                    (l.UserID == currentUserId || l.UserID == patient.UserID)
                    && l.Emergency
                    && !l.Resolved);

            string alertMessage = BuildAlertMessage(newVitals, bracelet, hasUnresolvedEmergency);

            return new JsonResult(new
            {
                success = true,
                heartRate,
                bloodPressure,
                respiration,
                temperature,
                movement,
                battery = Math.Round(battery, 1),
                updatedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                hasUnresolvedEmergency,
                alertMessage
            });
        }
    }
}