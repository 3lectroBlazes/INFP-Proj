using INFP_Proj.Data;
using INFP_Proj.Models;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.User
{
    [Authorize]
    public class LogsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public LogsModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public string CurrentUserName { get; set; } = string.Empty;
        public IList<LogListItem> Logs { get; set; } = new List<LogListItem>();

        public async Task OnGetAsync()
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return;
            }

            var user = await _userManager.FindByIdAsync(currentUserId);
            CurrentUserName = user != null
                ? $"{user.FirstName} {user.LastName}"
                : "Your account";

            var (linkedPatientId, isSelf) = await GetLinkedRoleAsync(currentUserId);

            var query = _context.Logs.Where(l => l.UserID == currentUserId);

            if (linkedPatientId.HasValue)
            {
                query = query.Union(_context.Logs.Where(l => l.Emergency && l.PatientID == linkedPatientId.Value));
            }

            var logs = await query.OrderByDescending(l => l.Timestamp).ToListAsync();

            Logs = logs.Select(l =>
            {
                bool currentUserAcknowledged = isSelf ? l.selfAcknowledged : l.relativeAcknowledged;
                bool eligibleToAcknowledge = linkedPatientId.HasValue && l.PatientID == linkedPatientId.Value;

                return new LogListItem
                {
                    LogId = l.LogID,
                    Event = l.Event,
                    Emergency = l.Emergency,
                    Resolved = l.Resolved,
                    Timestamp = l.Timestamp,
                    SelfAcknowledged = l.selfAcknowledged,
                    RelativeAcknowledged = l.relativeAcknowledged,
                    CurrentUserAcknowledged = currentUserAcknowledged,
                    CanAcknowledge = l.Emergency && !l.Resolved && !currentUserAcknowledged && eligibleToAcknowledge
                };
            }).ToList();
        }

        public async Task<IActionResult> OnPostAcknowledgeAsync(int id)
        {
            var currentUserId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(currentUserId))
            {
                return RedirectToPage("/Login");
            }

            var log = await _context.Logs.FirstOrDefaultAsync(l => l.LogID == id);
            if (log == null || !log.Emergency || log.Resolved)
            {
                return RedirectToPage();
            }

            var (linkedPatientId, isSelf) = await GetLinkedRoleAsync(currentUserId);

            bool authorized = linkedPatientId.HasValue && log.PatientID == linkedPatientId.Value;
            if (!authorized)
            {
                return Forbid();
            }

            if (isSelf)
            {
                log.selfAcknowledged = true;
            }
            else
            {
                log.relativeAcknowledged = true;
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Emergency acknowledged.";
            return RedirectToPage();
        }

        private async Task<(int? PatientId, bool IsSelf)> GetLinkedRoleAsync(string currentUserId)
        {
            var ownPatientId = await _context.Patients
                .Where(p => p.UserID == currentUserId)
                .Select(p => (int?)p.PatientID)
                .FirstOrDefaultAsync();

            if (ownPatientId.HasValue)
            {
                return (ownPatientId, true);
            }

            var relativePatientId = await _context.Relationships
                .Where(r => r.UserID == currentUserId)
                .Select(r => (int?)r.PatientID)
                .FirstOrDefaultAsync();

            return (relativePatientId, false);
        }
    }
}
