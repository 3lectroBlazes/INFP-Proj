using System.Threading.Tasks;
using INFP_Proj.Data;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin.Reception
{
    public class TransferPatientModel : PageModel
    {
        private readonly AppDbContext _context;

        public TransferPatientModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TransferPatientViewModel Input { get; set; } = new TransferPatientViewModel();
        public List<SelectListItem> AdmittedPatients { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> AvailableBeds { get; set; } = new List<SelectListItem>();
        public List<WardStatViewModel> WardStats { get; set; } = new List<WardStatViewModel>();
        public int TotalHospitalCapacity { get; set; }
        public int TotalOccupied { get; set; }
        public int TotalAvailable { get; set; }


        public async Task OnGet()
        {
            await PopulatePageDataAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await PopulatePageDataAsync();
                return Page();
            }

            Beds? newBed = await _context.Beds.Include(b => b.Wards).FirstOrDefaultAsync(b => b.BedID == Input.NewBedID);
            if (newBed == null || newBed.PatientID != null)
            {
                ModelState.AddModelError("Input.NewBedID", "This bed is no longer available.");
                await PopulatePageDataAsync();
                return Page();
            }

            Beds? oldBed = await _context.Beds.FirstOrDefaultAsync(b => b.PatientID == Input.PatientID);
            if (oldBed != null)
            {
                oldBed.PatientID = null;
            }

            newBed.PatientID = Input.PatientID;

            Records? latestRecord = await _context.Records
                .Where(r => r.PatientID == Input.PatientID)
                .OrderByDescending(r => r.AdmissionDateTime)
                .FirstOrDefaultAsync();

            if (latestRecord != null)
            {
                latestRecord.BedID = newBed.BedID;
                latestRecord.WardID = newBed.WardID;
                string wardName = newBed.Wards?.WardName ?? "Unknown Ward";
                latestRecord.Description += $"\n[Transferred to Bed {newBed.BedID} ({wardName}) on {DateTime.UtcNow:g}]";
            }

            BraceletRelation? braceletRelation = await _context.BraceletRelations
                .FirstOrDefaultAsync(br => br.PatientID == Input.PatientID);

            if (braceletRelation != null)
            {
                Bracelet? assignedBracelet = await _context.Bracelets.FindAsync(braceletRelation.BraceletID);
                if (assignedBracelet != null)
                {
                    assignedBracelet.Location = $"Ward {newBed.WardID}";
                }
            }
            await _context.SaveChangesAsync();

            string displayWardName = newBed.Wards?.WardName ?? $"Ward {newBed.WardID}";
            TempData["SuccessMessage"] = $"Patient successfully transferred to Bed #{newBed.BedID} in the {displayWardName} ward.";

            return RedirectToPage("./WardBedChange");
        }

        private async Task PopulatePageDataAsync()
        {
            List<Beds> allBeds = await _context.Beds.Include(b => b.Wards).ToListAsync();
            List<Wards> allWards = await _context.Wards.ToListAsync();
            List<Beds> patientsInBeds = allBeds.Where(b => b.PatientID != null).ToList();
            List<int?> admittedPatientIds = patientsInBeds.Select(b => b.PatientID).ToList();

            List<Patients> patients = await _context.Patients
                .Include(p => p.User)
                .Where(p => admittedPatientIds.Contains(p.PatientID))
                .ToListAsync();

            AdmittedPatients = patients.Select(p => {
                Beds currentBed = patientsInBeds.First(b => b.PatientID == p.PatientID);
                string wardName = currentBed.Wards?.WardName ?? $"Ward {currentBed.WardID}";
                return new SelectListItem
                {
                    Value = p.PatientID.ToString(),
                    Text = $"{p.User.FirstName} {p.User.LastName} (Current: Bed {currentBed.BedID}, {wardName})"
                };
            }).ToList();

            AvailableBeds = allBeds
                .Where(b => b.PatientID == null)
                .Select(b => new SelectListItem
                {
                    Value = b.BedID.ToString(),
                    Text = $"Bed #{b.BedID} - Room {b.Room} ({b.Wards?.WardName})"
                })
                .ToList();

            WardStats.Clear();
            foreach (Wards? ward in allWards)
            {
                List<Beds> bedsInWard = allBeds.Where(b => b.WardID == ward.WardID).ToList();
                WardStats.Add(new WardStatViewModel
                {
                    WardID = ward.WardID,
                    WardName = ward.WardName,
                    MaxCapacity = ward.MaxCapacity,
                    OccupiedBeds = bedsInWard.Count(b => b.PatientID != null),
                    AvailableBeds = bedsInWard.Count(b => b.PatientID == null)
                });
            }

            TotalHospitalCapacity = WardStats.Sum(w => w.MaxCapacity);
            TotalOccupied = WardStats.Sum(w => w.OccupiedBeds);
            TotalAvailable = WardStats.Sum(w => w.AvailableBeds);
        }
    }
}
