using INFP_Proj.Data;
using INFP_Proj.Services;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using System.ComponentModel.DataAnnotations;

namespace INFP_Proj.Pages.Admin
{
    public class LogsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly AdminLogService _adminLogService;

        public LogsModel(AppDbContext context, AdminLogService adminLogService)
        {
            _context = context;
            _adminLogService = adminLogService;
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
                .Include(l => l.Patient)
                    .ThenInclude(p => p!.User)
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
                    Resolved = l.Resolved,
                    Timestamp = l.Timestamp,
                    SelfAcknowledged = l.selfAcknowledged,
                    RelativeAcknowledged = l.relativeAcknowledged,
                    PatientName = l.Patient != null && l.Patient.User != null
                        ? $"{l.Patient.User.FirstName} {l.Patient.User.LastName}"
                        : null
                })
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostResolveAsync(int id)
        {
            var log = await _context.Logs.FirstOrDefaultAsync(l => l.LogID == id);
            if (log != null && log.Emergency && !log.Resolved)
            {
                if (log.selfAcknowledged || log.relativeAcknowledged)
                {
                    log.Resolved = true;
                    await _context.SaveChangesAsync();
                    TempData["Message"] = "Emergency marked as resolved.";
                }
                else
                {
                    TempData["Message"] = "This emergency can't be resolved yet — it needs to be acknowledged by the patient or a relative first.";
                }
            }
            return RedirectToPage(new { FromDate, ToDate, UserId, EmergencyFilter });
        }

        public async Task<IActionResult> OnPostSelfAcknowledgeAsync(int id)
        {
            if (!User.IsInRole("Doctor") && !User.IsInRole("Nurse"))
            {
                return Forbid();
            }

            var log = await _context.Logs.FirstOrDefaultAsync(l => l.LogID == id);
            if (log != null && log.Emergency && !log.Resolved && !log.selfAcknowledged)
            {
                log.selfAcknowledged = true;
                await _context.SaveChangesAsync();
                TempData["Message"] = "Marked as acknowledged on behalf of the patient.";
            }
            return RedirectToPage(new { FromDate, ToDate, UserId, EmergencyFilter });
        }
        private async Task AddPatientLogIfLinkedAsync(int patientId, string message)
        {
            var userId = await _context.Patients
                .Where(p => p.PatientID == patientId)
                .Select(p => p.UserID)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(userId))
            {
                await _adminLogService.AddLogAsync(message, userId: userId);
            }
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
