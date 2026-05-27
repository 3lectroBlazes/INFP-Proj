using INFP_Proj.Data;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Services
{
    /// <summary>
    /// Resolves the current user until authentication is implemented.
    /// </summary>
    public class UserContextService
    {
        // Demo patient user "Sadev IDK" linked to PatientID 1.
        public const string DemoUserId = "your-appuser-id-here"; // changed to string
        public const int DemoPatientId = 1;

        private readonly AppDbContext _context;

        public UserContextService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<string> GetCurrentUserIdAsync() // changed return type to string
        {
            var userId = await _context.Patients
                .Where(p => p.PatientID == DemoPatientId)
                .Select(p => p.UserID)
                .FirstOrDefaultAsync();

            return !string.IsNullOrEmpty(userId) ? userId : DemoUserId; // changed check
        }
    }
}