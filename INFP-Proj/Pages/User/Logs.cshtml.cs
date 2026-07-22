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

        public LogsModel(
            AppDbContext context,
            UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public string CurrentUserName { get; set; } =
            string.Empty;

        public bool IsPatient { get; set; }

        public IList<LogListItem> Logs { get; set; } =
            new List<LogListItem>();

        public async Task OnGetAsync()
        {
            string? currentUserId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return;
            }

            AppUser? user =
                await _userManager.FindByIdAsync(
                    currentUserId);

            CurrentUserName =
                user == null
                    ? "Your account"
                    : $"{user.FirstName} {user.LastName}".Trim();

            (int? linkedPatientId, bool isSelf) =
                await GetLinkedRoleAsync(currentUserId);

            IsPatient = isSelf;

            /*
             * Show:
             * 1. Logs belonging directly to the logged-in user.
             * 2. Emergency logs belonging to the linked patient.
             *
             * Both halves of the Union need matching Includes,
             * otherwise EF Core will not populate navigation
             * properties consistently across the combined set.
             */
            IQueryable<Log> query =
                _context.Logs
                    .AsNoTracking()
                    .Include(log => log.User)
                    .Include(log => log.Patient)
                        .ThenInclude(patient => patient!.User)
                    .Where(log =>
                        log.UserID == currentUserId);

            if (linkedPatientId.HasValue)
            {
                query = query.Union(
                    _context.Logs
                        .AsNoTracking()
                        .Include(log => log.User)
                        .Include(log => log.Patient)
                            .ThenInclude(patient => patient!.User)
                        .Where(log =>
                            log.Emergency &&
                            log.PatientID ==
                                linkedPatientId.Value));
            }

            List<Log> databaseLogs =
                await query
                    .OrderByDescending(log =>
                        log.Timestamp)
                    .ToListAsync();

            Logs = databaseLogs
                .Select(log =>
                {
                    bool eligibleToAcknowledge =
                        linkedPatientId.HasValue &&
                        log.PatientID ==
                            linkedPatientId.Value;

                    /*
                     * relativeAcknowledged is a single shared flag:
                     * true once ANY linked relative has acknowledged.
                     * There is no per-relative record anymore.
                     */
                    bool currentUserAcknowledged =
                        isSelf
                            ? log.selfAcknowledged
                            : log.relativeAcknowledged;

                    return new LogListItem
                    {
                        LogId = log.LogID,
                        UserName =
                            log.User == null
                                ? string.Empty
                                : $"{log.User.FirstName} {log.User.LastName}".Trim(),
                        Event = log.Event,
                        Emergency = log.Emergency,
                        Resolved = log.Resolved,
                        Timestamp = log.Timestamp,
                        IsMedicationRequest = log.MedicationListID.HasValue,

                        SelfAcknowledged =
                            log.selfAcknowledged,

                        RelativeAcknowledged =
                            log.relativeAcknowledged,

                        AcknowledgedAt =
                            log.AcknowledgedAt,

                        PatientName =
                            log.Patient?.User == null
                                ? null
                                : $"{log.Patient.User.FirstName} {log.Patient.User.LastName}".Trim(),

                        CurrentUserAcknowledged =
                            currentUserAcknowledged,

                        CanAcknowledge =
                            log.Emergency &&
                            !log.Resolved &&
                            !currentUserAcknowledged &&
                            eligibleToAcknowledge
                    };
                })
                .ToList();
        }

        /*
         * Supports your existing form:
         * asp-page-handler="Acknowledge"
         * name/id parameter: id
         */
        public Task<IActionResult>
            OnPostAcknowledgeAsync(int id)
        {
            return AcknowledgeEmergencyAsync(id);
        }

        /*
         * Also supports:
         * asp-page-handler="AcknowledgeEmergency"
         * route parameter: logId
         */
        public Task<IActionResult>
            OnPostAcknowledgeEmergencyAsync(int logId)
        {
            return AcknowledgeEmergencyAsync(logId);
        }

        private async Task<IActionResult>
            AcknowledgeEmergencyAsync(int logId)
        {
            string? currentUserId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                return RedirectToPage("/Login");
            }

            Log? emergencyLog =
                await _context.Logs
                    .FirstOrDefaultAsync(log =>
                        log.LogID == logId &&
                        log.Emergency &&
                        !log.Resolved);

            if (emergencyLog == null ||
                !emergencyLog.PatientID.HasValue)
            {
                TempData["ErrorMessage"] =
                    "The emergency could not be found " +
                    "or has already been resolved.";

                return RedirectToPage();
            }

            (int? linkedPatientId, bool isSelf) =
                await GetLinkedRoleAsync(currentUserId);

            bool authorised =
                linkedPatientId.HasValue &&
                linkedPatientId.Value ==
                    emergencyLog.PatientID.Value;

            if (!authorised)
            {
                return Forbid();
            }

            /*
             * Patient acknowledgement.
             */
            if (isSelf)
            {
                if (emergencyLog.selfAcknowledged)
                {
                    TempData["Message"] =
                        "You have already acknowledged " +
                        "this emergency.";

                    return RedirectToPage();
                }

                emergencyLog.selfAcknowledged = true;
                emergencyLog.AcknowledgedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["Message"] =
                    "Emergency acknowledged successfully.";

                return RedirectToPage();
            }

            /*
             * A relative must have an exact Relationships row
             * linking their account to this patient.
             */
            bool isLinkedRelative =
                await _context.Relationships
                    .AsNoTracking()
                    .AnyAsync(relationship =>
                        relationship.PatientID ==
                            emergencyLog.PatientID.Value &&
                        relationship.UserID ==
                            currentUserId);

            if (!isLinkedRelative)
            {
                TempData["ErrorMessage"] =
                    "You are not authorised to acknowledge " +
                    "this patient's emergency.";

                return RedirectToPage();
            }

            /*
             * relativeAcknowledged is now a single shared flag on
             * the Log itself. The first linked relative to
             * acknowledge locks it for the group.
             */
            if (emergencyLog.relativeAcknowledged)
            {
                TempData["Message"] =
                    "This emergency has already been " +
                    "acknowledged by a relative.";

                return RedirectToPage();
            }

            emergencyLog.relativeAcknowledged = true;
            emergencyLog.AcknowledgedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Message"] =
                "Emergency acknowledged successfully.";

            return RedirectToPage();
        }

        private async Task<(int? PatientId, bool IsSelf)>
            GetLinkedRoleAsync(string currentUserId)
        {
            int? ownPatientId =
                await _context.Patients
                    .AsNoTracking()
                    .Where(patient =>
                        patient.UserID ==
                            currentUserId)
                    .Select(patient =>
                        (int?)patient.PatientID)
                    .FirstOrDefaultAsync();

            if (ownPatientId.HasValue)
            {
                return (
                    ownPatientId,
                    true
                );
            }

            int? relativePatientId =
                await _context.Relationships
                    .AsNoTracking()
                    .Where(relationship =>
                        relationship.UserID ==
                            currentUserId)
                    .Select(relationship =>
                        (int?)relationship.PatientID)
                    .FirstOrDefaultAsync();

            return (
                relativePatientId,
                false
            );
        }
    }
}