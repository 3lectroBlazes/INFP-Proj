using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using INFP_Proj.Data;

namespace INFP_Proj.Pages.Admin
{
    public class CreateModel : PageModel
    {
        private readonly INFP_Proj.Data.AppDbContext _context;

        public CreateModel(INFP_Proj.Data.AppDbContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
        ViewData["BraceletID"] = new SelectList(_context.Bracelets, "BraceletID", "BraceletID");
        ViewData["UserID"] = new SelectList(_context.Users, "UserID", "UserID");
            return Page();
        }

        [BindProperty]
        public Patients Patients { get; set; } = default!;

        // For more information, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Patients.Add(Patients);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
