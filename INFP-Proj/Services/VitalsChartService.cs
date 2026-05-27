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
    }
}
