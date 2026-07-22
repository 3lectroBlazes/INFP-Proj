using INFP_Proj.Data;
using INFP_Proj.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Pages.Admin.Reception
{
    public class CalendarModel : PageModel
    {
        private readonly AppDbContext _context;

        public CalendarModel(AppDbContext context)
        {
            _context = context;
        }

        public List<SelectListItem> PatientsList { get; set; } = new List<SelectListItem>();

        public List<SelectListItem> TimeSlots { get; set; } = new List<SelectListItem>();

        public async Task OnGetAsync()
        {
            PatientsList = await _context.Patients
                .Join(_context.Users,
                      patient => patient.UserID,
                      user => user.Id,
                      (patient, user) => new SelectListItem
                      {
                          Value = patient.PatientID.ToString(),
                          Text = $"{user.FirstName} {user.LastName} (ID: {patient.PatientID})"
                      })
                .ToListAsync();

            int startHour = 9;  
            int endHour = 17;  

            for (int i = startHour; i <= endHour; i++)
            {
                DateTime time = DateTime.Today.AddHours(i);
                TimeSlots.Add(new SelectListItem
                {
                    Value = time.ToString("HH:mm"),  
                    Text = time.ToString("hh:mm tt") 
                });
            }
        }

        public async Task<JsonResult> OnGetFetchAppointmentsAsync()
        {
            var appointments = await _context.Appointments
                .Select(a => new
                {
                    id = a.AppointmentRequestID,
                    title = a.Reason,
                    start = a.AppointmentDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    extendedProps = new
                    {
                        patientId = a.PatientID,
                        urgency = a.Urgency,
                        status = a.Status
                    }
                })
                .ToListAsync();

            return new JsonResult(appointments);
        }

        public async Task<IActionResult> OnPostAddAsync([FromBody] AppointmentDto dto)
        {
            if (dto == null) return BadRequest();

            if (dto.DateTime < DateTime.Now)
            {
                return new JsonResult(new { success = false, message = "Appointments cannot be scheduled in the past." });
            }

            bool isDoubleBooked = await _context.Appointments
                .AnyAsync(a => a.AppointmentDate == dto.DateTime);

            if (isDoubleBooked)
            {
                return new JsonResult(new { success = false, message = "This time slot is already booked. Please select another time." });
            }

            var newAppt = new Appointment
            {
                PatientID = dto.PatientID,
                Reason = dto.Reason,
                Urgency = dto.Urgency ?? "Normal",
                Status = "Pending",
                AppointmentDate = dto.DateTime,
                RequestedAt = DateTime.Now
            };

            _context.Appointments.Add(newAppt);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostUpdateAsync([FromBody] AppointmentDto dto)
        {
            if (dto == null || dto.AppointmentRequestID == 0) return BadRequest();

            if (dto.DateTime < DateTime.Now)
            {
                return new JsonResult(new { success = false, message = "Appointments cannot be scheduled in the past." });
            }

            bool isDoubleBooked = await _context.Appointments
                .AnyAsync(a => a.AppointmentDate == dto.DateTime && a.AppointmentRequestID != dto.AppointmentRequestID);

            if (isDoubleBooked)
            {
                return new JsonResult(new { success = false, message = "This time slot is already booked by another patient." });
            }

            var appt = await _context.Appointments.FindAsync(dto.AppointmentRequestID);
            if (appt == null) return NotFound();

            appt.PatientID = dto.PatientID;
            appt.Reason = dto.Reason;
            appt.Urgency = dto.Urgency ?? "Normal";
            appt.AppointmentDate = dto.DateTime;

            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostDeleteAsync([FromBody] int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            _context.Appointments.Remove(appt);
            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public class AppointmentDto
        {
            public int AppointmentRequestID { get; set; }
            public int PatientID { get; set; }
            public string Reason { get; set; } = string.Empty;
            public string Urgency { get; set; } = string.Empty;
            public DateTime DateTime { get; set; }
        }
    }
}