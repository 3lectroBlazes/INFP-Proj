using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using INFP_Proj.Data;

namespace INFP_Proj.Services
{
    public class VitalsAlertService
    {
        private readonly AppDbContext _context;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;

        public VitalsAlertService(
            AppDbContext context,
            INotificationService notificationService,
            IUserService userService)
        {
            _context = context;
            _notificationService = notificationService;
            _userService = userService;
        }

        public async Task EvaluateVitalsAsync(Vitals currentVitals)
        {
            var thresholds = await _context.Thresholds.FirstOrDefaultAsync();

            if (thresholds == null) return;

            var alerts = new List<string>();

            if (currentVitals.SystolicBloodPressure > thresholds.SBPUpperThreshold)
                alerts.Add($"Systolic BP is critically high ({currentVitals.SystolicBloodPressure} mmHg).");

            if (currentVitals.SystolicBloodPressure < thresholds.SBPLowerThreshold)
                alerts.Add($"Systolic BP is critically low ({currentVitals.SystolicBloodPressure} mmHg).");

            if (currentVitals.DiastolicBloodPressure > thresholds.DBPUpperThreshold)
                alerts.Add($"Diastolic BP is critically high ({currentVitals.DiastolicBloodPressure} mmHg).");

            if (currentVitals.DiastolicBloodPressure < thresholds.DBPLowerThreshold)
                alerts.Add($"Diastolic BP is critically low ({currentVitals.DiastolicBloodPressure} mmHg).");

            var previousVitals = await _context.Vitals
                .Where(v => v.PatientID == currentVitals.PatientID && v.RecordedAt < currentVitals.RecordedAt)
                .OrderByDescending(v => v.RecordedAt)
                .FirstOrDefaultAsync();

            if (previousVitals != null)
            {
                if (currentVitals.HeartRate.HasValue && previousVitals.HeartRate.HasValue && previousVitals.HeartRate > 0)
                {
                    float hrChangePercent = ((currentVitals.HeartRate.Value - previousVitals.HeartRate.Value) / previousVitals.HeartRate.Value) * 100;

                    if (thresholds.HeartRateUpperPercentageThreshold.HasValue && hrChangePercent >= thresholds.HeartRateUpperPercentageThreshold)
                        alerts.Add($"Heart Rate spiked by {hrChangePercent:F1}% to {currentVitals.HeartRate} bpm.");

                    if (thresholds.HeartRateLowerPercentageThreshold.HasValue && hrChangePercent <= -thresholds.HeartRateLowerPercentageThreshold)
                        alerts.Add($"Heart Rate dropped by {Math.Abs(hrChangePercent):F1}% to {currentVitals.HeartRate} bpm.");
                }

                if (currentVitals.RespiratoryRate.HasValue && previousVitals.RespiratoryRate.HasValue && previousVitals.RespiratoryRate > 0)
                {
                    float rrChangePercent = ((currentVitals.RespiratoryRate.Value - previousVitals.RespiratoryRate.Value) / previousVitals.RespiratoryRate.Value) * 100;

                    if (thresholds.RespiratoryRateUpperPercentageThreshold.HasValue && rrChangePercent >= thresholds.RespiratoryRateUpperPercentageThreshold)
                        alerts.Add($"Respiratory Rate spiked by {rrChangePercent:F1}% to {currentVitals.RespiratoryRate} breaths/min.");
                }
            }

            if (currentVitals.RespiratoryRate < thresholds.RespiratoryRateLowerThreshold)
                alerts.Add($"Respiratory Rate is critically low ({currentVitals.RespiratoryRate} breaths/min).");

            if (alerts.Any())
            {
                var patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.PatientID == currentVitals.PatientID);

                string patientName = patient?.User?.UserName ?? $"ID {currentVitals.PatientID}";

                string alertSubject = $"URGENT: Vitals Alert for Patient {patientName}";
                string alertMessage = $"The following vitals triggered an alarm at {currentVitals.RecordedAt:HH:mm}:\n\n- "
                                      + string.Join("\n- ", alerts);

                var usersToNotify = await _userService.GetCareTeamAndFamilyIdsAsync(currentVitals.PatientID);

                if (usersToNotify != null && usersToNotify.Any())
                {
                    await _notificationService.SendAlertAsync(usersToNotify, alertSubject, alertMessage);
                }
            }
        }
    }
}