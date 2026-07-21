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
        private readonly IEmailService _emailService; // Changed from ISmsService

        public ManageNotificationsModel(UserManager<AppUser> userManager, AppDbContext context, IEmailService emailService)
        {
            _userManager = userManager;
            _context = context;
            _emailService = emailService;
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

            // Check for valid email instead of phone number
            if (user != null && !string.IsNullOrEmpty(user.Email))
            {
                string subject = $"HOSPITAL ALERT: {messageType}";
                string finalMessage = customMessage;

                await _emailService.SendEmailAsync(user.Email, subject, finalMessage);

                // Green Box Text
                TempData["SuccessMessage"] = $"Email sent to patient {user.FirstName} {user.LastName}.";
                // JS Popup Text
                TempData["AlertMessage"] = $"[Email to {user.FirstName}]: {finalMessage}";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to send: User has no valid email registered.";
            }

            return RedirectToPage();
        }

        // --- HANDLER 2: HARDWARE FAILURE NOTIFICATIONS ---
        public async Task<IActionResult> OnPostHardwareAlertAsync(string targetNurseId, int braceletId, string issueDescription)
        {
            var nurse = await _userManager.FindByIdAsync(targetNurseId);

            // Check for valid email instead of phone number
            if (nurse != null && !string.IsNullOrEmpty(nurse.Email))
            {
                string subject = "HARDWARE ALERT";
                string finalMessage = $"Bracelet #{braceletId} has reported a failure. Details: {issueDescription}. Please check immediately.";

                await _emailService.SendEmailAsync(nurse.Email, subject, finalMessage);

                // Green Box Text
                TempData["SuccessMessage"] = $"Hardware alert email sent to Nurse {nurse.FirstName}.";
                // JS Popup Text
                TempData["AlertMessage"] = $"[Email to Nurse {nurse.FirstName}]: {finalMessage}";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to send: Nurse has no valid email registered.";
            }

            return RedirectToPage();
        }

        // --- HANDLER 3: EMERGENCY ALERT ---
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
            string subject = $"EMERGENCY - {emergencyType.ToUpper()}!";
            string finalMessage = $"Location: {locationDetails}. Immediate assistance required.";

            foreach (var staff in usersToNotify)
            {
                // Check for valid email instead of phone number
                if (!string.IsNullOrEmpty(staff.Email))
                {
                    await _emailService.SendEmailAsync(staff.Email, subject, finalMessage);
                    successCount++;
                }
            }

            TempData["SuccessMessage"] = $"Emergency Email broadcasted to {successCount} staff members.";
            TempData["AlertMessage"] = $"[BROADCAST EMAIL]: {finalMessage}";
            return RedirectToPage();
        }

        private async Task PopulateDropdownsAsync()
        {
            var patients = await _userManager.GetUsersInRoleAsync("User");
            PatientList = patients.Select(p => new SelectListItem
            {
                Value = p.Id,
                // Display Email instead of Phone Number in UI
                Text = $"{p.FirstName} {p.LastName} ({p.Email ?? "No Email"})"
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