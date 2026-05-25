using INFP_Proj.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace INFP_Proj.Pages.User
{
    public class TrackerModel : PageModel
    {
        private readonly VitalsChartService _vitalsChartService;

        // Demo patient linked to seeded user "Sadev IDK" (UserID 4).
        private const int DemoPatientId = 1;

        public TrackerModel(VitalsChartService vitalsChartService)
        {
            _vitalsChartService = vitalsChartService;
        }

        public INFP_Proj.Models.VitalsChartViewModel ChartData { get; set; } = new();

        public async Task OnGetAsync()
        {
            ChartData = await _vitalsChartService.BuildChartModelAsync(DemoPatientId);
        }
    }
}
