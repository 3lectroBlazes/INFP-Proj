using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using INFP_Proj.Data;

namespace INFP_Proj.Pages.Admin
{
    public class IndexModel : PageModel
    {
        private readonly INFP_Proj.Data.AppDbContext _context;

        public IndexModel(INFP_Proj.Data.AppDbContext context)
        {
            _context = context;
        }

        public IList<Patients> Patients { get;set; } = default!;

        public async Task OnGetAsync()
        {
            Patients = await _context.Patients
                .Include(p => p.Bracelet)
                .Include(p => p.User).ToListAsync();
        }
    }
}
