using INFP_Proj.Services;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INFP_Proj.Pages.Admin
{
    public class TrackerModel : PageModel
    {
        private readonly VitalsChartService _vitalsChartService;
        private readonly VitalsSimulationService _simulationService;

        public TrackerModel(VitalsChartService vitalsChartService, VitalsSimulationService simulationService)
        {
            _vitalsChartService = vitalsChartService;
            _simulationService = simulationService;
        }

        public AdminVitalsChartViewModel ChartData { get; set; } = new();

        public async Task OnGetAsync([FromQuery] List<int> patientIds)
        {
            ChartData = await _vitalsChartService.BuildAdminMultiPatientChartAsync(patientIds);
        }

        public async Task<IActionResult> OnPostSimulateVitalAsync(int patientId, string vital, string direction)
        {
            VitalsSimulationOutcome outcome = await _simulationService.SimulateVitalAsync(patientId, vital, direction);

            if (outcome.IsError)
            {
                TempData["ErrorMessage"] = outcome.Message;
            }
            else
            {
                TempData["Message"] = outcome.Message;
            }

            return RedirectToPage(new { patientIds = new List<int> { patientId } });
        }
    }
}
