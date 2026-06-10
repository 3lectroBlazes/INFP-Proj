using INFP_Proj.Data;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin.Reception
{
    [Authorize(Roles = "Reception")]
    public class WardBedChangeModel : PageModel
    {
        private readonly AppDbContext _context;

        public WardBedChangeModel(AppDbContext context)
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

        public async Task OnGetAsync()
        {
            await PopulatePageDataAsync();
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