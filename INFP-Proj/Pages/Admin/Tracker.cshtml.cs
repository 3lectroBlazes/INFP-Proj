using INFP_Proj.Services;
using INFP_Proj.ViewModel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INFP_Proj.Pages.Admin
{
    public class TrackerModel : PageModel
    {
        private readonly VitalsChartService _vitalsChartService;

        public TrackerModel(VitalsChartService vitalsChartService)
        {
            _vitalsChartService = vitalsChartService;
        }

        public AdminVitalsChartViewModel ChartData { get; set; } = new();

        public async Task OnGetAsync([FromQuery] List<int> patientIds)
        {
            ChartData = await _vitalsChartService.BuildAdminMultiPatientChartAsync(patientIds);
        }
    }
}
