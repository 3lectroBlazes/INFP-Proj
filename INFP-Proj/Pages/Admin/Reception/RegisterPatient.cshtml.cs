using INFP_Proj.Data;
using INFP_Proj.Models;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin.Reception
{
    [Authorize(Roles = "Reception")]
    public class RegisterPatientModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly AppDbContext _context;

        public RegisterPatientModel(UserManager<AppUser> userManager, RoleManager<AppRole> roleManager, AppDbContext context)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [BindProperty]
        public RegisterPatientViewModel Input { get; set; } = new RegisterPatientViewModel();

        [BindProperty]
        public int? SelectedWardID { get; set; }

        public List<SelectListItem> AvailableWards { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableBracelets { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableBeds { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableDiagnoses { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> EligibleExistingUsers { get; set; } = new List<SelectListItem>();

        public async Task OnGetAsync()
        {
            PopulateWards();
            PopulateAllBracelets();
            await PopulateDiagnosesAsync();
            await PopulateEligibleExistingUsersAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            bool isExistingUser = string.Equals(Input.Mode, "Existing", StringComparison.OrdinalIgnoreCase);

            if (isExistingUser)
            {
                if (string.IsNullOrWhiteSpace(Input.ExistingUserId))
                {
                    ModelState.AddModelError(nameof(Input.ExistingUserId), "Please select a registered user.");
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(Input.FirstName))
                {
                    ModelState.AddModelError(nameof(Input.FirstName), "First Name is required.");
                }

                if (string.IsNullOrWhiteSpace(Input.LastName))
                {
                    ModelState.AddModelError(nameof(Input.LastName), "Last Name is required.");
                }

                if (string.IsNullOrWhiteSpace(Input.Email))
                {
                    ModelState.AddModelError(nameof(Input.Email), "Patient Email is required.");
                }
            }

            AppUser? existingUser = null;
            if (isExistingUser && !string.IsNullOrWhiteSpace(Input.ExistingUserId))
            {
                existingUser = await _userManager.FindByIdAsync(Input.ExistingUserId);
                if (existingUser == null)
                {
                    ModelState.AddModelError(nameof(Input.ExistingUserId), "Selected user was not found.");
                }
                else if (!await IsEligibleUserAsync(existingUser))
                {
                    ModelState.AddModelError(nameof(Input.ExistingUserId), "Only non-admin users can be admitted.");
                }
                else if (await _context.Patients.AnyAsync(p => p.UserID == existingUser.Id && p.Status == "Admitted"))
                {
                    ModelState.AddModelError(nameof(Input.ExistingUserId), "This user is already currently admitted.");
                }
            }

            if (!ModelState.IsValid)
            {
                await ReloadFormListsAsync();
                return Page();
            }

            AppUser user;

            if (isExistingUser)
            {
                user = existingUser!;
            }
            else
            {
                user = new AppUser
                {
                    UserName = Input.Email,
                    Email = Input.Email,
                    FirstName = Input.FirstName!,
                    LastName = Input.LastName!,
                    EmailConfirmed = true
                };

                IdentityResult createResult = await _userManager.CreateAsync(user, "TempPass123!");
                if (!createResult.Succeeded)
                {
                    foreach (IdentityError error in createResult.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }

                    await ReloadFormListsAsync();
                    return Page();
                }

                await _userManager.AddToRoleAsync(user, "User");
            }

            Patients? patient = await _context.Patients.FirstOrDefaultAsync(p => p.UserID == user.Id);

            if (patient == null)
            {
                patient = new Patients
                {
                    UserID = user.Id,
                    Status = "Admitted"
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Readmission: reuse the existing patient row and free any previous bracelet.
                patient.Status = "Admitted";

                List<BraceletRelation> oldRelations = await _context.BraceletRelations
                    .Where(br => br.PatientID == patient.PatientID)
                    .ToListAsync();
                if (oldRelations.Count > 0)
                {
                    _context.BraceletRelations.RemoveRange(oldRelations);
                }

                // Close out any still-open admission so the new admission is a fresh,
                // separate record rather than overwriting the previous one.
                List<Records> openRecords = await _context.Records
                    .Where(r => r.PatientID == patient.PatientID && r.DischargeDateTime == null)
                    .ToListAsync();
                foreach (Records openRecord in openRecords)
                {
                    openRecord.DischargeDateTime = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
            }

            if (isExistingUser)
            {
                await EnsurePatientRoleAssignedAsync(user.Id);
            }

            _context.BraceletRelations.Add(new BraceletRelation
            {
                PatientID = patient.PatientID,
                BraceletID = Input.BraceletID
            });

            Beds? selectedBed = await _context.Beds.FindAsync(Input.BedID);
            int assignedWardID = 0;
            if (selectedBed != null)
            {
                selectedBed.PatientID = patient.PatientID;
                assignedWardID = selectedBed.WardID;
            }

            Bracelet? assignedBracelet = await _context.Bracelets.FindAsync(Input.BraceletID);
            if (assignedBracelet != null && assignedWardID != 0)
            {
                assignedBracelet.Location = $"Ward {assignedWardID}";
            }

            Records patrec = new Records
            {
                PatientID = patient.PatientID,
                BedID = Input.BedID,
                WardID = assignedWardID,
                HospitalID = 1,
                DiagnosisID = Input.DiagnosisID,
                Description = Input.AdmissionNotes,
                AdmissionDateTime = DateTime.UtcNow
            };
            _context.Records.Add(patrec);

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Patient {user.FirstName} {user.LastName} registered successfully. Assigned to Bed #{Input.BedID} (Bracelet #{Input.BraceletID} located in Ward {assignedWardID}).";
            return RedirectToPage("./RegisterPatient");
        }

        private async Task EnsurePatientRoleAssignedAsync(string userId)
        {
            if (!await _roleManager.RoleExistsAsync("Patient"))
            {
                await _roleManager.CreateAsync(new AppRole { Name = "Patient" });
            }

            AppUser? user = await _userManager.FindByIdAsync(userId);
            if (user != null && !await _userManager.IsInRoleAsync(user, "Patient"))
            {
                IdentityResult result = await _userManager.AddToRoleAsync(user, "Patient");
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        private async Task ReloadFormListsAsync()
        {
            PopulateWards();
            PopulateAllBracelets();
            await PopulateDiagnosesAsync();
            await PopulateEligibleExistingUsersAsync();
            if (SelectedWardID.HasValue)
            {
                PopulateBedsForWard(SelectedWardID.Value);
            }
        }

        private void PopulateWards()
        {
            var wardIds = _context.Beds.Select(b => b.WardID).Distinct().ToList();
            AvailableWards = wardIds.Select(id => new SelectListItem
            {
                Value = id.ToString(),
                Text = $"Ward {id}"
            }).ToList();
        }

        private void PopulateAllBracelets()
        {
            List<int> assignedBraceletIds = _context.BraceletRelations
                .Select(br => br.BraceletID)
                .ToList();

            AvailableBracelets = _context.Bracelets
                .Where(b => !assignedBraceletIds.Contains(b.BraceletID))
                .Select(b => new SelectListItem
                {
                    Value = b.BraceletID.ToString(),
                    Text = $"Bracelet #{b.BraceletID} (Location: {b.Location} | Batt: {b.Battery}%)"
                }).ToList();
        }

        private void PopulateBedsForWard(int wardId)
        {
            AvailableBeds = _context.Beds
                .Where(b => b.PatientID == null && b.WardID == wardId)
                .Select(b => new SelectListItem
                {
                    Value = b.BedID.ToString(),
                    Text = $"Bed #{b.BedID} - Room {b.Room} (Ward {b.WardID})"
                }).ToList();
        }

        private async Task PopulateDiagnosesAsync()
        {
            AvailableDiagnoses = await _context.Diagnoses
                .OrderBy(d => d.DiagnosisName)
                .Select(d => new SelectListItem
                {
                    Value = d.DiagnosisID.ToString(),
                    Text = d.DiagnosisName
                }).ToListAsync();
        }

        private async Task<bool> IsEligibleUserAsync(AppUser user)
        {
            IList<string> roles = await _userManager.GetRolesAsync(user);
            HashSet<string> adminRoleNames = await GetAdminRoleNamesAsync();
            return !roles.Any(r => adminRoleNames.Contains(r));
        }

        private async Task<HashSet<string>> GetAdminRoleNamesAsync()
        {
            return (await _roleManager.Roles
                .Where(r => r.IsAdmin && r.Name != null)
                .Select(r => r.Name!)
                .ToListAsync())
                .ToHashSet();
        }

        private async Task PopulateEligibleExistingUsersAsync()
        {
            HashSet<string> admittedUserIds = (await _context.Patients
                .Where(p => p.Status == "Admitted")
                .Select(p => p.UserID)
                .ToListAsync()).ToHashSet();

            HashSet<string> excludedUserIds = new HashSet<string>(admittedUserIds);
            HashSet<string> adminRoleNames = await GetAdminRoleNamesAsync();

            foreach (string roleName in adminRoleNames)
            {
                foreach (AppUser user in await _userManager.GetUsersInRoleAsync(roleName))
                {
                    excludedUserIds.Add(user.Id);
                }
            }

            EligibleExistingUsers = (await _userManager.Users
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToListAsync())
                .Where(u => !excludedUserIds.Contains(u.Id))
                .Select(u => new SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FirstName} {u.LastName}".Trim() + (u.Email != null ? $" ({u.Email})" : ""),
                    Selected = Input.ExistingUserId == u.Id
                })
                .ToList();
        }

        public JsonResult OnGetWardData(int wardId)
        {
            var beds = _context.Beds
                .Where(b => b.PatientID == null && b.WardID == wardId)
                .Select(b => new
                {
                    value = b.BedID.ToString(),
                    text = $"Bed #{b.BedID} - Room {b.Room}"
                }).ToList();

            return new JsonResult(new { beds = beds });
        }
    }
}
