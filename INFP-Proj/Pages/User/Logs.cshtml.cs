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
        private const string SelectedPatientSessionKey =
            "SelectedPatientId";

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

        public bool HasSelectedPatient { get; set; }

        public string SelectedPatientName { get; set; } =
            string.Empty;

        public IList<LogListItem> Logs { get; set; } =
            new List<LogListItem>();


        // =========================================================
        // PAGE LOAD
        // =========================================================

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


            /*
             * Get the patient explicitly selected
             * on the Dashboard.
             *
             * If this account is itself a patient and
             * nothing has been selected yet, its own
             * patient record is used automatically.
             */
            (Patients? selectedPatient, bool isSelf) =
                await GetSelectedPatientRoleAsync(
                    currentUserId);


            if (selectedPatient == null)
            {
                HasSelectedPatient = false;

                /*
                 * There may still be normal activity logs
                 * belonging directly to this user's account.
                 */
                Logs =
                    await LoadUserOnlyLogsAsync(
                        currentUserId);

                return;
            }


            HasSelectedPatient = true;

            IsPatient = isSelf;

            SelectedPatientName =
                selectedPatient.User == null
                    ? $"Patient #{selectedPatient.PatientID}"
                    : $"{selectedPatient.User.FirstName} {selectedPatient.User.LastName}"
                        .Trim();


            int linkedPatientId =
                selectedPatient.PatientID;


            /*
             * Show:
             *
             * 1. Logs belonging directly to the logged-in user.
             *
             * 2. Emergency logs belonging to the patient
             *    currently selected on the Dashboard.
             */
            IQueryable<Log> query =
                _context.Logs
                    .AsNoTracking()
                    .Where(log =>
                        log.UserID ==
                        currentUserId);


            query =
                query.Union(
                    _context.Logs
                        .AsNoTracking()
                        .Where(log =>
                            log.Emergency
                            &&
                            log.PatientID ==
                            linkedPatientId));


            List<Log> databaseLogs =
                await query
                    .OrderByDescending(log =>
                        log.Timestamp)
                    .ToListAsync();


            List<int> logIds =
                databaseLogs
                    .Select(log =>
                        log.LogID)
                    .ToList();


            /*
             * Load individual acknowledgements.
             *
             * This allows multiple relatives to
             * acknowledge independently.
             */
            List<LogAcknowledgement> acknowledgements =
                logIds.Count == 0
                    ? new List<LogAcknowledgement>()
                    : await _context
                        .LogAcknowledgements
                        .AsNoTracking()
                        .Where(acknowledgement =>
                            logIds.Contains(
                                acknowledgement.LogID))
                        .ToListAsync();


            /*
             * All Relationships rows for this patient
             * are treated as linked relatives/caretakers.
             */
            List<string> relativeUserIds =
                await _context.Relationships
                    .AsNoTracking()
                    .Where(relationship =>
                        relationship.PatientID ==
                        linkedPatientId)
                    .Select(relationship =>
                        relationship.UserID)
                    .Distinct()
                    .ToListAsync();


            HashSet<string> relativeUserIdSet =
                relativeUserIds.ToHashSet(
                    StringComparer.OrdinalIgnoreCase);


            Logs =
                databaseLogs
                    .Select(log =>
                    {
                        /*
                         * Acknowledgement is allowed only if
                         * this emergency belongs to the
                         * patient currently being viewed.
                         */
                        bool eligibleToAcknowledge =
                            log.PatientID ==
                            linkedPatientId;


                        /*
                         * For relatives, check their own
                         * LogAcknowledgement row.
                         */
                        bool currentRelativeAcknowledged =
                            !isSelf
                            &&
                            acknowledgements.Any(
                                acknowledgement =>
                                    acknowledgement.LogID ==
                                    log.LogID
                                    &&
                                    acknowledgement.UserID ==
                                    currentUserId);


                        /*
                         * Patients use the existing
                         * selfAcknowledged field.
                         *
                         * Relatives use LogAcknowledgements.
                         */
                        bool currentUserAcknowledged =
                            isSelf
                                ? log.selfAcknowledged
                                : currentRelativeAcknowledged;


                        int relativeAcknowledgementCount =
                            acknowledgements
                                .Where(acknowledgement =>
                                    acknowledgement.LogID ==
                                    log.LogID
                                    &&
                                    relativeUserIdSet.Contains(
                                        acknowledgement.UserID))
                                .Select(acknowledgement =>
                                    acknowledgement.UserID)
                                .Distinct(
                                    StringComparer.OrdinalIgnoreCase)
                                .Count();


                        return new LogListItem
                        {
                            LogId =
                                log.LogID,

                            Event =
                                log.Event,

                            Emergency =
                                log.Emergency,

                            Resolved =
                                log.Resolved,

                            Timestamp =
                                log.Timestamp,

                            SelfAcknowledged =
                                log.selfAcknowledged,


                            /*
                             * Existing shared Boolean:
                             *
                             * true means at least one relative
                             * has acknowledged.
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
                                log.Emergency
                                &&
                                !log.Resolved
                                &&
                                !currentUserAcknowledged
                                &&
                                eligibleToAcknowledge
                        };
                    })
                    .ToList();
        }


        // =========================================================
        // EXISTING ACKNOWLEDGE HANDLER
        // =========================================================
        //
        // Supports:
        //
        // asp-page-handler="Acknowledge"
        // parameter: id
        // =========================================================

        public Task<IActionResult>
            OnPostAcknowledgeAsync(int id)
        {
            return AcknowledgeEmergencyAsync(id);
        }


        // =========================================================
        // ALTERNATIVE EXISTING ACKNOWLEDGE HANDLER
        // =========================================================
        //
        // Supports:
        //
        // asp-page-handler="AcknowledgeEmergency"
        // parameter: logId
        // =========================================================

        public Task<IActionResult>
            OnPostAcknowledgeEmergencyAsync(
                int logId)
        {
            return AcknowledgeEmergencyAsync(
                logId);
        }


        // =========================================================
        // ACKNOWLEDGE EMERGENCY
        // =========================================================

        private async Task<IActionResult>
            AcknowledgeEmergencyAsync(
                int logId)
        {
            string? currentUserId =
                _userManager.GetUserId(User);

            if (string.IsNullOrWhiteSpace(
                    currentUserId))
            {
                return RedirectToPage(
                    "/Login");
            }


            Log? emergencyLog =
                await _context.Logs
                    .FirstOrDefaultAsync(log =>
                        log.LogID ==
                        logId
                        &&
                        log.Emergency
                        &&
                        !log.Resolved);


            if (
                emergencyLog == null
                ||
                !emergencyLog.PatientID.HasValue
            )
            {
                TempData["ErrorMessage"] =
                    "The emergency could not be found or has already been resolved.";

                return RedirectToPage();
            }


            /*
             * Get the patient currently selected
             * by this user.
             */
            (Patients? selectedPatient, bool isSelf) =
                await GetSelectedPatientRoleAsync(
                    currentUserId);


            /*
             * Do not allow the user to acknowledge
             * an emergency for another patient by
             * changing the Log ID manually.
             */
            bool authorised =
                selectedPatient != null
                &&
                selectedPatient.PatientID ==
                emergencyLog.PatientID.Value;


            if (!authorised)
            {
                return Forbid();
            }


            // =====================================================
            // PATIENT ACKNOWLEDGEMENT
            // =====================================================

            if (isSelf)
            {
                if (emergencyLog.selfAcknowledged)
                {
                    TempData["Message"] =
                        "You have already acknowledged this emergency.";

                    return RedirectToPage();
                }


                emergencyLog.selfAcknowledged = true;

                // Automatically resolve once BOTH
                // patient and relative have acknowledged.
                if (emergencyLog.selfAcknowledged &&
                    emergencyLog.relativeAcknowledged)
                {
                    emergencyLog.Resolved = true;
                }

                await _context.SaveChangesAsync();

                TempData["Message"] =
                    emergencyLog.Resolved
                        ? "Emergency acknowledged and resolved."
                        : "Emergency acknowledged successfully.";


                return RedirectToPage();
            }


            // =====================================================
            // RELATIVE ACKNOWLEDGEMENT
            // =====================================================

            /*
             * Make sure this account still has an
             * exact Relationships row with the
             * selected patient.
             */
            bool isLinkedRelative =
                await _context.Relationships
                    .AsNoTracking()
                    .AnyAsync(relationship =>
                        relationship.PatientID ==
                        emergencyLog.PatientID.Value
                        &&
                        relationship.UserID ==
                        currentUserId);


            if (!isLinkedRelative)
            {
                TempData["ErrorMessage"] =
                    "You are not authorised to acknowledge this patient's emergency.";

                return RedirectToPage();
            }


            /*
             * Prevent the same relative from
             * acknowledging twice.
             */
            bool alreadyAcknowledged =
                await _context
                    .LogAcknowledgements
                    .AsNoTracking()
                    .AnyAsync(acknowledgement =>
                        acknowledgement.LogID ==
                        logId
                        &&
                        acknowledgement.UserID ==
                        currentUserId);


            if (alreadyAcknowledged)
            {
                TempData["Message"] =
                    "You have already acknowledged this emergency.";

                return RedirectToPage();
            }


            /*
             * Store the acknowledgement belonging
             * specifically to this relative.
             */
            _context.LogAcknowledgements.Add(
                new LogAcknowledgement
                {
                    LogID =
                        logId,

                    UserID =
                        currentUserId,

                    AcknowledgedAt =
                        DateTime.UtcNow
                });


            /*
             * Keep this older Boolean updated because
             * Admin / teammate code still uses it.
             *
             * We are NOT removing or changing their logic.
             *
             * It simply means:
             *
             * "At least one relative acknowledged."
             */
            emergencyLog.relativeAcknowledged = true;

            // Automatically resolve once BOTH
            // patient and relative have acknowledged.
            if (emergencyLog.selfAcknowledged &&
                emergencyLog.relativeAcknowledged)
            {
                emergencyLog.Resolved = true;
            }

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                /*
                 * The database has a unique constraint
                 * on LogID + UserID.
                 *
                 * If two requests arrive at the same time,
                 * treat it as already acknowledged.
                 */
                TempData["Message"] =
                    "You have already acknowledged this emergency.";

                return RedirectToPage();
            }


            TempData["Message"] =
                emergencyLog.Resolved
                     ? "Emergency acknowledged and resolved."
                     : "Emergency acknowledged successfully.";


            return RedirectToPage();
        }


        // =========================================================
        // GET CURRENTLY SELECTED PATIENT
        // =========================================================
        //
        // IMPORTANT:
        //
        // Old version:
        //     Relationships.FirstOrDefault()
        //
        // New version:
        //     uses SelectedPatientId stored by Dashboard.
        //
        // This fixes relatives with multiple linked patients.
        // =========================================================

        private async Task<(Patients? Patient, bool IsSelf)>
            GetSelectedPatientRoleAsync(
                string currentUserId)
        {
            int? selectedPatientId =
                HttpContext.Session.GetInt32(
                    SelectedPatientSessionKey);


            // =====================================================
            // EXPLICIT PATIENT SELECTION EXISTS
            // =====================================================

            if (selectedPatientId.HasValue)
            {
                Patients? selectedPatient =
                    await _context.Patients
                        .Include(patient =>
                            patient.User)
                        .FirstOrDefaultAsync(patient =>
                            patient.PatientID ==
                            selectedPatientId.Value);


                if (selectedPatient != null)
                {
                    bool isSelf =
                        selectedPatient.UserID ==
                        currentUserId;


                    if (isSelf)
                    {
                        return (
                            selectedPatient,
                            true
                        );
                    }


                    bool isRelated =
                        await _context
                            .Relationships
                            .AsNoTracking()
                            .AnyAsync(relationship =>
                                relationship.PatientID ==
                                selectedPatient.PatientID
                                &&
                                relationship.UserID ==
                                currentUserId);


                    if (isRelated)
                    {
                        return (
                            selectedPatient,
                            false
                        );
                    }
                }


                /*
                 * Selection is invalid or access
                 * was removed.
                 */
                HttpContext.Session.Remove(
                    SelectedPatientSessionKey);
            }


            // =====================================================
            // DEFAULT TO USER'S OWN PATIENT RECORD
            // =====================================================

            Patients? ownPatient =
                await _context.Patients
                    .Include(patient =>
                        patient.User)
                    .FirstOrDefaultAsync(patient =>
                        patient.UserID ==
                        currentUserId);


            if (ownPatient != null)
            {
                HttpContext.Session.SetInt32(
                    SelectedPatientSessionKey,
                    ownPatient.PatientID);


                return (
                    ownPatient,
                    true
                );
            }


            // =====================================================
            // RELATIVE HAS NOT SELECTED A PATIENT
            // =====================================================

            return (
                null,
                false
            );
        }


        // =========================================================
        // LOAD NORMAL USER LOGS
        // =========================================================
        //
        // Used only when a relative has not yet selected
        // a patient.
        //
        // We preserve normal account logs, but do not
        // randomly choose one of their related patients.
        // =========================================================

        private async Task<IList<LogListItem>>
            LoadUserOnlyLogsAsync(
                string currentUserId)
        {
            List<Log> databaseLogs =
                await _context.Logs
                    .AsNoTracking()
                    .Where(log =>
                        log.UserID ==
                        currentUserId)
                    .OrderByDescending(log =>
                        log.Timestamp)
                    .ToListAsync();


            return databaseLogs
                .Select(log =>
                    new LogListItem
                    {
                        LogId =
                            log.LogID,

                        Event =
                            log.Event,

                        Emergency =
                            log.Emergency,

                        Resolved =
                            log.Resolved,

                        Timestamp =
                            log.Timestamp,

                        SelfAcknowledged =
                            log.selfAcknowledged,

                        RelativeAcknowledged =
                            log.relativeAcknowledged,

                        CurrentUserAcknowledged =
                            false,

                        CurrentRelativeAcknowledged =
                            false,

                        RelativeAcknowledgementCount =
                            0,

                        TotalRelativeCount =
                            0,

                        CanAcknowledge =
                            false
                    })
                .ToList();
        }
    }
}