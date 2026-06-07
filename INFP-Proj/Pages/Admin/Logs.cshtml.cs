using INFP_Proj.Models;
using INFP_Proj.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
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
        public SelectList UserOptions { get; set; } = default!;

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [BindProperty(SupportsGet = true)]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? UserId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string EmergencyFilter { get; set; } = "all";

        public async Task OnGetAsync()
        {
            await PopulateUserOptionsAsync();

            var query = _context.Logs
                .Include(l => l.User)
                .AsQueryable();

            if (FromDate.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(FromDate.Value.Date, DateTimeKind.Local).ToUniversalTime();
                query = query.Where(l => l.Timestamp >= fromUtc);
            }

            if (ToDate.HasValue)
            {
                // Inclusive of the entire selected day.
                var toUtc = DateTime.SpecifyKind(ToDate.Value.Date.AddDays(1), DateTimeKind.Local).ToUniversalTime();
                query = query.Where(l => l.Timestamp < toUtc);
            }

            if (!string.IsNullOrEmpty(UserId))
            {
                query = query.Where(l => l.UserID == UserId);
            }

            if (string.Equals(EmergencyFilter, "yes", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(l => l.Emergency);
            }
            else if (string.Equals(EmergencyFilter, "no", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(l => !l.Emergency);
            }

            Logs = await query
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

        private async Task PopulateUserOptionsAsync()
        {
            var users = await _context.Logs
                .Where(l => l.User != null)
                .Select(l => new
                {
                    Id = l.UserID,
                    Name = l.User!.FirstName + " " + l.User.LastName
                })
                .Distinct()
                .OrderBy(u => u.Name)
                .ToListAsync();

            UserOptions = new SelectList(users, "Id", "Name", UserId);
        }
    }
}
