using INFP_Proj.Data;
using INFP_Proj.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace INFP_Proj.Pages.Admin.Reception
{
    [Authorize(Roles = "Reception")]
    public class RegisterPatientModel : PageModel
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly AppDbContext _context;

        public RegisterPatientModel(UserManager<AppUser> userManager, AppDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        [BindProperty]
        public RegisterPatientViewModel Input { get; set; } = new RegisterPatientViewModel();

        [BindProperty]
        public int? SelectedWardID { get; set; }

        public List<SelectListItem> AvailableWards { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableBracelets { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableBeds { get; set; } = new List<SelectListItem>();

        public void OnGet()
        {
            PopulateWards();
            PopulateAllBracelets();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                PopulateWards();
                PopulateAllBracelets();
                if (SelectedWardID.HasValue)
                {
                    PopulateBedsForWard(SelectedWardID.Value);
                }
                return Page();
            }

            AppUser user = new AppUser
            {
                UserName = Input.Email,
                Email = Input.Email,
                FirstName = Input.FirstName,
                MiddleName = Input.MiddleName,
                LastName = Input.LastName,
                EmailConfirmed = true
            };

            IdentityResult result = await _userManager.CreateAsync(user, "TempPass123!");

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "User");

                Patients patient = new Patients
                {
                    UserID = user.Id,
                    Status = "Admitted"
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();

                BraceletRelation braceletRelation = new BraceletRelation
                {
                    PatientID = patient.PatientID,
                    BraceletID = Input.BraceletID
                };
                _context.BraceletRelations.Add(braceletRelation);

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
                    DiagnosisID = 1,
                    MedicationListID = 1,
                    Description = Input.Description,
                    AdmissionDateTime = DateTime.UtcNow
                };
                _context.Records.Add(patrec);

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = $"Patient {Input.FirstName} {Input.LastName} registered successfully. Assigned to Bed #{Input.BedID} (Bracelet #{Input.BraceletID} located in Ward {assignedWardID}).";
                return RedirectToPage("./RegisterPatient");
            }

            foreach (IdentityError? error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            PopulateWards();
            PopulateAllBracelets();
            if (SelectedWardID.HasValue)
            {
                PopulateBedsForWard(SelectedWardID.Value);
            }
            return Page();
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