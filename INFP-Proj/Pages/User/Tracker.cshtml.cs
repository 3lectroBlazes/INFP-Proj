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
    public class TrackerModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public TrackerModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public VitalsChartViewModel ChartData { get; set; } = new();
        public bool HasPatientRecord { get; set; }

        public float? LatestHeartRate { get; set; }
        public float? LatestBloodPressure { get; set; }
        public float? LatestRespiration { get; set; }
        public float? LatestTemperature { get; set; }
        public DateTime? LatestUpdatedAt { get; set; }

        public string HeartRateStatus { get; set; } = "No data";
        public string BloodPressureStatus { get; set; } = "No data";
        public string RespirationStatus { get; set; } = "No data";
        public string TemperatureStatus { get; set; } = "No data";
        public bool HasAttention { get; set; }

        public async Task OnGetAsync()
        {
            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            Patients? patient = await GetLinkedPatientAsync(userId);

            if (patient == null)
            {
                HasPatientRecord = false;
                return;
            }

            HasPatientRecord = true;

            await LoadLatestVitalsAsync(patient.PatientID);
            await LoadHourlyAverageChartAsync(patient);
        }

        public async Task<IActionResult> OnGetLatestVitalsAsync()
        {
            string? userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "User is not logged in."
                });
            }

            Patients? patient = await GetLinkedPatientAsync(userId);

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

            string heartRateStatus = GetHeartRateStatus(heartRate);
            string bloodPressureStatus = GetBloodPressureStatus(bloodPressure);
            string respirationStatus = GetRespirationStatus(respiration);
            string temperatureStatus = GetTemperatureStatus(temperature);

            bool hasAttention =
                heartRateStatus == "Attention" ||
                bloodPressureStatus == "Attention" ||
                respirationStatus == "Attention" ||
                temperatureStatus == "Attention";

            return new JsonResult(new
            {
                success = true,
                heartRate = heartRate.ToString("0"),
                bloodPressure = bloodPressure.ToString("0"),
                respiration = respiration.ToString("0"),
                temperature = temperature.ToString("0.0"),
                updatedAt = ToSingaporeTime(newVitals.RecordedAt).ToString("yyyy-MM-dd HH:mm:ss"),

                heartRateStatus,
                bloodPressureStatus,
                respirationStatus,
                temperatureStatus,
                hasAttention
            });
        }

        private async Task LoadLatestVitalsAsync(int patientId)
        {
            var braceletRelation = await _context.BraceletRelations
                .Include(br => br.Bracelet)
                .FirstOrDefaultAsync(br => br.PatientID == patientId);

            var latestVitals = await _context.Vitals
                .Where(v => v.PatientID == patientId)
                .OrderByDescending(v => v.RecordedAt)
                .FirstOrDefaultAsync();

            var bracelet = braceletRelation?.Bracelet;

            LatestHeartRate = bracelet?.HeartRate ?? latestVitals?.HeartRate;
            LatestBloodPressure = bracelet?.BloodPressure ?? latestVitals?.BloodPressure;
            LatestRespiration = bracelet?.Respiration ?? latestVitals?.RespiratoryRate;
            LatestTemperature = latestVitals?.Temperature;
            LatestUpdatedAt = latestVitals?.RecordedAt;

            HeartRateStatus = GetHeartRateStatus(LatestHeartRate);
            BloodPressureStatus = GetBloodPressureStatus(LatestBloodPressure);
            RespirationStatus = GetRespirationStatus(LatestRespiration);
            TemperatureStatus = GetTemperatureStatus(LatestTemperature);

            HasAttention =
                HeartRateStatus == "Attention" ||
                BloodPressureStatus == "Attention" ||
                RespirationStatus == "Attention" ||
                TemperatureStatus == "Attention";
        }

        private async Task LoadHourlyAverageChartAsync(Patients patient)
        {
            var vitals = await _context.Vitals
                .Where(v => v.PatientID == patient.PatientID)
                .OrderBy(v => v.RecordedAt)
                .ToListAsync();

            var hourlyVitals = vitals
                .GroupBy(v => GetUtcHour(v.RecordedAt))
                .OrderBy(g => g.Key)
                .Select(g => new
                {
                    HourUtc = g.Key,
                    HeartRate = Average(g.Select(v => v.HeartRate)),
                    RespiratoryRate = Average(g.Select(v => v.RespiratoryRate)),
                    BloodPressure = Average(g.Select(v => v.BloodPressure)),
                    Temperature = Average(g.Select(v => v.Temperature))
                })
                .ToList();

            ChartData = new VitalsChartViewModel
            {
                PatientId = patient.PatientID,
                PatientName = patient.User != null
                    ? $"{patient.User.FirstName} {patient.User.LastName}"
                    : $"Patient #{patient.PatientID}",

                Labels = hourlyVitals
                    .Select(v => ToSingaporeTime(v.HourUtc).ToString("MMM d, HH:00"))
                    .ToList(),

                HeartRate = hourlyVitals
                    .Select(v => v.HeartRate)
                    .ToList(),

                RespiratoryRate = hourlyVitals
                    .Select(v => v.RespiratoryRate)
                    .ToList(),

                BloodPressure = hourlyVitals
                    .Select(v => v.BloodPressure)
                    .ToList(),

                Temperature = hourlyVitals
                    .Select(v => v.Temperature)
                    .ToList()
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

        private static DateTime GetUtcHour(DateTime recordedAt)
        {
            DateTime utcTime;

            if (recordedAt.Kind == DateTimeKind.Utc)
            {
                utcTime = recordedAt;
            }
            else if (recordedAt.Kind == DateTimeKind.Local)
            {
                utcTime = recordedAt.ToUniversalTime();
            }
            else
            {
                utcTime = DateTime.SpecifyKind(recordedAt, DateTimeKind.Utc);
            }

            return new DateTime(
                utcTime.Year,
                utcTime.Month,
                utcTime.Day,
                utcTime.Hour,
                0,
                0,
                DateTimeKind.Utc
            );
        }

        private static DateTime ToSingaporeTime(DateTime utcDateTime)
        {
            DateTime utcTime = utcDateTime.Kind == DateTimeKind.Utc
                ? utcDateTime
                : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

            TimeZoneInfo singaporeTimeZone;

            try
            {
                singaporeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Singapore Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                singaporeTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Singapore");
            }

            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, singaporeTimeZone);
        }

        private static float? Average(IEnumerable<float?> values)
        {
            var validValues = values
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            return validValues.Count == 0 ? null : validValues.Average();
        }

        private static string GetHeartRateStatus(float? value)
        {
            if (!value.HasValue) return "No data";
            if (value < 60 || value > 100) return "Attention";
            return "Normal";
        }

        private static string GetRespirationStatus(float? value)
        {
            if (!value.HasValue) return "No data";
            if (value < 12 || value > 20) return "Attention";
            return "Normal";
        }

        private static string GetBloodPressureStatus(float? value)
        {
            if (!value.HasValue) return "No data";
            if (value < 90 || value > 140) return "Attention";
            return "Normal";
        }

        private static string GetTemperatureStatus(float? value)
        {
            if (!value.HasValue) return "No data";
            if (value >= 38) return "Attention";
            if (value < 36) return "Warning";
            return "Normal";
        }
    }
}