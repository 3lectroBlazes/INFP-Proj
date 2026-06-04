using INFP_Proj.Data;
using INFP_Proj.Models;
using INFP_Proj.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.User
{
    public class LogsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserContextService _userContextService;
        private readonly UserManager<AppUser> _userManager;

        public LogsModel(AppDbContext context, UserContextService userContextService, UserManager<AppUser> userManager)
        {
            _context = context;
            _userContextService = userContextService;
            _userManager = userManager;
        }

        public string CurrentUserName { get; set; } = string.Empty;
        public IList<LogListItem> Logs { get; set; } = new List<LogListItem>();

        public async Task OnGetAsync()
        {
            var currentUserId = await _userContextService.GetCurrentUserIdAsync();

            // Use UserManager instead of _context.Users
            var user = await _userManager.FindByIdAsync(currentUserId);
            CurrentUserName = user != null
                ? $"{user.FirstName} {user.LastName}"
                : "Your account";

            Logs = await _context.Logs
                .Where(l => l.UserID == currentUserId)
                .OrderByDescending(l => l.Timestamp)
                .Select(l => new LogListItem
                {
                    LogId = l.LogID,
                    Event = l.Event,
                    Emergency = l.Emergency,
                    Timestamp = l.Timestamp
                })
                .ToListAsync();
        }
    }
}