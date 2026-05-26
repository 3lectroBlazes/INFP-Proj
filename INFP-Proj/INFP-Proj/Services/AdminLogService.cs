using INFP_Proj.Data;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Services
{
    public class AdminLogService
    {
        // Default staff user for admin actions until authentication is implemented.
        public const int DefaultStaffUserId = 1;

        private readonly AppDbContext _context;

        public AdminLogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddLogAsync(string eventDescription, bool emergency = false, int? userId = null)
        {
            var nextId = await _context.Logs.AnyAsync()
                ? await _context.Logs.MaxAsync(l => l.LogID) + 1
                : 1;

            _context.Logs.Add(new Log
            {
                LogID = nextId,
                UserID = userId ?? DefaultStaffUserId,
                Event = eventDescription,
                Emergency = emergency,
                Timestamp = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
    }
}
