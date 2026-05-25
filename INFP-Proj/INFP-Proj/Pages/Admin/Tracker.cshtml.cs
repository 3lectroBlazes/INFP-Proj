using INFP_Proj.Services;
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

        public INFP_Proj.Models.VitalsChartViewModel ChartData { get; set; } = new();

        public async Task OnGetAsync(int? patientId)
        {
            var selectedPatientId = patientId ?? 1;
            ChartData = await _vitalsChartService.BuildChartModelAsync(
                selectedPatientId,
                showPatientSelector: true);
        }
    }
}
