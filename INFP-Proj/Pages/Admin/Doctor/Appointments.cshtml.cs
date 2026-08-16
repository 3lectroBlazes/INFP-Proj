using INFP_Proj.Data;
using INFP_Proj.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin.Doctor
{
    [Authorize(Roles = "Doctor")]
    public class AppointmentsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;

        public AppointmentsModel(AppDbContext context, UserManager<AppUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public List<DoctorAppointmentViewModel> PendingAppointments { get; set; } = new();
        public List<DoctorAppointmentViewModel> UpcomingAppointments { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            // 1. Get the currently logged-in Doctor
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Login");

            // 2. Fetch all future appointments assigned to this specific Doctor
            var allMyAppointments = await (from a in _context.Appointments
                                           join p in _context.Patients on a.PatientID equals p.PatientID
                                           join u in _context.Users on p.UserID equals u.Id
                                           where a.DoctorID == user.Id && a.DateTime >= DateTime.Today
                                           orderby a.DateTime
                                           select new DoctorAppointmentViewModel
                                           {
                                               AppointmentRequestID = a.AppointmentRequestID,
                                               PatientName = $"{u.FirstName} {u.LastName}",
                                               Reason = a.Reason,
                                               Urgency = a.Urgency,
                                               DateTime = a.DateTime,
                                               Status = a.Status,
                                               DocAcknowledged = a.DocAcknowledged,
                                               PatientAcknowledged = a.PatientAcknowledged
                                           }).ToListAsync();

            // 3. Separate into lists for the UI
            PendingAppointments = allMyAppointments.Where(a => !a.DocAcknowledged && a.Status != "Rejected").ToList();
            UpcomingAppointments = allMyAppointments.Where(a => a.DocAcknowledged && a.Status != "Rejected").ToList();

            return Page();
        }
        public async Task<IActionResult> OnPostAcknowledgeAsync(int id, string notes)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToPage("/Login");

            var appointment = await _context.Appointments
                .FirstOrDefaultAsync(a => a.AppointmentRequestID == id);

            if (appointment == null) return NotFound();

            // Make sure this doctor owns the appointment
            if (appointment.DoctorID != user.Id) return Forbid();

            appointment.Status = "Completed";
            appointment.DoctorResponse = notes; 

            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        // ViewModel to easily pass joined data to the frontend
        public class DoctorAppointmentViewModel
        {
            public int AppointmentRequestID { get; set; }
            public string PatientName { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
            public string Urgency { get; set; } = string.Empty;
            public DateTime DateTime { get; set; }
            public string Status { get; set; } = string.Empty;
            public bool DocAcknowledged { get; set; }
            public bool PatientAcknowledged { get; set; }
        }
    }
}