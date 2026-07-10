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

        public async Task<IActionResult> OnPostSimulateStepAsync([FromForm] int patientId, [FromForm] string vital, [FromForm] string direction)
        {
            var reading = await _simulationService.RecordSimulatedReadingAsync(patientId, vital, direction);
            if (reading == null)
            {
                return NotFound();
            }

            return new JsonResult(new { success = true, recordedAt = reading.RecordedAt });
        }
    }
}
