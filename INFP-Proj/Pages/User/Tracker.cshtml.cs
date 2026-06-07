using INFP_Proj.Data;
using INFP_Proj.Models;
using INFP_Proj.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.User
{
    [Authorize]
    public class TrackerModel : PageModel
    {
        private readonly VitalsChartService _vitalsChartService;
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public TrackerModel(
            VitalsChartService vitalsChartService,
            AppDbContext context,
            UserManager<AppUser> userManager)
        {
            _vitalsChartService = vitalsChartService;
            _context = context;
            _userManager = userManager;
        }

        public VitalsChartViewModel ChartData { get; set; } = new();
        public bool HasPatientRecord { get; set; }

        public async Task OnGetAsync()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return;
            }

            var patientId = await _context.Patients
                .Where(p => p.UserID == userId)
                .Select(p => p.PatientID)
                .FirstOrDefaultAsync();

            if (patientId == 0)
            {
                return;
            }

            HasPatientRecord = true;
            ChartData = await _vitalsChartService.BuildChartModelAsync(patientId);
        }
    }
}
