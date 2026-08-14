using System.Data;
using System.Data.Common;
using INFP_Proj.Data;
using INFP_Proj.Models;
using INFP_Proj.Services;
using INFP_Proj.ViewModel;
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
        private static readonly HashSet<string> AllowedVitals =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "HeartRate",
                "RespiratoryRate",
                "SystolicBloodPressure",
                "DiastolicBloodPressure"
            };

        private static readonly HashSet<string> AllowedDirections =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "Spike",
                "Dip",
                "Stable"
            };

        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly IEmailService _emailService;

        public TrackerModel(
            AppDbContext context,
            UserManager<AppUser> userManager,
            IEmailService emailService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
        }

        public VitalsChartViewModel ChartData { get; set; } = new();

        public bool HasPatientRecord { get; set; }

        public float? LatestHeartRate { get; set; }
        public float? LatestSystolicBloodPressure { get; set; }
        public float? LatestDiastolicBloodPressure { get; set; }
        public float? LatestRespiration { get; set; }
        public DateTime? LatestUpdatedAt { get; set; }

        public string HeartRateStatus { get; set; } = "No data";
        public string SystolicStatus { get; set; } = "No data";
        public string DiastolicStatus { get; set; } = "No data";
        public string RespirationStatus { get; set; } = "No data";

        public bool HasAttention { get; set; }

        // Heart-rate baseline and calculated range.
        public float HeartRateBaseline { get; set; }
        public float HeartRateLowerThreshold { get; set; }
        public float HeartRateUpperThreshold { get; set; }
        public float HeartRateLowerPercentage { get; set; }
        public float HeartRateUpperPercentage { get; set; }

        // Respiratory baseline and calculated range.
        public float RespiratoryRateBaseline { get; set; }
        public float RespiratoryRateLowerThreshold { get; set; }
        public float RespiratoryRateUpperThreshold { get; set; }
        public float RespiratoryRateUpperPercentage { get; set; }

        // Fixed blood-pressure ranges.
        public float SystolicLowerThreshold { get; set; }
        public float SystolicUpperThreshold { get; set; }
        public float DiastolicLowerThreshold { get; set; }
        public float DiastolicUpperThreshold { get; set; }

        public async Task OnGetAsync()
        {
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                HasPatientRecord = false;
                return;
            }

            Patients? patient =
                await GetLinkedPatientAsync(currentUserId);

            if (patient == null)
            {
                HasPatientRecord = false;
                return;
            }

            HasPatientRecord = true;

            ThresholdConfiguration configuration =
                await LoadThresholdConfigurationAsync();

            List<Vitals> recentVitals =
                await GetRecentVitalsAsync(
                    patient.PatientID,
                    11);

            Vitals? latestVitals =
                recentVitals.FirstOrDefault();

            /*
             * Exclude the latest reading when calculating its baseline.
             * This prevents the reading from influencing its own range.
             */
            List<Vitals> baselineReadings =
                recentVitals
                    .Skip(1)
                    .Take(10)
                    .ToList();

            PatientThresholdState thresholds =
                BuildPatientThresholdState(
                    configuration,
                    baselineReadings);

            SetDisplayedThresholds(thresholds);

            await LoadLatestVitalsAsync(
                patient.PatientID,
                latestVitals,
                thresholds);

            await LoadHourlyAverageChartAsync(patient);
        }

        /*
         * The browser calls this every 30 seconds.
         * It only reads the latest values. It does not create new rows.
         */
        public async Task<IActionResult> OnGetLatestVitalsAsync()
        {
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return new JsonResult(new
                {
                    success = false,
                    message = "User is not logged in."
                });
            }

            Patients? patient =
                await GetLinkedPatientAsync(currentUserId);

            if (patient == null)
            {
                return new JsonResult(new
                {
                    success = false,
                    message =
                        "No patient record is linked to this account."
                });
            }

            ThresholdConfiguration configuration =
                await LoadThresholdConfigurationAsync();

            List<Vitals> recentVitals =
                await GetRecentVitalsAsync(
                    patient.PatientID,
                    11);

            Vitals? latestVitals =
                recentVitals.FirstOrDefault();

            List<Vitals> baselineReadings =
                recentVitals
                    .Skip(1)
                    .Take(10)
                    .ToList();

            PatientThresholdState thresholds =
                BuildPatientThresholdState(
                    configuration,
                    baselineReadings);

            BraceletRelation? braceletRelation =
                await _context.BraceletRelations
                    .AsNoTracking()
                    .Include(br => br.Bracelet)
                    .FirstOrDefaultAsync(
                        br => br.PatientID ==
                              patient.PatientID);

            Bracelet? bracelet =
                braceletRelation?.Bracelet;

            float? heartRate =
                latestVitals?.HeartRate ??
                bracelet?.HeartRate;

            float? respiratoryRate =
                latestVitals?.RespiratoryRate ??
                bracelet?.RespiratoryRate;

            float? systolicBloodPressure =
                latestVitals?.SystolicBloodPressure ??
                bracelet?.SystolicBloodPressure;

            float? diastolicBloodPressure =
                latestVitals?.DiastolicBloodPressure ??
                bracelet?.DiastolicBloodPressure;

            string heartRateStatus =
                GetVitalStatus(
                    heartRate,
                    thresholds.HeartRate);

            string respirationStatus =
                GetVitalStatus(
                    respiratoryRate,
                    thresholds.Respiratory);

            string systolicStatus =
                GetVitalStatus(
                    systolicBloodPressure,
                    thresholds.Systolic);

            string diastolicStatus =
                GetVitalStatus(
                    diastolicBloodPressure,
                    thresholds.Diastolic);

            bool hasAttention =
                IsAbnormalStatus(heartRateStatus) ||
                IsAbnormalStatus(respirationStatus) ||
                IsAbnormalStatus(systolicStatus) ||
                IsAbnormalStatus(diastolicStatus);

            return new JsonResult(new
            {
                success = true,

                heartRate =
                    heartRate?.ToString("0") ?? "N/A",

                respiration =
                    respiratoryRate?.ToString("0") ?? "N/A",

                systolicBloodPressure =
                    systolicBloodPressure?.ToString("0") ?? "N/A",

                diastolicBloodPressure =
                    diastolicBloodPressure?.ToString("0") ?? "N/A",

                updatedAt = latestVitals == null
                    ? "N/A"
                    : ToSingaporeTime(
                            latestVitals.RecordedAt)
                        .ToString(
                            "dd MMM yyyy, hh:mm:ss tt"),

                heartRateStatus,
                respirationStatus,
                systolicStatus,
                diastolicStatus,
                hasAttention,

                heartRateBaseline =
                    thresholds.HeartRateBaseline.ToString("0.#"),

                heartRateLower =
                    thresholds.HeartRate.Lower.ToString("0.#"),

                heartRateUpper =
                    thresholds.HeartRate.Upper.ToString("0.#"),

                respiratoryBaseline =
                    thresholds.RespiratoryBaseline.ToString("0.#"),

                respiratoryLower =
                    thresholds.Respiratory.Lower.ToString("0.#"),

                respiratoryUpper =
                    thresholds.Respiratory.Upper.ToString("0.#"),

                systolicLower =
                    thresholds.Systolic.Lower.ToString("0.#"),

                systolicUpper =
                    thresholds.Systolic.Upper.ToString("0.#"),

                diastolicLower =
                    thresholds.Diastolic.Lower.ToString("0.#"),

                diastolicUpper =
                    thresholds.Diastolic.Upper.ToString("0.#")
            });
        }

        public async Task<IActionResult>
            OnPostSimulateVitalAsync(
                string vital,
                string direction)
        {
            string? currentUserId = _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrWhiteSpace(vital) ||
                string.IsNullOrWhiteSpace(direction) ||
                !AllowedVitals.Contains(vital) ||
                !AllowedDirections.Contains(direction))
            {
                TempData["ErrorMessage"] =
                    "The selected vital simulation option is invalid.";

                return RedirectToPage();
            }

            Patients? patient =
                await GetLinkedPatientAsync(currentUserId);

            if (patient == null)
            {
                TempData["ErrorMessage"] =
                    "No patient record is linked to this account.";

                return RedirectToPage();
            }

            ThresholdConfiguration configuration =
                await LoadThresholdConfigurationAsync();

            /*
             * Existing readings are used as the baseline before
             * the new simulated reading is inserted.
             */
            List<Vitals> baselineReadings =
                await GetRecentVitalsAsync(
                    patient.PatientID,
                    10);

            PatientThresholdState thresholds =
                BuildPatientThresholdState(
                    configuration,
                    baselineReadings);

            Vitals reading =
                CreateStableReading(
                    patient.PatientID,
                    thresholds);

            ApplySimulationDirection(
                reading,
                vital,
                direction,
                thresholds);

            await SaveReadingAndUpdateBraceletAsync(
                patient.PatientID,
                reading);

            AlertResult alertResult =
                await CreateAlertAndNotifyCaretakersAsync(
                    patient,
                    reading,
                    vital,
                    thresholds);

            string readableVital =
                GetReadableVitalName(vital);

            if (alertResult.AlertCreated)
            {
                if (alertResult.CaretakerCount == 0)
                {
                    TempData["ErrorMessage"] =
                        $"{direction} reading recorded for " +
                        $"{readableVital}. An emergency log was " +
                        "created, but no linked caretaker email " +
                        "was found.";
                }
                else if (alertResult.EmailsSent == 0)
                {
                    TempData["ErrorMessage"] =
                        $"{direction} reading recorded for " +
                        $"{readableVital}. An emergency log was " +
                        "created, but the caretaker email could " +
                        "not be sent.";
                }
                else if (alertResult.EmailsSent <
                         alertResult.CaretakerCount)
                {
                    TempData["ErrorMessage"] =
                        $"{direction} reading recorded for " +
                        $"{readableVital}. The emergency log was " +
                        $"created, but only {alertResult.EmailsSent} " +
                        $"of {alertResult.CaretakerCount} caretaker " +
                        "email(s) were sent.";
                }
                else
                {
                    TempData["Message"] =
                        $"{direction} reading recorded for " +
                        $"{readableVital}. The emergency log was " +
                        "created and all linked caretakers were " +
                        "notified by email.";
                }
            }
            else if (alertResult.CooldownActive)
            {
                TempData["Message"] =
                    $"{direction} reading recorded for " +
                    $"{readableVital}. A recent unresolved alert " +
                    "already exists, so another email was not sent.";
            }
            else
            {
                TempData["Message"] =
                    $"{direction} reading recorded for " +
                    $"{readableVital}.";
            }

            return RedirectToPage();
        }

        /*
         * Reads the database with explicit CAST statements.
         * This supports databases where the columns were created as int
         * even though the C# model currently uses float.
         */
        private async Task<ThresholdConfiguration>
            LoadThresholdConfigurationAsync()
        {
            DbConnection connection =
                _context.Database.GetDbConnection();

            bool shouldCloseConnection =
                connection.State != ConnectionState.Open;

            if (shouldCloseConnection)
            {
                await connection.OpenAsync();
            }

            try
            {
                await using DbCommand command =
                    connection.CreateCommand();

                command.CommandText = """
                    SELECT TOP (1)
                        CAST(SBPLowerThreshold AS real)
                            AS SBPLowerThreshold,

                        CAST(SBPUpperThreshold AS real)
                            AS SBPUpperThreshold,

                        CAST(DBPLowerThreshold AS real)
                            AS DBPLowerThreshold,

                        CAST(DBPUpperThreshold AS real)
                            AS DBPUpperThreshold,

                        CAST(
                            HeartRateLowerPercentageThreshold
                            AS real
                        ) AS HeartRateLowerPercentageThreshold,

                        CAST(
                            HeartRateUpperPercentageThreshold
                            AS real
                        ) AS HeartRateUpperPercentageThreshold,

                        CAST(
                            RespiratoryRateLowerThreshold
                            AS real
                        ) AS RespiratoryRateLowerThreshold,

                        CAST(
                            RespiratoryRateUpperPercentageThreshold
                            AS real
                        ) AS RespiratoryRateUpperPercentageThreshold

                    FROM dbo.Thresholds
                    ORDER BY ThresholdID DESC;
                    """;

                await using DbDataReader reader =
                    await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return CreateFallbackConfiguration();
                }

                return new ThresholdConfiguration
                {
                    Systolic = CreateFixedRange(
                        ReadNullableFloat(
                            reader,
                            "SBPLowerThreshold"),
                        ReadNullableFloat(
                            reader,
                            "SBPUpperThreshold"),
                        90f,
                        120f),

                    Diastolic = CreateFixedRange(
                        ReadNullableFloat(
                            reader,
                            "DBPLowerThreshold"),
                        ReadNullableFloat(
                            reader,
                            "DBPUpperThreshold"),
                        60f,
                        80f),

                    HeartRateLowerPercentage =
                        ValidatePercentage(
                            ReadNullableFloat(
                                reader,
                                "HeartRateLowerPercentageThreshold"),
                            20f),

                    HeartRateUpperPercentage =
                        ValidatePercentage(
                            ReadNullableFloat(
                                reader,
                                "HeartRateUpperPercentageThreshold"),
                            20f),

                    RespiratoryLowerThreshold =
                        ReadPositiveValueOrFallback(
                            ReadNullableFloat(
                                reader,
                                "RespiratoryRateLowerThreshold"),
                            12f),

                    RespiratoryUpperPercentage =
                        ValidatePercentage(
                            ReadNullableFloat(
                                reader,
                                "RespiratoryRateUpperPercentageThreshold"),
                            25f)
                };
            }
            finally
            {
                if (shouldCloseConnection)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private static ThresholdConfiguration
            CreateFallbackConfiguration()
        {
            return new ThresholdConfiguration
            {
                Systolic = new VitalRange
                {
                    Lower = 90f,
                    Upper = 120f
                },

                Diastolic = new VitalRange
                {
                    Lower = 60f,
                    Upper = 80f
                },

                HeartRateLowerPercentage = 20f,
                HeartRateUpperPercentage = 20f,

                RespiratoryLowerThreshold = 12f,
                RespiratoryUpperPercentage = 25f
            };
        }

        private static float? ReadNullableFloat(
            DbDataReader reader,
            string columnName)
        {
            int columnIndex =
                reader.GetOrdinal(columnName);

            if (reader.IsDBNull(columnIndex))
            {
                return null;
            }

            return Convert.ToSingle(
                reader.GetValue(columnIndex));
        }

        private static float ValidatePercentage(
            float? configuredValue,
            float fallback)
        {
            if (!configuredValue.HasValue ||
                configuredValue.Value <= 0 ||
                configuredValue.Value >= 100)
            {
                return fallback;
            }

            return configuredValue.Value;
        }

        private static float ReadPositiveValueOrFallback(
            float? configuredValue,
            float fallback)
        {
            if (!configuredValue.HasValue ||
                configuredValue.Value <= 0)
            {
                return fallback;
            }

            return configuredValue.Value;
        }

        private static VitalRange CreateFixedRange(
            float? configuredLower,
            float? configuredUpper,
            float fallbackLower,
            float fallbackUpper)
        {
            if (!configuredLower.HasValue ||
                !configuredUpper.HasValue ||
                configuredLower.Value >= configuredUpper.Value)
            {
                return new VitalRange
                {
                    Lower = fallbackLower,
                    Upper = fallbackUpper
                };
            }

            return new VitalRange
            {
                Lower = configuredLower.Value,
                Upper = configuredUpper.Value
            };
        }

        private static PatientThresholdState
            BuildPatientThresholdState(
                ThresholdConfiguration configuration,
                IEnumerable<Vitals> baselineReadings)
        {
            List<Vitals> readings =
                baselineReadings.ToList();

            float heartRateBaseline =
                CalculateAverageBaseline(
                    readings.Select(v => v.HeartRate),
                    75f);

            float respiratoryBaseline =
                CalculateAverageBaseline(
                    readings.Select(v => v.RespiratoryRate),
                    16f);

            float heartRateLower =
                heartRateBaseline *
                (1f -
                 configuration
                     .HeartRateLowerPercentage / 100f);

            float heartRateUpper =
                heartRateBaseline *
                (1f +
                 configuration
                     .HeartRateUpperPercentage / 100f);

            float respiratoryUpper =
                respiratoryBaseline *
                (1f +
                 configuration
                     .RespiratoryUpperPercentage / 100f);

            /*
             * Ensure the calculated upper range remains above
             * the fixed lower respiratory threshold.
             */
            respiratoryUpper = Math.Max(
                configuration.RespiratoryLowerThreshold + 1f,
                respiratoryUpper);

            return new PatientThresholdState
            {
                Configuration = configuration,

                HeartRateBaseline = heartRateBaseline,

                RespiratoryBaseline =
                    respiratoryBaseline,

                HeartRate = new VitalRange
                {
                    Lower = heartRateLower,
                    Upper = heartRateUpper
                },

                Respiratory = new VitalRange
                {
                    Lower =
                        configuration
                            .RespiratoryLowerThreshold,

                    Upper = respiratoryUpper
                },

                Systolic = configuration.Systolic,
                Diastolic = configuration.Diastolic
            };
        }

        private static float CalculateAverageBaseline(
            IEnumerable<float?> values,
            float fallback)
        {
            List<float> validValues =
                values
                    .Where(value =>
                        value.HasValue &&
                        value.Value > 0)
                    .Select(value =>
                        value!.Value)
                    .ToList();

            if (validValues.Count == 0)
            {
                return fallback;
            }

            return validValues.Average();
        }

        private void SetDisplayedThresholds(
            PatientThresholdState thresholds)
        {
            HeartRateBaseline =
                thresholds.HeartRateBaseline;

            HeartRateLowerThreshold =
                thresholds.HeartRate.Lower;

            HeartRateUpperThreshold =
                thresholds.HeartRate.Upper;

            HeartRateLowerPercentage =
                thresholds.Configuration
                    .HeartRateLowerPercentage;

            HeartRateUpperPercentage =
                thresholds.Configuration
                    .HeartRateUpperPercentage;

            RespiratoryRateBaseline =
                thresholds.RespiratoryBaseline;

            RespiratoryRateLowerThreshold =
                thresholds.Respiratory.Lower;

            RespiratoryRateUpperThreshold =
                thresholds.Respiratory.Upper;

            RespiratoryRateUpperPercentage =
                thresholds.Configuration
                    .RespiratoryUpperPercentage;

            SystolicLowerThreshold =
                thresholds.Systolic.Lower;

            SystolicUpperThreshold =
                thresholds.Systolic.Upper;

            DiastolicLowerThreshold =
                thresholds.Diastolic.Lower;

            DiastolicUpperThreshold =
                thresholds.Diastolic.Upper;
        }

        private static Vitals CreateStableReading(
            int patientId,
            PatientThresholdState thresholds)
        {
            return new Vitals
            {
                PatientID = patientId,

                HeartRate =
                    GenerateStableValue(
                        thresholds.HeartRate),

                RespiratoryRate =
                    GenerateStableValue(
                        thresholds.Respiratory),

                SystolicBloodPressure =
                    GenerateStableValue(
                        thresholds.Systolic),

                DiastolicBloodPressure =
                    GenerateStableValue(
                        thresholds.Diastolic),

                RecordedAt = DateTime.UtcNow
            };
        }

        private static void ApplySimulationDirection(
            Vitals reading,
            string vital,
            string direction,
            PatientThresholdState thresholds)
        {
            if (vital.Equals(
                    "HeartRate",
                    StringComparison.OrdinalIgnoreCase))
            {
                reading.HeartRate =
                    GenerateByDirection(
                        thresholds.HeartRate,
                        direction);

                return;
            }

            if (vital.Equals(
                    "RespiratoryRate",
                    StringComparison.OrdinalIgnoreCase))
            {
                reading.RespiratoryRate =
                    GenerateByDirection(
                        thresholds.Respiratory,
                        direction);

                return;
            }

            if (vital.Equals(
                    "SystolicBloodPressure",
                    StringComparison.OrdinalIgnoreCase))
            {
                reading.SystolicBloodPressure =
                    GenerateByDirection(
                        thresholds.Systolic,
                        direction);

                return;
            }

            if (vital.Equals(
                    "DiastolicBloodPressure",
                    StringComparison.OrdinalIgnoreCase))
            {
                reading.DiastolicBloodPressure =
                    GenerateByDirection(
                        thresholds.Diastolic,
                        direction);
            }
        }

        private static float GenerateByDirection(
            VitalRange range,
            string direction)
        {
            if (direction.Equals(
                    "Spike",
                    StringComparison.OrdinalIgnoreCase))
            {
                return GenerateSpikeValue(range);
            }

            if (direction.Equals(
                    "Dip",
                    StringComparison.OrdinalIgnoreCase))
            {
                return GenerateDipValue(range);
            }

            return GenerateStableValue(range);
        }

        private static float GenerateStableValue(
            VitalRange range)
        {
            float totalRange =
                range.Upper - range.Lower;

            float safeLower =
                range.Lower + totalRange * 0.20f;

            float safeUpper =
                range.Upper - totalRange * 0.20f;

            return RandomBetween(
                safeLower,
                safeUpper);
        }

        private static float GenerateSpikeValue(
            VitalRange range)
        {
            float totalRange = Math.Max(
                range.Upper - range.Lower,
                5f);

            float minimumIncrease = Math.Max(
                1f,
                totalRange * 0.10f);

            float maximumIncrease = Math.Max(
                3f,
                totalRange * 0.30f);

            return range.Upper +
                   RandomBetween(
                       minimumIncrease,
                       maximumIncrease);
        }

        private static float GenerateDipValue(
            VitalRange range)
        {
            float totalRange = Math.Max(
                range.Upper - range.Lower,
                5f);

            float minimumDecrease = Math.Max(
                1f,
                totalRange * 0.10f);

            float maximumDecrease = Math.Max(
                3f,
                totalRange * 0.30f);

            return Math.Max(
                1f,
                range.Lower -
                RandomBetween(
                    minimumDecrease,
                    maximumDecrease));
        }

        private static float RandomBetween(
            float minimum,
            float maximum)
        {
            if (maximum <= minimum)
            {
                return minimum;
            }

            return minimum +
                   (float)Random.Shared.NextDouble() *
                   (maximum - minimum);
        }

        private async Task
            SaveReadingAndUpdateBraceletAsync(
                int patientId,
                Vitals reading)
        {
            BraceletRelation? braceletRelation =
                await _context.BraceletRelations
                    .Include(br => br.Bracelet)
                    .FirstOrDefaultAsync(
                        br => br.PatientID == patientId);

            if (braceletRelation?.Bracelet != null)
            {
                Bracelet bracelet =
                    braceletRelation.Bracelet;

                bracelet.HeartRate =
                    reading.HeartRate;

                bracelet.RespiratoryRate =
                    reading.RespiratoryRate;

                bracelet.SystolicBloodPressure =
                    reading.SystolicBloodPressure;

                bracelet.DiastolicBloodPressure =
                    reading.DiastolicBloodPressure;

                bracelet.Battery = Math.Max(
                    0f,
                    (bracelet.Battery ?? 100f) - 0.1f);
            }

            _context.Vitals.Add(reading);

            await _context.SaveChangesAsync();
        }

        private async Task<AlertResult>
            CreateAlertAndNotifyCaretakersAsync(
                Patients patient,
                Vitals reading,
                string selectedVital,
                PatientThresholdState thresholds)
        {
            VitalAlertDetails? alert =
                GetAlertDetails(
                    reading,
                    selectedVital,
                    thresholds);

            if (alert == null)
            {
                return new AlertResult();
            }

            string eventPrefix =
                $"Vital alert - {alert.DisplayName}:";

            DateTime cooldownStart =
                DateTime.UtcNow.AddMinutes(-30);

            bool recentAlertExists =
                await _context.Logs.AnyAsync(log =>
                    log.PatientID == patient.PatientID &&
                    log.Emergency &&
                    !log.Resolved &&
                    log.Timestamp >= cooldownStart &&
                    log.Event.StartsWith(eventPrefix));

            if (recentAlertExists)
            {
                return new AlertResult
                {
                    CooldownActive = true
                };
            }

            string comparisonDescription =
                alert.Direction == "Dip"
                    ? $"below the lower limit of " +
                      $"{alert.Range.Lower:0.#} " +
                      $"{alert.Unit}"
                    : $"above the upper limit of " +
                      $"{alert.Range.Upper:0.#} " +
                      $"{alert.Unit}";

            var emergencyLog = new Log
            {
                UserID = patient.UserID,
                PatientID = patient.PatientID,

                Event =
                    $"{eventPrefix} {alert.Direction}. " +
                    $"Reading {alert.Value:0.#} " +
                    $"{alert.Unit} was " +
                    $"{comparisonDescription}.",

                Emergency = true,
                Resolved = false,
                selfAcknowledged = false,
                relativeAcknowledged = false,
                Timestamp = DateTime.UtcNow
            };

            _context.Logs.Add(emergencyLog);
            await _context.SaveChangesAsync();

            List<string> caretakerEmails =
                await GetCaretakerEmailsAsync(
                    patient.PatientID,
                    patient.UserID);
            string patientName =
                patient.User == null
                    ? $"Patient #{patient.PatientID}"
                    : ($"{patient.User.FirstName} " +
                       $"{patient.User.LastName}")
                        .Trim();

            DateTime recordedAtSingapore =
                ToSingaporeTime(reading.RecordedAt);

            string subject =
                $"Hospital Portal Alert - {alert.DisplayName} - " +
                $"{recordedAtSingapore:yyyyMMdd-HHmmss}";

            string emailBody =
                $"Hospital Portal vital notification\n\n" +
                $"Patient: {patientName}\n" +
                $"Vital: {alert.DisplayName}\n" +
                $"Condition: {alert.Direction}\n" +
                $"Reading: {alert.Value:0.#} {alert.Unit}\n" +
                $"Calculated range: " +
                $"{alert.Range.Lower:0.#}-" +
                $"{alert.Range.Upper:0.#} {alert.Unit}\n" +
                $"Recorded at: " +
                $"{recordedAtSingapore:dd MMM yyyy, hh:mm tt}\n\n" +
                "Please sign in to the Hospital Portal to review " +
                "and acknowledge the alert.\n\n" +
                "This is an automated notification.";

            int emailsSent = 0;

            foreach (string caretakerEmail in caretakerEmails)
            {
                bool sent =
                    await _emailService.SendEmailAsync(
                        caretakerEmail,
                        subject,
                        emailBody);

                if (sent)
                {
                    emailsSent++;
                }
            }

            return new AlertResult
            {
                AlertCreated = true,
                CaretakerCount = caretakerEmails.Count,
                EmailsSent = emailsSent
            };
        }

        private async Task<List<string>>
            GetCaretakerEmailsAsync(
                int patientId,
                string patientUserId)
        {
            List<string?> databaseEmails =
                await _context.Relationships
                    .Where(relationship =>
                        relationship.PatientID == patientId &&
                        relationship.UserID != patientUserId)

                    .Join(
                        _context.Users,
                        relationship => relationship.UserID,
                        user => user.Id,
                        (relationship, user) => user.Email)

                    .ToListAsync();

            return databaseEmails
                .Where(email =>
                    !string.IsNullOrWhiteSpace(email))

                .Select(email =>
                    email!.Trim())

                .Distinct(
                    StringComparer.OrdinalIgnoreCase)

                .ToList();
        }

        private static VitalAlertDetails?
            GetAlertDetails(
                Vitals reading,
                string vital,
                PatientThresholdState thresholds)
        {
            if (vital.Equals(
                    "HeartRate",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BuildAlertDetails(
                    "Heart Rate",
                    "bpm",
                    reading.HeartRate,
                    thresholds.HeartRate);
            }

            if (vital.Equals(
                    "RespiratoryRate",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BuildAlertDetails(
                    "Breathing Rate",
                    "breaths/min",
                    reading.RespiratoryRate,
                    thresholds.Respiratory);
            }

            if (vital.Equals(
                    "SystolicBloodPressure",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BuildAlertDetails(
                    "Systolic Blood Pressure",
                    "mmHg",
                    reading.SystolicBloodPressure,
                    thresholds.Systolic);
            }

            if (vital.Equals(
                    "DiastolicBloodPressure",
                    StringComparison.OrdinalIgnoreCase))
            {
                return BuildAlertDetails(
                    "Diastolic Blood Pressure",
                    "mmHg",
                    reading.DiastolicBloodPressure,
                    thresholds.Diastolic);
            }

            return null;
        }

        private static VitalAlertDetails?
            BuildAlertDetails(
                string displayName,
                string unit,
                float? value,
                VitalRange range)
        {
            if (!value.HasValue)
            {
                return null;
            }

            if (value.Value < range.Lower)
            {
                return new VitalAlertDetails
                {
                    DisplayName = displayName,
                    Unit = unit,
                    Value = value.Value,
                    Direction = "Dip",
                    Range = range
                };
            }

            if (value.Value > range.Upper)
            {
                return new VitalAlertDetails
                {
                    DisplayName = displayName,
                    Unit = unit,
                    Value = value.Value,
                    Direction = "Spike",
                    Range = range
                };
            }

            return null;
        }

        private async Task LoadLatestVitalsAsync(
            int patientId,
            Vitals? latestVitals,
            PatientThresholdState thresholds)
        {
            BraceletRelation? braceletRelation =
                await _context.BraceletRelations
                    .AsNoTracking()
                    .Include(br => br.Bracelet)
                    .FirstOrDefaultAsync(
                        br => br.PatientID == patientId);

            Bracelet? bracelet =
                braceletRelation?.Bracelet;

            LatestHeartRate =
                latestVitals?.HeartRate ??
                bracelet?.HeartRate;

            LatestRespiration =
                latestVitals?.RespiratoryRate ??
                bracelet?.RespiratoryRate;

            LatestSystolicBloodPressure =
                latestVitals?.SystolicBloodPressure ??
                bracelet?.SystolicBloodPressure;

            LatestDiastolicBloodPressure =
                latestVitals?.DiastolicBloodPressure ??
                bracelet?.DiastolicBloodPressure;

            LatestUpdatedAt = latestVitals == null
                ? null
                : ToSingaporeTime(
                    latestVitals.RecordedAt);

            HeartRateStatus =
                GetVitalStatus(
                    LatestHeartRate,
                    thresholds.HeartRate);

            RespirationStatus =
                GetVitalStatus(
                    LatestRespiration,
                    thresholds.Respiratory);

            SystolicStatus =
                GetVitalStatus(
                    LatestSystolicBloodPressure,
                    thresholds.Systolic);

            DiastolicStatus =
                GetVitalStatus(
                    LatestDiastolicBloodPressure,
                    thresholds.Diastolic);

            HasAttention =
                IsAbnormalStatus(HeartRateStatus) ||
                IsAbnormalStatus(RespirationStatus) ||
                IsAbnormalStatus(SystolicStatus) ||
                IsAbnormalStatus(DiastolicStatus);
        }

        private async Task<List<Vitals>>
            GetRecentVitalsAsync(
                int patientId,
                int numberOfReadings)
        {
            return await _context.Vitals
                .AsNoTracking()
                .Where(v =>
                    v.PatientID == patientId)
                .OrderByDescending(v =>
                    v.RecordedAt)
                .Take(numberOfReadings)
                .ToListAsync();
        }

        private async Task LoadHourlyAverageChartAsync(
            Patients patient)
        {
            List<Vitals> vitals =
                await _context.Vitals
                    .AsNoTracking()
                    .Where(v =>
                        v.PatientID == patient.PatientID)
                    .OrderBy(v =>
                        v.RecordedAt)
                    .ToListAsync();

            var hourlyVitals = vitals
                .GroupBy(v =>
                    GetUtcHour(v.RecordedAt))
                .OrderBy(group =>
                    group.Key)
                .Select(group => new
                {
                    HourUtc = group.Key,

                    HeartRate = Average(
                        group.Select(v =>
                            v.HeartRate)),

                    RespiratoryRate = Average(
                        group.Select(v =>
                            v.RespiratoryRate)),

                    SystolicBloodPressure = Average(
                        group.Select(v =>
                            v.SystolicBloodPressure)),

                    DiastolicBloodPressure = Average(
                        group.Select(v =>
                            v.DiastolicBloodPressure))
                })
                .ToList();

            ChartData = new VitalsChartViewModel
            {
                PatientId = patient.PatientID,

                PatientName =
                    patient.User == null
                        ? $"Patient #{patient.PatientID}"
                        : ($"{patient.User.FirstName} " +
                           $"{patient.User.LastName}")
                            .Trim(),

                Labels = hourlyVitals
                    .Select(v =>
                        ToSingaporeTime(v.HourUtc)
                            .ToString("MMM d, HH:00"))
                    .ToList(),

                HeartRate = hourlyVitals
                    .Select(v => v.HeartRate)
                    .ToList(),

                RespiratoryRate = hourlyVitals
                    .Select(v => v.RespiratoryRate)
                    .ToList(),

                SystolicBloodPressure = hourlyVitals
                    .Select(v =>
                        v.SystolicBloodPressure)
                    .ToList(),

                DiastolicBloodPressure = hourlyVitals
                    .Select(v =>
                        v.DiastolicBloodPressure)
                    .ToList()
            };
        }

        private async Task<Patients?>
            GetLinkedPatientAsync(
                string currentUserId)
        {
            const string selectedPatientSessionKey =
                "SelectedPatientId";

            int? selectedPatientId =
                HttpContext.Session.GetInt32(
                    selectedPatientSessionKey);

            if (selectedPatientId.HasValue)
            {
                bool isOwner =
                    await _context.Patients
                        .AsNoTracking()
                        .AnyAsync(patient =>
                            patient.PatientID ==
                                selectedPatientId.Value &&
                            patient.UserID ==
                                currentUserId);

                if (isOwner)
                {
                    return await _context.Patients
                        .Include(patient => patient.User)
                        .FirstOrDefaultAsync(patient =>
                            patient.PatientID ==
                                selectedPatientId.Value);
                }

                bool isRelated =
                    await _context.Relationships
                        .AsNoTracking()
                        .AnyAsync(relationship =>
                            relationship.PatientID ==
                                selectedPatientId.Value &&
                            relationship.UserID ==
                                currentUserId);

                if (isRelated)
                {
                    return await _context.Patients
                        .Include(patient => patient.User)
                        .FirstOrDefaultAsync(patient =>
                            patient.PatientID ==
                                selectedPatientId.Value);
                }

                HttpContext.Session.Remove(
                    selectedPatientSessionKey);
            }

            // If this account is the patient, use its own record.
            Patients? ownPatient =
                await _context.Patients
                    .Include(patient => patient.User)
                    .FirstOrDefaultAsync(patient =>
                        patient.UserID == currentUserId);

            if (ownPatient != null)
            {
                HttpContext.Session.SetInt32(
                    selectedPatientSessionKey,
                    ownPatient.PatientID);

                return ownPatient;
            }

            // A relative/caregiver must select a patient
            // from the Dashboard before opening the tracker.
            return null;
        }

        private static string GetVitalStatus(
            float? value,
            VitalRange range)
        {
            if (!value.HasValue)
            {
                return "No data";
            }

            if (value.Value < range.Lower)
            {
                return "Dip";
            }

            if (value.Value > range.Upper)
            {
                return "Spike";
            }

            return "Stable";
        }

        private static bool IsAbnormalStatus(
            string status)
        {
            return status == "Dip" ||
                   status == "Spike";
        }

        private static string GetReadableVitalName(
            string vital)
        {
            return vital switch
            {
                "HeartRate" =>
                    "Heart Rate",

                "RespiratoryRate" =>
                    "Breathing Rate",

                "SystolicBloodPressure" =>
                    "Systolic Blood Pressure",

                "DiastolicBloodPressure" =>
                    "Diastolic Blood Pressure",

                _ => vital
            };
        }

        private static DateTime GetUtcHour(
            DateTime recordedAt)
        {
            DateTime utcTime;

            if (recordedAt.Kind ==
                DateTimeKind.Utc)
            {
                utcTime = recordedAt;
            }
            else if (recordedAt.Kind ==
                     DateTimeKind.Local)
            {
                utcTime =
                    recordedAt.ToUniversalTime();
            }
            else
            {
                utcTime =
                    DateTime.SpecifyKind(
                        recordedAt,
                        DateTimeKind.Utc);
            }

            return new DateTime(
                utcTime.Year,
                utcTime.Month,
                utcTime.Day,
                utcTime.Hour,
                0,
                0,
                DateTimeKind.Utc);
        }

        private static DateTime ToSingaporeTime(
            DateTime utcDateTime)
        {
            DateTime utcTime =
                utcDateTime.Kind ==
                DateTimeKind.Utc
                    ? utcDateTime
                    : DateTime.SpecifyKind(
                        utcDateTime,
                        DateTimeKind.Utc);

            TimeZoneInfo singaporeTimeZone;

            try
            {
                singaporeTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "Singapore Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                singaporeTimeZone =
                    TimeZoneInfo.FindSystemTimeZoneById(
                        "Asia/Singapore");
            }

            return TimeZoneInfo.ConvertTimeFromUtc(
                utcTime,
                singaporeTimeZone);
        }

        private static float? Average(
            IEnumerable<float?> values)
        {
            List<float> validValues =
                values
                    .Where(value =>
                        value.HasValue)
                    .Select(value =>
                        value!.Value)
                    .ToList();

            return validValues.Count == 0
                ? null
                : validValues.Average();
        }

        private sealed class VitalRange
        {
            public float Lower { get; set; }
            public float Upper { get; set; }
        }

        private sealed class ThresholdConfiguration
        {
            public VitalRange Systolic { get; set; } = new();
            public VitalRange Diastolic { get; set; } = new();

            public float HeartRateLowerPercentage { get; set; }
            public float HeartRateUpperPercentage { get; set; }

            public float RespiratoryLowerThreshold { get; set; }
            public float RespiratoryUpperPercentage { get; set; }
        }

        private sealed class PatientThresholdState
        {
            public ThresholdConfiguration Configuration { get; set; }
                = new();

            public float HeartRateBaseline { get; set; }
            public float RespiratoryBaseline { get; set; }

            public VitalRange HeartRate { get; set; } = new();
            public VitalRange Respiratory { get; set; } = new();
            public VitalRange Systolic { get; set; } = new();
            public VitalRange Diastolic { get; set; } = new();
        }

        private sealed class VitalAlertDetails
        {
            public string DisplayName { get; set; }
                = string.Empty;

            public string Unit { get; set; }
                = string.Empty;

            public float Value { get; set; }

            public string Direction { get; set; }
                = string.Empty;

            public VitalRange Range { get; set; }
                = new();
        }

        private sealed class AlertResult
        {
            public bool AlertCreated { get; set; }
            public bool CooldownActive { get; set; }
            public int CaretakerCount { get; set; }
            public int EmailsSent { get; set; }
        }
    }
}