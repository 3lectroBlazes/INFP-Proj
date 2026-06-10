using INFP_Proj.Data;
using INFP_Proj.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Services
{
    public class AdminLogService
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public AdminLogService(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task AddLogAsync(string eventDescription, bool emergency = false, string? userId = null, int patientUser = 0, int medication = 0, string? dosage = null)
        {
            if (string.IsNullOrEmpty(userId))
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                userId = adminUsers.FirstOrDefault()?.Id;

                if (userId == null)
                {
                    foreach (var role in new[] { "Nurse", "Doctor", "Reception" })
                    {
                        var staffUsers = await _userManager.GetUsersInRoleAsync(role);
                        userId = staffUsers.FirstOrDefault()?.Id;
                        if (userId != null)
                        {
                            break;
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            if (medication != 0 && patientUser != 0)
            {
                _context.Logs.Add(new Log
                {
                    UserID = userId,
                    PatientID = patientUser,
                    Event = eventDescription,
                    MedicationID = medication,
                    Dosage = dosage,
                    Emergency = emergency,
                    Timestamp = DateTime.UtcNow
                });
            }
            else
                _context.Logs.Add(new Log
            {
                UserID = userId,
                Event = eventDescription,
                Emergency = emergency,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }
}