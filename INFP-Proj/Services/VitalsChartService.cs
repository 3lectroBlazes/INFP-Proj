using INFP_Proj.Data;
using INFP_Proj.Models;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Services
{
    public class VitalsChartService
    {
        private readonly AppDbContext _context;

        public VitalsChartService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<VitalsChartViewModel> BuildChartModelAsync(
            int patientId,
            bool showPatientSelector = false)
        {
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.PatientID == patientId);

            var vitals = await _context.Vitals
                .Where(v => v.PatientID == patientId)
                .OrderBy(v => v.RecordedAt)
                .ToListAsync();

            var model = new VitalsChartViewModel
            {
                PatientId = patientId,
                PatientName = patient?.User != null
                    ? $"{patient.User.FirstName} {patient.User.LastName}"
                    : $"Patient #{patientId}",
                ShowPatientSelector = showPatientSelector,
                Labels = vitals.Select(v => v.RecordedAt.ToLocalTime().ToString("MMM d, HH:mm")).ToList(),
                HeartRate = vitals.Select(v => v.HeartRate).ToList(),
                RespiratoryRate = vitals.Select(v => v.RespiratoryRate).ToList(),
                BloodPressure = vitals.Select(v => v.BloodPressure).ToList(),
                Temperature = vitals.Select(v => v.Temperature).ToList()
            };

            if (showPatientSelector)
            {
                model.Patients = await _context.Patients
                    .Include(p => p.User)
                    .OrderBy(p => p.PatientID)
                    .Select(p => new PatientSelectItem
                    {
                        PatientId = p.PatientID,
                        DisplayName = p.User != null
                            ? $"{p.User.FirstName} {p.User.LastName}"
                            : $"Patient #{p.PatientID}"
                    })
                    .ToListAsync();
            }

            return model;
        }

        public async Task<AdminVitalsChartViewModel> BuildAdminMultiPatientChartAsync(IList<int> selectedPatientIds)
        {
            var allPatients = await _context.Patients
                .Include(p => p.User)
                .OrderBy(p => p.PatientID)
                .Select(p => new PatientSelectItem
                {
                    PatientId = p.PatientID,
                    DisplayName = p.User != null
                        ? $"{p.User.FirstName} {p.User.LastName}"
                        : $"Patient #{p.PatientID}"
                })
                .ToListAsync();

            var ids = selectedPatientIds
                .Where(id => allPatients.Any(p => p.PatientId == id))
                .Distinct()
                .ToList();

            if (ids.Count == 0 && allPatients.Count > 0)
            {
                ids.Add(allPatients[0].PatientId);
            }

            var model = new AdminVitalsChartViewModel
            {
                Patients = allPatients,
                SelectedPatientIds = ids
            };

            if (ids.Count == 0)
            {
                return model;
            }

            var vitals = await _context.Vitals
                .Where(v => ids.Contains(v.PatientID))
                .OrderBy(v => v.RecordedAt)
                .ToListAsync();

            var timeline = vitals
                .Select(v => v.RecordedAt)
                .Distinct()
                .OrderBy(t => t)
                .ToList();

            model.Labels = timeline
                .Select(t => t.ToLocalTime().ToString("MMM d, HH:mm"))
                .ToList();

            foreach (var patientId in ids)
            {
                var patientInfo = allPatients.First(p => p.PatientId == patientId);
                var byTime = vitals
                    .Where(v => v.PatientID == patientId)
                    .GroupBy(v => v.RecordedAt)
                    .ToDictionary(g => g.Key, g => g.First());

                model.Series.Add(new PatientVitalsSeries
                {
                    PatientId = patientId,
                    PatientName = patientInfo.DisplayName,
                    HeartRate = timeline.Select(t => byTime.TryGetValue(t, out var v) ? v.HeartRate : null).ToList(),
                    RespiratoryRate = timeline.Select(t => byTime.TryGetValue(t, out var v) ? v.RespiratoryRate : null).ToList(),
                    BloodPressure = timeline.Select(t => byTime.TryGetValue(t, out var v) ? v.BloodPressure : null).ToList(),
                    Temperature = timeline.Select(t => byTime.TryGetValue(t, out var v) ? v.Temperature : null).ToList()
                });
            }

            return model;
        }
    }
}
