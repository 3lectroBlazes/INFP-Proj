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
        private const string DefaultDosage = "As directed";

        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;
        private readonly AdminLogService _adminLogService;

        public CreateModel(
            AppDbContext context,
            UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager,
            AdminLogService adminLogService)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _adminLogService = adminLogService;
        }

        [BindProperty]
        public PatientAdmissionInput Input { get; set; } = new();

        public SelectList UserOptions { get; set; } = default!;
        public List<SelectListItem> BraceletOptions { get; set; } = new();
        public SelectList BedOptions { get; set; } = default!;
        public SelectList WardOptions { get; set; } = default!;
        public SelectList DiagnosisOptions { get; set; } = default!;
        public MultiSelectList MedicationOptions { get; set; } = default!;

        // BraceletID == 0 means "create a new bracelet"; null means nothing selected.
        private const int NewBraceletValue = 0;

        public async Task<IActionResult> OnGetAsync()
        {
            await PopulateSelectListsAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var isNewBracelet = Input.BraceletID == NewBraceletValue;

            if (Input.BraceletID is null)
            {
                ModelState.AddModelError(nameof(Input.BraceletID), "Please select a bracelet.");
            }

            if (Input.MedicationIDs.Count == 0)
            {
                ModelState.AddModelError(nameof(Input.MedicationIDs), "Please select at least one medication.");
            }

            await ValidateSelectionsAsync();

            if (!ModelState.IsValid)
            {
                await ReturnPageWithListsAsync();
                return Page();
            }

            var hospital = await _context.Hospitals.OrderBy(h => h.HospitalID).FirstOrDefaultAsync();
            if (hospital == null)
            {
                ModelState.AddModelError(string.Empty, "No hospital exists in the system. Add a hospital before admitting a patient.");
                await ReturnPageWithListsAsync();
                return Page();
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                Bracelet bracelet;
                if (isNewBracelet)
                {
                    bracelet = new Bracelet();
                    _context.Bracelets.Add(bracelet);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    bracelet = (await _context.Bracelets.FindAsync(Input.BraceletID!.Value))!;
                }

                var patient = await _context.Patients
                    .FirstOrDefaultAsync(p => p.UserID == Input.UserId);
                if (patient == null)
                {
                    patient = new Patients
                    {
                        UserID = Input.UserId!,
                        Status = "Admitted"
                    };
                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Readmission: reuse the existing patient record and free any previous bracelet.
                    patient.Status = "Admitted";
                    var oldRelations = await _context.BraceletRelations
                        .Where(br => br.PatientID == patient.PatientID)
                        .ToListAsync();
                    if (oldRelations.Count > 0)
                    {
                        _context.BraceletRelations.RemoveRange(oldRelations);
                    }
                    await _context.SaveChangesAsync();
                }

                _context.BraceletRelations.Add(new BraceletRelation
                {
                    PatientID = patient.PatientID,
                    BraceletID = bracelet.BraceletID
                });
                await _context.SaveChangesAsync();

                var medicationLists = Input.MedicationIDs
                    .Distinct()
                    .Select(medicationId => new MedicationList
                    {
                        PatientID = patient.PatientID,
                        MedicationID = medicationId,
                        Dosage = DefaultDosage
                    })
                    .ToList();

                _context.MedicationLists.AddRange(medicationLists);
                await _context.SaveChangesAsync();

                var record = new Records
                {
                    PatientID = patient.PatientID,
                    BedID = Input.BedID,
                    WardID = Input.WardID,
                    HospitalID = hospital.HospitalID,
                    DiagnosisID = Input.DiagnosisID,
                    MedicationListID = medicationLists[0].MedicationListID,
                    Description = string.IsNullOrWhiteSpace(Input.Description)
                        ? "Admitted"
                        : Input.Description.Trim(),
                    AdmissionDateTime = DateTime.UtcNow,
                    DischargeDateTime = null
                };
                _context.Records.Add(record);
                await _context.SaveChangesAsync();

                await EnsurePatientRoleAssignedAsync(Input.UserId!);

                await transaction.CommitAsync();

                var user = await _userManager.FindByIdAsync(Input.UserId!);
                var patientName = user != null
                    ? $"{user.FirstName} {user.LastName}"
                    : $"Patient #{patient.PatientID}";

                await _adminLogService.AddLogAsync($"New patient admitted: {patientName} (ID {patient.PatientID})");
                await _adminLogService.AddLogAsync("You were admitted as a patient", userId: Input.UserId);

                TempData["Message"] = $"Patient #{patient.PatientID} admitted successfully.";
                return RedirectToPage("./Index");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, $"Unable to admit patient: {ex.Message}");
                await ReturnPageWithListsAsync();
                return Page();
            }
        }

        private async Task ValidateSelectionsAsync()
        {
            if (!string.IsNullOrWhiteSpace(Input.UserId))
            {
                var user = await _userManager.FindByIdAsync(Input.UserId);
                if (user == null)
                {
                    ModelState.AddModelError(nameof(Input.UserId), "Selected user was not found.");
                }
                else if (!await IsEligibleUserAsync(user))
                {
                    ModelState.AddModelError(nameof(Input.UserId), "Only non-admin users can be admitted.");
                }
                else if (await _context.Patients.AnyAsync(p => p.UserID == Input.UserId && p.Status == "Admitted"))
                {
                    ModelState.AddModelError(nameof(Input.UserId), "This user is already currently admitted.");
                }
            }
            if (Input.BraceletID is > 0)
            {
                if (!await _context.Bracelets.AnyAsync(b => b.BraceletID == Input.BraceletID))
                {
                    ModelState.AddModelError(nameof(Input.BraceletID), "Selected bracelet was not found.");
                }
                else if (await _context.BraceletRelations.AnyAsync(br => br.BraceletID == Input.BraceletID))
                {
                    ModelState.AddModelError(nameof(Input.BraceletID), "This bracelet is already assigned to a patient.");
                }
            }

            if (Input.BedID > 0 && !await _context.Beds.AnyAsync(b => b.BedID == Input.BedID))
            {
                ModelState.AddModelError(nameof(Input.BedID), "Selected bed was not found.");
            }

            if (Input.WardID > 0 && !await _context.Wards.AnyAsync(w => w.WardID == Input.WardID))
            {
                ModelState.AddModelError(nameof(Input.WardID), "Selected ward was not found.");
            }

            if (Input.DiagnosisID > 0 && !await _context.Diagnoses.AnyAsync(d => d.DiagnosisID == Input.DiagnosisID))
            {
                ModelState.AddModelError(nameof(Input.DiagnosisID), "Selected diagnosis was not found.");
            }

            if (Input.MedicationIDs.Count > 0)
            {
                var validMedicationIds = await _context.Medications
                    .Where(m => Input.MedicationIDs.Contains(m.MedicationID))
                    .Select(m => m.MedicationID)
                    .ToListAsync();

                if (Input.MedicationIDs.Distinct().Any(id => !validMedicationIds.Contains(id)))
                {
                    ModelState.AddModelError(nameof(Input.MedicationIDs), "One or more selected medications were not found.");
                }
            }
        }

        private async Task EnsurePatientRoleAssignedAsync(string userId)
        {
            if (!await _roleManager.RoleExistsAsync("Patient"))
            {
                await _roleManager.CreateAsync(new AppRole { Name = "Patient" });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user != null && !await _userManager.IsInRoleAsync(user, "Patient"))
            {
                var result = await _userManager.AddToRoleAsync(user, "Patient");
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
                }
            }
        }

        private async Task<bool> IsEligibleUserAsync(AppUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            var adminRoleNames = await GetAdminRoleNamesAsync();
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

        private async Task ReturnPageWithListsAsync()
        {
            await PopulateSelectListsAsync();
        }

        private async Task PopulateSelectListsAsync()
        {
            var admittedUserIds = (await _context.Patients
                .Where(p => p.Status == "Admitted")
                .Select(p => p.UserID)
                .ToListAsync()).ToHashSet();

            var excludedUserIds = new HashSet<string>(admittedUserIds);
            var adminRoleNames = await GetAdminRoleNamesAsync();
            foreach (var roleName in adminRoleNames)
            {
                foreach (var user in await _userManager.GetUsersInRoleAsync(roleName))
                {
                    excludedUserIds.Add(user.Id);
                }
            }

            var availableUsers = (await _userManager.Users
                    .OrderBy(u => u.LastName)
                    .ThenBy(u => u.FirstName)
                    .ToListAsync())
                .Where(u => !excludedUserIds.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    Name = $"{u.FirstName} {u.LastName}".Trim() + (u.Email != null ? $" ({u.Email})" : "")
                })
                .ToList();

            var assignedBraceletIds = (await _context.BraceletRelations.Select(br => br.BraceletID).ToListAsync()).ToHashSet();
            var braceletOptions = (await _context.Bracelets
                    .OrderBy(b => b.BraceletID)
                    .ToListAsync())
                .Where(b => !assignedBraceletIds.Contains(b.BraceletID))
                .Select(b => new SelectListItem
                {
                    Value = b.BraceletID.ToString(),
                    Text = $"#{b.BraceletID}" + (b.Location != null ? $" — {b.Location}" : ""),
                    Selected = Input.BraceletID == b.BraceletID
                })
                .ToList();

            braceletOptions.Add(new SelectListItem
            {
                Value = NewBraceletValue.ToString(),
                Text = "Create new bracelet",
                Selected = Input.BraceletID == NewBraceletValue
            });

            var beds = await _context.Beds
                .OrderBy(b => b.BedID)
                .Select(b => new
                {
                    b.BedID,
                    Label = $"Bed #{b.BedID} — Room {b.Room}, Sector {b.Sector}"
                })
                .ToListAsync();

            var wards = await _context.Wards
                .OrderBy(w => w.WardName)
                .Select(w => new { w.WardID, w.WardName })
                .ToListAsync();

            var diagnoses = await _context.Diagnoses
                .OrderBy(d => d.DiagnosisName)
                .Select(d => new { d.DiagnosisID, d.DiagnosisName })
                .ToListAsync();

            var medications = await _context.Medications
                .OrderBy(m => m.MedicationName)
                .Select(m => new { m.MedicationID, m.MedicationName })
                .ToListAsync();

            UserOptions = new SelectList(availableUsers, "Id", "Name", Input.UserId);
            BraceletOptions = braceletOptions;
            BedOptions = new SelectList(beds, "BedID", "Label", Input.BedID);
            WardOptions = new SelectList(wards, "WardID", "WardName", Input.WardID);
            DiagnosisOptions = new SelectList(diagnoses, "DiagnosisID", "DiagnosisName", Input.DiagnosisID);
            MedicationOptions = new MultiSelectList(medications, "MedicationID", "MedicationName", Input.MedicationIDs);
        }
    }
}
