using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using INFP_Proj.Data;

namespace INFP_Proj.Services
{
    public class UserService : IUserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<string>> GetCareTeamAndFamilyIdsAsync(int patientId)
        {
            var patient = await _context.Patients.FindAsync(patientId);
            var users = new List<string>();

            if (patient != null && !string.IsNullOrEmpty(patient.UserID))
            {
                users.Add(patient.UserID);
            }

            return users;
        }
    }

    public class ConsoleNotificationService : INotificationService
    {
        public Task SendAlertAsync(IEnumerable<string> userIds, string subject, string message)
        {
            Debug.WriteLine("VITALS ALERT TRIGGERED");
            Debug.WriteLine($"Subject: {subject}");
            Debug.WriteLine($"Message: {message}");
            Debug.WriteLine($"Notifying User IDs: {string.Join(", ", userIds)}");

            return Task.CompletedTask;
        }
    }
}