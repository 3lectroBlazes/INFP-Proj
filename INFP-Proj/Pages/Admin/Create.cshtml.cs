using INFP_Proj.Data;
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
        public RegisterPatientInput Input { get; set; } = new();

        public SelectList UserOptions { get; set; } = default!;
        public SelectList BraceletOptions { get; set; } = default!;

        public async Task<IActionResult> OnGetAsync()
        {
            await PopulateSelectListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(Input.Status))
            {
                Input.Status = "Admitted";
            }

            if (Input.BraceletID <= 0)
            {
                ModelState.AddModelError(nameof(Input.BraceletID), "Please select a bracelet.");
            }

            var isNewUser = string.Equals(Input.AccountMode, "new", StringComparison.OrdinalIgnoreCase);
            string? userId = null;

            if (isNewUser)
            {
                ValidateNewUserFields();
                if (ModelState.IsValid)
                {
                    userId = await CreatePatientUserAsync();
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Input.ExistingUserId))
                {
                    ModelState.AddModelError(nameof(Input.ExistingUserId), "Please select an existing patient account.");
                }
                else
                {
                    userId = Input.ExistingUserId;
                }
            }

            if (!ModelState.IsValid || string.IsNullOrEmpty(userId))
            {
                await PopulateSelectListsAsync();
                return Page();
            }

            var userAlreadyPatient = await _context.Patients.AnyAsync(p => p.UserID == userId);
            if (userAlreadyPatient)
            {
                ModelState.AddModelError(
                    isNewUser ? nameof(Input.NewEmail) : nameof(Input.ExistingUserId),
                    "This user is already registered as a patient.");
                await PopulateSelectListsAsync();
                return Page();
            }

            var braceletInUse = await _context.Patients.AnyAsync(p => p.BraceletID == Input.BraceletID);
            if (braceletInUse)
            {
                ModelState.AddModelError(nameof(Input.BraceletID), "This bracelet is already assigned.");
                await PopulateSelectListsAsync();
                return Page();
            }

            var bracelet = await _context.Bracelets.FindAsync(Input.BraceletID);
            if (bracelet == null)
            {
                ModelState.AddModelError(nameof(Input.BraceletID), "Selected bracelet was not found.");
                await PopulateSelectListsAsync();
                return Page();
            }

            var patient = new Patients
            {
                UserID = userId!,
                BraceletID = Input.BraceletID,
                Status = Input.Status
            };

            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            bracelet.PatientID = patient.PatientID;
            await _context.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(userId!);
            var patientName = user != null
                ? $"{user.FirstName} {user.LastName}"
                : $"Patient #{patient.PatientID}";

            await _adminLogService.AddLogAsync($"New patient registered: {patientName} (ID {patient.PatientID})");
            await _adminLogService.AddLogAsync("Your patient account was created", userId: userId);

            TempData["Message"] = $"Patient #{patient.PatientID} created successfully.";
            return RedirectToPage("./Index");
        }

        private void ValidateNewUserFields()
        {
            if (string.IsNullOrWhiteSpace(Input.NewFirstName))
            {
                ModelState.AddModelError(nameof(Input.NewFirstName), "First name is required.");
            }

            if (string.IsNullOrWhiteSpace(Input.NewLastName))
            {
                ModelState.AddModelError(nameof(Input.NewLastName), "Last name is required.");
            }

            if (string.IsNullOrWhiteSpace(Input.NewEmail))
            {
                ModelState.AddModelError(nameof(Input.NewEmail), "Email is required.");
            }

            if (string.IsNullOrWhiteSpace(Input.NewPassword))
            {
                ModelState.AddModelError(nameof(Input.NewPassword), "Password is required.");
            }
        }

        private async Task<string?> CreatePatientUserAsync()
        {
            var user = new AppUser
            {
                FirstName = Input.NewFirstName!.Trim(),
                MiddleName = string.IsNullOrWhiteSpace(Input.NewMiddleName) ? null : Input.NewMiddleName.Trim(),
                LastName = Input.NewLastName!.Trim(),
                UserName = Input.NewEmail!.Trim(),
                Email = Input.NewEmail.Trim(),
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, Input.NewPassword!);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(nameof(Input.NewEmail), error.Description);
                }

                return null;
            }

            await _userManager.AddToRoleAsync(user, "Patient");
            return user.Id;
        }

        private async Task PopulateSelectListsAsync()
        {
            var assignedUserIds = await _context.Patients.Select(p => p.UserID).ToListAsync();
            var assignedBraceletIds = await _context.Patients.Select(p => p.BraceletID).ToListAsync();

            var patientRoleUsers = await _userManager.GetUsersInRoleAsync("Patient");
            var availableUsers = patientRoleUsers
                .Where(u => !assignedUserIds.Contains(u.Id))
                .OrderBy(u => u.LastName)
                .Select(u => new { UserID = u.Id, Name = $"{u.FirstName} {u.LastName} ({u.Email})" })
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

            UserOptions = new SelectList(availableUsers, "UserID", "Name", Input.ExistingUserId);
            BraceletOptions = new SelectList(availableBracelets, "BraceletID", "Label", Input.BraceletID);
        }
    }
}
