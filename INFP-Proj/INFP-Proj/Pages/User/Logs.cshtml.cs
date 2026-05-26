using INFP_Proj.Data;
using INFP_Proj.Models;
using INFP_Proj.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.User
{
    public class LogsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserContextService _userContextService;

        public LogsModel(AppDbContext context, UserContextService userContextService)
        {
            _context = context;
            _userContextService = userContextService;
        }

        public string CurrentUserName { get; set; } = string.Empty;
        public IList<LogListItem> Logs { get; set; } = new List<LogListItem>();

        public async Task OnGetAsync()
        {
            var currentUserId = await _userContextService.GetCurrentUserIdAsync();

            var user = await _context.Users.FindAsync(currentUserId);
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
