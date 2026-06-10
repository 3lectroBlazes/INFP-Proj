using INFP_Proj.Data;
using INFP_Proj.Models;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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