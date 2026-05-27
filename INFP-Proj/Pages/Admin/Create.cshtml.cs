using INFP_Proj.Data;
using INFP_Proj.Model;
using INFP_Proj.Models;
using INFP_Proj.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly AdminLogService _adminLogService;
        private readonly UserManager<AppUser> _userManager;

        public CreateModel(AppDbContext context, AdminLogService adminLogService, UserManager<AppUser> userManager)
        {
            _context = context;
            _adminLogService = adminLogService;
            _userManager = userManager;
        }

        [BindProperty]
        public Patients Patients { get; set; } = new() { Status = "Admitted" };

        public SelectList UserOptions { get; set; } = default!;
        public SelectList BraceletOptions { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            await PopulateSelectListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Patients.UserID))  
            {
                ModelState.AddModelError("Patients.UserID", "Please select a user.");
            }

            if (Patients.BraceletID <= 0)
            {
                ModelState.AddModelError("Patients.BraceletID", "Please select a bracelet.");
            }

            if (string.IsNullOrWhiteSpace(Patients.Status))
            {
                Patients.Status = "Admitted";
            }

            if (!ModelState.IsValid)
            {
                await PopulateSelectListsAsync();
                return Page();
            }

            var userAlreadyPatient = await _context.Patients
                .AnyAsync(p => p.UserID == Patients.UserID);
            if (userAlreadyPatient)
            {
                ModelState.AddModelError("Patients.UserID", "This user is already linked to a patient.");
                await PopulateSelectListsAsync();
                return Page();
            }

            var braceletInUse = await _context.Patients
                .AnyAsync(p => p.BraceletID == Patients.BraceletID);
            if (braceletInUse)
            {
                ModelState.AddModelError("Patients.BraceletID", "This bracelet is already assigned.");
                await PopulateSelectListsAsync();
                return Page();
            }

            Patients.PatientID = await _context.Patients.AnyAsync()
                ? await _context.Patients.MaxAsync(p => p.PatientID) + 1
                : 1;

            _context.Patients.Add(Patients);
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(Patients.UserID);
            var patientName = user != null
                ? $"{user.FirstName} {user.LastName}"
                : $"patient #{Patients.PatientID}";
            await _adminLogService.AddLogAsync($"New patient registered: {patientName}");
            await _adminLogService.AddLogAsync("Your patient account was created",userId: Patients.UserID);

            TempData["Message"] = "Patient created successfully.";
            return RedirectToPage("./Index");
        }

        private async Task PopulateSelectListsAsync()
        {
            var assignedUserIds = await _context.Patients.Select(p => p.UserID).ToListAsync();
            var assignedBraceletIds = await _context.Patients.Select(p => p.BraceletID).ToListAsync();

            var patientRoleUsers = await _userManager.GetUsersInRoleAsync("Patient");
            var availableUsers = patientRoleUsers
                .Where(u => !assignedUserIds.Contains(u.Id))
                .OrderBy(u => u.LastName)
                .Select(u => new { UserID = u.Id, Name = $"{u.FirstName} {u.LastName}" })
                .ToList();

            var availableBracelets = await _context.Bracelets
                .Where(b => !assignedBraceletIds.Contains(b.BraceletID))
                .OrderBy(b => b.BraceletID)
                .Select(b => new
                {
                    b.BraceletID,
                    Label = $"#{b.BraceletID}" + (b.Location != null ? $" — {b.Location}" : "")
                })
                .ToListAsync();

            UserOptions = new SelectList(availableUsers, "UserID", "Name");
            BraceletOptions = new SelectList(availableBracelets, "BraceletID", "Label");
        }
    }
}