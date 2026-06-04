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

        public async Task AddLogAsync(string eventDescription, bool emergency = false, string? userId = null)
        {
            if (userId == null)
            {
                var adminUsers = await _userManager.GetUsersInRoleAsync("Admin");
                userId = adminUsers.FirstOrDefault()?.Id;
            }

            var nextId = await _context.Logs.AnyAsync()
                ? await _context.Logs.MaxAsync(l => l.LogID) + 1
                : 1;

            _context.Logs.Add(new Log
            {
                LogID = nextId,
                UserID = userId ?? string.Empty,
                Event = eventDescription,
                Emergency = emergency,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }
}