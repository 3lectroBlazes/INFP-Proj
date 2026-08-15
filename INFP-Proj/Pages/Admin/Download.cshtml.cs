using INFP_Proj.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin
{
    public class DownloadModel : PageModel
    {
        private readonly AppDbContext _context;

        public DownloadModel(AppDbContext context)
        {
            _context = context;
        }

        public List<DeathCerts> Certificates { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? Search { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            if (User.IsInRole("Nurse"))
            {
                TempData["Error"] = "You do not have permission to view death certificates.";
                return RedirectToPage("/Admin/Index");
            }

            var query = _context.DeathCerts.AsQueryable();

            if (!string.IsNullOrWhiteSpace(Search))
            {
                var term = Search.Trim();
                query = query.Where(d => EF.Functions.Like(d.PatientName, $"%{term}%"));
            }

            Certificates = await query
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();

            return Page();
        }

        public async Task<IActionResult> OnGetDownloadAsync(int id)
        {
            var cert = await _context.DeathCerts.FirstOrDefaultAsync(d => d.DeathCertID == id);
            if (cert == null)
            {
                return NotFound();
            }
            return File(cert.PdfData, cert.ContentType, cert.FileName);
        }
    }
}