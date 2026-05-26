using INFP_Proj.Models;
using INFP_Proj.Data;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin
{
    public class LogsModel : PageModel
    {
        private readonly AppDbContext _context;

        public LogsModel(AppDbContext context)
        {
            _context = context;
        }

        public IList<LogListItem> Logs { get; set; } = new List<LogListItem>();

        public async Task OnGetAsync()
        {
            Logs = await _context.Logs
                .Include(l => l.User)
                .OrderByDescending(l => l.Timestamp)
                .Select(l => new LogListItem
                {
                    LogId = l.LogID,
                    UserName = l.User != null
                        ? $"{l.User.FirstName} {l.User.LastName}"
                        : $"User #{l.UserID}",
                    Event = l.Event,
                    Emergency = l.Emergency,
                    Timestamp = l.Timestamp
                })
                .ToListAsync();
        }
    }
}
