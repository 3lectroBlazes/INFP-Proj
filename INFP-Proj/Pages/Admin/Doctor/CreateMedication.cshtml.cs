using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using INFP_Proj.Data;

namespace INFP_Proj.Pages.Admin.Doctor;

public class CreateMedicationModel : PageModel
{
    private readonly AppDbContext _context;

    public CreateMedicationModel(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    [BindProperty]
    public Medications Medications { get; set; } = default!;

    // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.Medications.Add(Medications);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
