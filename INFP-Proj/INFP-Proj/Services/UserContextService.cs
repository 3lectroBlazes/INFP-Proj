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
        public const int DemoUserId = 4;
        public const int DemoPatientId = 1;

        private readonly AppDbContext _context;

        public UserContextService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<int> GetCurrentUserIdAsync()
        {
            var userId = await _context.Patients
                .Where(p => p.PatientID == DemoPatientId)
                .Select(p => p.UserID)
                .FirstOrDefaultAsync();

            return userId > 0 ? userId : DemoUserId;
        }
    }
}
