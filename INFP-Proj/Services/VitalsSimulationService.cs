using INFP_Proj.Data;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Services
{
    public class VitalsSimulationService
    {
        public static readonly string[] AllowedVitals = { "HeartRate", "RespiratoryRate", "BloodPressure" };
        public static readonly string[] AllowedDirections = { "Spike", "Dip" };

        private readonly AppDbContext _context;

        public VitalsSimulationService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Vitals?> RecordSimulatedReadingAsync(int patientId, string vital, string direction)
        {
            if (!AllowedVitals.Contains(vital) || !AllowedDirections.Contains(direction))
            {
                return null;
            }

            bool patientExists = await _context.Patients.AnyAsync(p => p.PatientID == patientId);
            if (!patientExists)
            {
                return null;
            }

            Vitals? last = await _context.Vitals
                .Where(v => v.PatientID == patientId)
                .OrderByDescending(v => v.RecordedAt)
                .FirstOrDefaultAsync();

            float baselineHeartRate = last?.HeartRate ?? 72f;
            float baselineRespiratoryRate = last?.RespiratoryRate ?? 16f;
            float baselineBloodPressure = last?.BloodPressure ?? 120f;
            float baselineTemperature = last?.Temperature ?? 36.5f;

            bool spike = direction == "Spike";

            var reading = new Vitals
            {
                PatientID = patientId,
                HeartRate = Jitter(baselineHeartRate, 2f),
                RespiratoryRate = Jitter(baselineRespiratoryRate, 1f),
                BloodPressure = Jitter(baselineBloodPressure, 2f),
                Temperature = Jitter(baselineTemperature, 0.1f),
                RecordedAt = DateTime.UtcNow
            };

            switch (vital)
            {
                case "HeartRate":
                    reading.HeartRate = spike
                        ? baselineHeartRate + 45f + (float)Random.Shared.NextDouble() * 15f
                        : baselineHeartRate - 30f - (float)Random.Shared.NextDouble() * 10f;
                    break;
                case "RespiratoryRate":
                    reading.RespiratoryRate = spike
                        ? baselineRespiratoryRate + 12f + (float)Random.Shared.NextDouble() * 6f
                        : baselineRespiratoryRate - 8f - (float)Random.Shared.NextDouble() * 4f;
                    break;
                case "BloodPressure":
                    reading.BloodPressure = spike
                        ? baselineBloodPressure + 35f + (float)Random.Shared.NextDouble() * 15f
                        : baselineBloodPressure - 30f - (float)Random.Shared.NextDouble() * 10f;
                    break;
            }

            // Clamp so dips can't go negative/implausible.
            reading.HeartRate = Math.Max(20f, reading.HeartRate.Value);
            reading.RespiratoryRate = Math.Max(4f, reading.RespiratoryRate.Value);
            reading.BloodPressure = Math.Max(40f, reading.BloodPressure.Value);

            _context.Vitals.Add(reading);
            await _context.SaveChangesAsync();
            return reading;
        }

        private static float Jitter(float baseline, float amount)
        {
            return baseline + ((float)Random.Shared.NextDouble() * 2f - 1f) * amount;
        }
    }
}
