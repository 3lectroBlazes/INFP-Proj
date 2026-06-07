using INFP_Proj.Data;
using INFP_Proj.Models;
using INFP_Proj.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin.Reception
{
    [Authorize(Roles = "Reception, Admin")]
    public class ManageNotificationsModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;
        private readonly ISmsService _smsService;

        public ManageNotificationsModel(UserManager<AppUser> userManager, AppDbContext context, ISmsService smsService)
        {
            _userManager = userManager;
            _context = context;
            _smsService = smsService;
        }

        public List<SelectListItem> PatientList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> NurseList { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> BraceletList { get; set; } = new List<SelectListItem>();

        public async Task OnGetAsync()
        {
            await PopulateDropdownsAsync();
        }

        // --- HANDLER 1: PATIENT/FAMILY NOTIFICATIONS ---
        public async Task<IActionResult> OnPostPatientAlertAsync(string targetUserId, string messageType, string customMessage)
        {
            var user = await _userManager.FindByIdAsync(targetUserId);
            if (user != null && !string.IsNullOrEmpty(user.PhoneNumber))
            {
                string finalMessage = $"HOSPITAL ALERT ({messageType}): {customMessage}";
                await _smsService.SendSmsAsync(user.PhoneNumber, finalMessage);

                // Green Box Text
                TempData["SuccessMessage"] = $"SMS sent to patient {user.FirstName} {user.LastName}.";
                // JS Popup Text
                TempData["AlertMessage"] = $"[SMS to {user.FirstName}]: {finalMessage}";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to send: User has no valid phone number registered.";
            }

            return RedirectToPage();
        }

        // --- HANDLER 2: HARDWARE FAILURE NOTIFICATIONS ---
        public async Task<IActionResult> OnPostHardwareAlertAsync(string targetNurseId, int braceletId, string issueDescription)
        {
            var nurse = await _userManager.FindByIdAsync(targetNurseId);
            if (nurse != null && !string.IsNullOrEmpty(nurse.PhoneNumber))
            {
                string finalMessage = $"HARDWARE ALERT: Bracelet #{braceletId} has reported a failure. Details: {issueDescription}. Please check immediately.";
                await _smsService.SendSmsAsync(nurse.PhoneNumber, finalMessage);

                // Green Box Text
                TempData["SuccessMessage"] = $"Hardware alert SMS sent to Nurse {nurse.FirstName}.";
                // JS Popup Text
                TempData["AlertMessage"] = $"[SMS to Nurse {nurse.FirstName}]: {finalMessage}";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to send: Nurse has no valid phone number registered.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostEmergencyAlertAsync(string targetGroup, string emergencyType, string locationDetails)
        {
            IList<AppUser> usersToNotify = new List<AppUser>();

            if (targetGroup == "Nurses" || targetGroup == "Both")
            {
                var nurses = await _userManager.GetUsersInRoleAsync("Nurse");
                ((List<AppUser>)usersToNotify).AddRange(nurses);
            }
            if (targetGroup == "Doctors" || targetGroup == "Both")
            {
                var doctors = await _userManager.GetUsersInRoleAsync("Doctor");
                ((List<AppUser>)usersToNotify).AddRange(doctors);
            }

            int successCount = 0;
            string finalMessage = $"EMERGENCY - {emergencyType.ToUpper()}! Location: {locationDetails}. Immediate assistance required.";

            foreach (var staff in usersToNotify)
            {
                if (!string.IsNullOrEmpty(staff.PhoneNumber))
                {
                    await _smsService.SendSmsAsync(staff.PhoneNumber, finalMessage);
                    successCount++;
                }
            }

            TempData["SuccessMessage"] = $"Emergency SMS broadcasted to {successCount} staff members.";
            TempData["AlertMessage"] = $"[BROADCAST SMS]: {finalMessage}";
            return RedirectToPage();
        }

        private async Task PopulateDropdownsAsync()
        {
            var patients = await _userManager.GetUsersInRoleAsync("User");
            PatientList = patients.Select(p => new SelectListItem
            {
                Value = p.Id,
                Text = $"{p.FirstName} {p.LastName} ({p.PhoneNumber ?? "No Phone"})"
            }).ToList();

            var nurses = await _userManager.GetUsersInRoleAsync("Nurse");
            NurseList = nurses.Select(n => new SelectListItem
            {
                Value = n.Id,
                Text = $"Nurse {n.FirstName} {n.LastName}"
            }).ToList();

            BraceletList = await _context.Bracelets.Select(b => new SelectListItem
            {
                Value = b.BraceletID.ToString(),
                Text = $"Bracelet #{b.BraceletID} (Loc: {b.Location})"
            }).ToListAsync();
        }
    }
}