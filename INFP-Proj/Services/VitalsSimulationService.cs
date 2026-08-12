using System.Data;
using System.Data.Common;
using INFP_Proj.Data;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Services
{
    /// <summary>
    /// Creates a test vitals reading for a patient (Spike/Dip/Stable) using the same
    /// threshold-based generation and caretaker-alerting behavior as the patient-facing
    /// "Simulate Vitals" controls on /User/Tracker.
    /// </summary>
    public class VitalsSimulationService
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
        private readonly IEmailService _emailService;

        public VitalsSimulationService(AppDbContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        public async Task<VitalsSimulationOutcome> SimulateVitalAsync(int patientId, string vital, string direction)
        {
            if (string.IsNullOrWhiteSpace(vital) ||
                string.IsNullOrWhiteSpace(direction) ||
                !AllowedVitals.Contains(vital) ||
                !AllowedDirections.Contains(direction))
            {
                return VitalsSimulationOutcome.Error("The selected vital simulation option is invalid.");
            }

            Patients? patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientID == patientId);

            if (patient == null)
            {
                return VitalsSimulationOutcome.Error("No patient record was found for that selection.");
            }

            ThresholdConfiguration configuration = await LoadThresholdConfigurationAsync();

            List<Vitals> baselineReadings = await GetRecentVitalsAsync(patientId, 10);

            PatientThresholdState thresholds = BuildPatientThresholdState(configuration, baselineReadings);

            Vitals reading = CreateStableReading(patientId, thresholds);
            ApplySimulationDirection(reading, vital, direction, thresholds);

            await SaveReadingAndUpdateBraceletAsync(patientId, reading);

            AlertResult alertResult = await CreateAlertAndNotifyCaretakersAsync(patient, reading, vital, thresholds);

            string readableVital = GetReadableVitalName(vital);

            if (alertResult.AlertCreated)
            {
                if (alertResult.CaretakerCount == 0)
                {
                    return VitalsSimulationOutcome.Error(
                        $"{direction} reading recorded for {readableVital}. An emergency log was created, " +
                        "but no linked caretaker email was found.");
                }

                if (alertResult.EmailsSent == 0)
                {
                    return VitalsSimulationOutcome.Error(
                        $"{direction} reading recorded for {readableVital}. An emergency log was created, " +
                        "but the caretaker email could not be sent.");
                }

                if (alertResult.EmailsSent < alertResult.CaretakerCount)
                {
                    return VitalsSimulationOutcome.Error(
                        $"{direction} reading recorded for {readableVital}. An emergency log was created and " +
                        $"{alertResult.EmailsSent} of {alertResult.CaretakerCount} caretaker emails were sent.");
                }

                return VitalsSimulationOutcome.Success(
                    $"{direction} reading recorded for {readableVital}. An emergency log was created and " +
                    $"{alertResult.EmailsSent} caretaker email(s) were sent.");
            }

            if (alertResult.CooldownActive)
            {
                return VitalsSimulationOutcome.Success(
                    $"{direction} reading recorded for {readableVital}. A recent unresolved alert already " +
                    "exists, so another email was not sent.");
            }

            return VitalsSimulationOutcome.Success($"{direction} reading recorded for {readableVital}.");
        }

        private async Task<ThresholdConfiguration> LoadThresholdConfigurationAsync()
        {
            DbConnection connection = _context.Database.GetDbConnection();
            bool closeWhenFinished = connection.State != ConnectionState.Open;

            if (closeWhenFinished)
            {
                await connection.OpenAsync();
            }

            try
            {
                await using DbCommand command = connection.CreateCommand();

                command.CommandText = """
                    SELECT TOP (1)
                        CAST(SBPLowerThreshold AS real) AS SBPLowerThreshold,
                        CAST(SBPUpperThreshold AS real) AS SBPUpperThreshold,
                        CAST(DBPLowerThreshold AS real) AS DBPLowerThreshold,
                        CAST(DBPUpperThreshold AS real) AS DBPUpperThreshold,
                        CAST(HeartRateLowerPercentageThreshold AS real) AS HeartRateLowerPercentageThreshold,
                        CAST(HeartRateUpperPercentageThreshold AS real) AS HeartRateUpperPercentageThreshold,
                        CAST(RespiratoryRateLowerThreshold AS real) AS RespiratoryRateLowerThreshold,
                        CAST(RespiratoryRateUpperPercentageThreshold AS real) AS RespiratoryRateUpperPercentageThreshold
                    FROM dbo.Thresholds
                    ORDER BY ThresholdID DESC;
                    """;

                await using DbDataReader reader = await command.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return CreateFallbackConfiguration();
                }

                return new ThresholdConfiguration
                {
                    Systolic = CreateFixedRange(
                        ReadNullableFloat(reader, "SBPLowerThreshold"),
                        ReadNullableFloat(reader, "SBPUpperThreshold"),
                        90f, 120f),

                    Diastolic = CreateFixedRange(
                        ReadNullableFloat(reader, "DBPLowerThreshold"),
                        ReadNullableFloat(reader, "DBPUpperThreshold"),
                        60f, 80f),

                    HeartRateLowerPercentage = ValidatePercentage(
                        ReadNullableFloat(reader, "HeartRateLowerPercentageThreshold"), 20f),

                    HeartRateUpperPercentage = ValidatePercentage(
                        ReadNullableFloat(reader, "HeartRateUpperPercentageThreshold"), 20f),

                    RespiratoryLowerThreshold = ReadPositiveValueOrFallback(
                        ReadNullableFloat(reader, "RespiratoryRateLowerThreshold"), 12f),

                    RespiratoryUpperPercentage = ValidatePercentage(
                        ReadNullableFloat(reader, "RespiratoryRateUpperPercentageThreshold"), 25f)
                };
            }
            finally
            {
                if (closeWhenFinished)
                {
                    await connection.CloseAsync();
                }
            }
        }

        private static ThresholdConfiguration CreateFallbackConfiguration()
        {
            return new ThresholdConfiguration
            {
                Systolic = new VitalRange { Lower = 90f, Upper = 120f },
                Diastolic = new VitalRange { Lower = 60f, Upper = 80f },
                HeartRateLowerPercentage = 20f,
                HeartRateUpperPercentage = 20f,
                RespiratoryLowerThreshold = 12f,
                RespiratoryUpperPercentage = 25f
            };
        }

        private static float? ReadNullableFloat(DbDataReader reader, string columnName)
        {
            int index = reader.GetOrdinal(columnName);
            return reader.IsDBNull(index) ? null : Convert.ToSingle(reader.GetValue(index));
        }

        private static float ValidatePercentage(float? value, float fallback)
        {
            if (!value.HasValue || value.Value <= 0 || value.Value >= 100)
            {
                return fallback;
            }

            return value.Value;
        }

        private static float ReadPositiveValueOrFallback(float? value, float fallback)
        {
            if (!value.HasValue || value.Value <= 0)
            {
                return fallback;
            }

            return value.Value;
        }

        private static VitalRange CreateFixedRange(float? lower, float? upper, float fallbackLower, float fallbackUpper)
        {
            if (!lower.HasValue || !upper.HasValue || lower.Value >= upper.Value)
            {
                return new VitalRange { Lower = fallbackLower, Upper = fallbackUpper };
            }

            return new VitalRange { Lower = lower.Value, Upper = upper.Value };
        }

        private static PatientThresholdState BuildPatientThresholdState(
            ThresholdConfiguration configuration,
            IEnumerable<Vitals> baselineReadings)
        {
            List<Vitals> readings = baselineReadings.ToList();

            float heartBaseline = CalculateAverageBaseline(readings.Select(v => v.HeartRate), 75f);
            float respiratoryBaseline = CalculateAverageBaseline(readings.Select(v => v.RespiratoryRate), 16f);

            float respiratoryUpper = respiratoryBaseline * (1f + configuration.RespiratoryUpperPercentage / 100f);
            respiratoryUpper = Math.Max(configuration.RespiratoryLowerThreshold + 1f, respiratoryUpper);

            return new PatientThresholdState
            {
                HeartRate = new VitalRange
                {
                    Lower = heartBaseline * (1f - configuration.HeartRateLowerPercentage / 100f),
                    Upper = heartBaseline * (1f + configuration.HeartRateUpperPercentage / 100f)
                },

                Respiratory = new VitalRange
                {
                    Lower = configuration.RespiratoryLowerThreshold,
                    Upper = respiratoryUpper
                },

                Systolic = configuration.Systolic,
                Diastolic = configuration.Diastolic
            };
        }

        private static float CalculateAverageBaseline(IEnumerable<float?> values, float fallback)
        {
            List<float> validValues = values
                .Where(value => value.HasValue && value.Value > 0)
                .Select(value => value!.Value)
                .ToList();

            return validValues.Count == 0 ? fallback : validValues.Average();
        }

        private static Vitals CreateStableReading(int patientId, PatientThresholdState thresholds)
        {
            return new Vitals
            {
                PatientID = patientId,
                HeartRate = GenerateStableValue(thresholds.HeartRate),
                RespiratoryRate = GenerateStableValue(thresholds.Respiratory),
                SystolicBloodPressure = GenerateStableValue(thresholds.Systolic),
                DiastolicBloodPressure = GenerateStableValue(thresholds.Diastolic),
                RecordedAt = DateTime.UtcNow
            };
        }

        private static void ApplySimulationDirection(
            Vitals reading,
            string vital,
            string direction,
            PatientThresholdState thresholds)
        {
            if (vital.Equals("HeartRate", StringComparison.OrdinalIgnoreCase))
            {
                reading.HeartRate = GenerateByDirection(thresholds.HeartRate, direction);
                return;
            }

            if (vital.Equals("RespiratoryRate", StringComparison.OrdinalIgnoreCase))
            {
                reading.RespiratoryRate = GenerateByDirection(thresholds.Respiratory, direction);
                return;
            }

            if (vital.Equals("SystolicBloodPressure", StringComparison.OrdinalIgnoreCase))
            {
                reading.SystolicBloodPressure = GenerateByDirection(thresholds.Systolic, direction);
                return;
            }

            if (vital.Equals("DiastolicBloodPressure", StringComparison.OrdinalIgnoreCase))
            {
                reading.DiastolicBloodPressure = GenerateByDirection(thresholds.Diastolic, direction);
            }
        }

        private static float GenerateByDirection(VitalRange range, string direction)
        {
            if (direction.Equals("Spike", StringComparison.OrdinalIgnoreCase))
            {
                return GenerateSpikeValue(range);
            }

            if (direction.Equals("Dip", StringComparison.OrdinalIgnoreCase))
            {
                return GenerateDipValue(range);
            }

            return GenerateStableValue(range);
        }

        private static float GenerateStableValue(VitalRange range)
        {
            float width = range.Upper - range.Lower;
            float safeLower = range.Lower + width * 0.20f;
            float safeUpper = range.Upper - width * 0.20f;

            return RandomBetween(safeLower, safeUpper);
        }

        private static float GenerateSpikeValue(VitalRange range)
        {
            float width = Math.Max(range.Upper - range.Lower, 5f);

            return range.Upper + RandomBetween(
                Math.Max(1f, width * 0.10f),
                Math.Max(3f, width * 0.30f));
        }

        private static float GenerateDipValue(VitalRange range)
        {
            float width = Math.Max(range.Upper - range.Lower, 5f);

            return Math.Max(1f, range.Lower - RandomBetween(
                Math.Max(1f, width * 0.10f),
                Math.Max(3f, width * 0.30f)));
        }

        private static float RandomBetween(float minimum, float maximum)
        {
            if (maximum <= minimum)
            {
                return minimum;
            }

            return minimum + (float)Random.Shared.NextDouble() * (maximum - minimum);
        }

        private async Task SaveReadingAndUpdateBraceletAsync(int patientId, Vitals reading)
        {
            BraceletRelation? relation = await _context.BraceletRelations
                .Include(br => br.Bracelet)
                .FirstOrDefaultAsync(br => br.PatientID == patientId);

            if (relation?.Bracelet != null)
            {
                relation.Bracelet.HeartRate = reading.HeartRate;
                relation.Bracelet.RespiratoryRate = reading.RespiratoryRate;
                relation.Bracelet.SystolicBloodPressure = reading.SystolicBloodPressure;
                relation.Bracelet.DiastolicBloodPressure = reading.DiastolicBloodPressure;
                relation.Bracelet.Battery = Math.Max(0f, (relation.Bracelet.Battery ?? 100f) - 0.1f);
            }

            _context.Vitals.Add(reading);

            await _context.SaveChangesAsync();
        }

        private async Task<AlertResult> CreateAlertAndNotifyCaretakersAsync(
            Patients patient,
            Vitals reading,
            string selectedVital,
            PatientThresholdState thresholds)
        {
            VitalAlertDetails? alert = GetAlertDetails(reading, selectedVital, thresholds);

            if (alert == null)
            {
                return new AlertResult();
            }

            string eventPrefix = $"Vital alert - {alert.DisplayName}:";
            DateTime cooldownStart = DateTime.UtcNow.AddMinutes(-30);

            bool recentAlertExists = await _context.Logs.AnyAsync(log =>
                log.PatientID == patient.PatientID &&
                log.Emergency &&
                !log.Resolved &&
                log.Timestamp >= cooldownStart &&
                log.Event.StartsWith(eventPrefix));

            if (recentAlertExists)
            {
                return new AlertResult { CooldownActive = true };
            }

            string comparison = alert.Direction == "Dip"
                ? $"below the lower limit of {alert.Range.Lower:0.#} {alert.Unit}"
                : $"above the upper limit of {alert.Range.Upper:0.#} {alert.Unit}";

            _context.Logs.Add(new Log
            {
                UserID = patient.UserID,
                PatientID = patient.PatientID,
                Event = $"{eventPrefix} {alert.Direction}. Reading {alert.Value:0.#} {alert.Unit} was {comparison}.",
                Emergency = true,
                Resolved = false,
                selfAcknowledged = false,
                relativeAcknowledged = false,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();

            List<string> caretakerEmails = await GetCaretakerEmailsAsync(patient.PatientID, patient.UserID);

            string patientName = patient.User == null
                ? $"Patient #{patient.PatientID}"
                : $"{patient.User.FirstName} {patient.User.LastName}".Trim();

            DateTime recordedAtSingapore = ToSingaporeTime(reading.RecordedAt);

            string subject = $"Hospital Portal Alert - {alert.DisplayName} - {recordedAtSingapore:yyyyMMdd-HHmmss}";

            string body =
                "Hospital Portal vital notification\n\n" +
                $"Patient: {patientName}\n" +
                $"Vital: {alert.DisplayName}\n" +
                $"Condition: {alert.Direction}\n" +
                $"Reading: {alert.Value:0.#} {alert.Unit}\n" +
                $"Safe range: {alert.Range.Lower:0.#}-{alert.Range.Upper:0.#} {alert.Unit}\n" +
                $"Recorded at: {recordedAtSingapore:dd MMM yyyy, hh:mm tt}\n\n" +
                "Please sign in to the Hospital Portal to review and acknowledge the alert.\n\n" +
                "This is an automated notification.";

            int emailsSent = 0;

            foreach (string caretakerEmail in caretakerEmails)
            {
                bool sent = await _emailService.SendEmailAsync(caretakerEmail, subject, body);

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

        private async Task<List<string>> GetCaretakerEmailsAsync(int patientId, string patientUserId)
        {
            List<string?> emails = await _context.Relationships
                .Where(relationship => relationship.PatientID == patientId && relationship.UserID != patientUserId)
                .Join(
                    _context.Users,
                    relationship => relationship.UserID,
                    user => user.Id,
                    (relationship, user) => user.Email)
                .ToListAsync();

            return emails
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Select(email => email!.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static VitalAlertDetails? GetAlertDetails(Vitals reading, string vital, PatientThresholdState thresholds)
        {
            if (vital.Equals("HeartRate", StringComparison.OrdinalIgnoreCase))
            {
                return BuildAlertDetails("Heart Rate", "bpm", reading.HeartRate, thresholds.HeartRate);
            }

            if (vital.Equals("RespiratoryRate", StringComparison.OrdinalIgnoreCase))
            {
                return BuildAlertDetails("Breathing Rate", "breaths/min", reading.RespiratoryRate, thresholds.Respiratory);
            }

            if (vital.Equals("SystolicBloodPressure", StringComparison.OrdinalIgnoreCase))
            {
                return BuildAlertDetails("Systolic Blood Pressure", "mmHg", reading.SystolicBloodPressure, thresholds.Systolic);
            }

            if (vital.Equals("DiastolicBloodPressure", StringComparison.OrdinalIgnoreCase))
            {
                return BuildAlertDetails("Diastolic Blood Pressure", "mmHg", reading.DiastolicBloodPressure, thresholds.Diastolic);
            }

            return null;
        }

        private static VitalAlertDetails? BuildAlertDetails(string displayName, string unit, float? value, VitalRange range)
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

        private async Task<List<Vitals>> GetRecentVitalsAsync(int patientId, int count)
        {
            return await _context.Vitals
                .AsNoTracking()
                .Where(v => v.PatientID == patientId)
                .OrderByDescending(v => v.RecordedAt)
                .Take(count)
                .ToListAsync();
        }

        private static string GetReadableVitalName(string vital)
        {
            return vital switch
            {
                "HeartRate" => "Heart Rate",
                "RespiratoryRate" => "Breathing Rate",
                "SystolicBloodPressure" => "Systolic Blood Pressure",
                "DiastolicBloodPressure" => "Diastolic Blood Pressure",
                _ => vital
            };
        }

        private static DateTime ToSingaporeTime(DateTime utcDateTime)
        {
            DateTime utc = utcDateTime.Kind == DateTimeKind.Utc
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

            return TimeZoneInfo.ConvertTimeFromUtc(utc, singaporeTimeZone);
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
            public VitalRange HeartRate { get; set; } = new();
            public VitalRange Respiratory { get; set; } = new();
            public VitalRange Systolic { get; set; } = new();
            public VitalRange Diastolic { get; set; } = new();
        }

        private sealed class VitalAlertDetails
        {
            public string DisplayName { get; set; } = string.Empty;
            public string Unit { get; set; } = string.Empty;
            public float Value { get; set; }
            public string Direction { get; set; } = string.Empty;
            public VitalRange Range { get; set; } = new();
        }

        private sealed class AlertResult
        {
            public bool AlertCreated { get; set; }
            public bool CooldownActive { get; set; }
            public int CaretakerCount { get; set; }
            public int EmailsSent { get; set; }
        }
    }

    public sealed class VitalsSimulationOutcome
    {
        public bool IsError { get; private init; }
        public string Message { get; private init; } = string.Empty;

        public static VitalsSimulationOutcome Success(string message) => new() { IsError = false, Message = message };
        public static VitalsSimulationOutcome Error(string message) => new() { IsError = true, Message = message };
    }
}
