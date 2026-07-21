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
             */
            IQueryable<Log> query =
                _context.Logs
                    .AsNoTracking()
                    .Where(log =>
                        log.UserID == currentUserId);

            if (linkedPatientId.HasValue)
            {
                query = query.Union(
                    _context.Logs
                        .AsNoTracking()
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

            List<int> logIds =
                databaseLogs
                    .Select(log => log.LogID)
                    .ToList();

            /*
             * Load individual relative acknowledgements
             * for all displayed emergency logs.
             */
            List<LogAcknowledgement> acknowledgements =
                logIds.Count == 0
                    ? new List<LogAcknowledgement>()
                    : await _context.LogAcknowledgements
                        .AsNoTracking()
                        .Where(acknowledgement =>
                            logIds.Contains(
                                acknowledgement.LogID))
                        .ToListAsync();

            /*
             * All users linked through Relationships are currently
             * treated as relatives/caretakers for this patient.
             */
            List<string> relativeUserIds =
                linkedPatientId.HasValue
                    ? await _context.Relationships
                        .AsNoTracking()
                        .Where(relationship =>
                            relationship.PatientID ==
                                linkedPatientId.Value)
                        .Select(relationship =>
                            relationship.UserID)
                        .Distinct()
                        .ToListAsync()
                    : new List<string>();

            HashSet<string> relativeUserIdSet =
                relativeUserIds.ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

            Logs = databaseLogs
                .Select(log =>
                {
                    bool eligibleToAcknowledge =
                        linkedPatientId.HasValue &&
                        log.PatientID ==
                            linkedPatientId.Value;

                    bool currentRelativeAcknowledged =
                        !isSelf &&
                        acknowledgements.Any(
                            acknowledgement =>
                                acknowledgement.LogID ==
                                    log.LogID &&
                                acknowledgement.UserID ==
                                    currentUserId);

                    bool currentUserAcknowledged =
                        isSelf
                            ? log.selfAcknowledged
                            : currentRelativeAcknowledged;

                    int relativeAcknowledgementCount =
                        acknowledgements
                            .Where(acknowledgement =>
                                acknowledgement.LogID ==
                                    log.LogID &&
                                relativeUserIdSet.Contains(
                                    acknowledgement.UserID))
                            .Select(acknowledgement =>
                                acknowledgement.UserID)
                            .Distinct(
                                StringComparer.OrdinalIgnoreCase)
                            .Count();

                    return new LogListItem
                    {
                        LogId = log.LogID,
                        Event = log.Event,
                        Emergency = log.Emergency,
                        Resolved = log.Resolved,
                        Timestamp = log.Timestamp,

                        SelfAcknowledged =
                            log.selfAcknowledged,

                        /*
                         * This now means at least one relative
                         * has acknowledged the emergency.
                         */
                        RelativeAcknowledged =
                            relativeAcknowledgementCount > 0,

                        CurrentUserAcknowledged =
                            currentUserAcknowledged,

                        CurrentRelativeAcknowledged =
                            currentRelativeAcknowledged,

                        RelativeAcknowledgementCount =
                            relativeAcknowledgementCount,

                        TotalRelativeCount =
                            relativeUserIds.Count,

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
             * Patient acknowledgement remains in the existing
             * selfAcknowledged column.
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

            bool alreadyAcknowledged =
                await _context.LogAcknowledgements
                    .AsNoTracking()
                    .AnyAsync(acknowledgement =>
                        acknowledgement.LogID ==
                            logId &&
                        acknowledgement.UserID ==
                            currentUserId);

            if (alreadyAcknowledged)
            {
                TempData["Message"] =
                    "You have already acknowledged " +
                    "this emergency.";

                return RedirectToPage();
            }

            _context.LogAcknowledgements.Add(
                new LogAcknowledgement
                {
                    LogID = logId,
                    UserID = currentUserId,
                    AcknowledgedAt = DateTime.UtcNow
                });

            /*
             * Keep the old shared Boolean updated for compatibility
             * with any existing teammate code.
             *
             * It now means:
             * "At least one relative acknowledged."
             *
             * The page itself uses LogAcknowledgements to identify
             * each individual relative.
             */
            emergencyLog.relativeAcknowledged = true;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["Message"] =
                    "You have already acknowledged " +
                    "this emergency.";

                return RedirectToPage();
            }

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