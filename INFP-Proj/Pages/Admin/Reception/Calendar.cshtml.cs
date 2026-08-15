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
        public List<SelectListItem> DoctorsList { get; set; } = new List<SelectListItem>();
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

            var doctorRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Doctor");

            if (doctorRole != null)
            {
                DoctorsList = await _context.UserRoles
                    .Where(ur => ur.RoleId == doctorRole.Id)
                    .Join(_context.Users,
                          ur => ur.UserId,
                          user => user.Id,
                          (ur, user) => new SelectListItem
                          {
                              Value = user.Id,
                              Text = $"Dr. {user.FirstName} {user.LastName}"
                          })
                    .ToListAsync();
            }
            else
            {
                DoctorsList = new List<SelectListItem>();
            }

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
                    start = a.DateTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                    extendedProps = new
                    {
                        patientId = a.PatientID,
                        doctorId = a.DoctorID,
                        urgency = a.Urgency,
                        status = a.Status,
                        docAcknowledged = a.DocAcknowledged,
                        patientAcknowledged = a.PatientAcknowledged,

                        hasPendingChange = _context.AppointmentChangeRequests
                            .Any(r => r.AppointmentRequestID == a.AppointmentRequestID && r.Status == "Pending"),

                        requestedDateTime = _context.AppointmentChangeRequests
                            .Where(r => r.AppointmentRequestID == a.AppointmentRequestID && r.Status == "Pending")
                            .Select(r => (DateTime?)r.RequestedDateTime)
                            .FirstOrDefault()
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
                .AnyAsync(a => a.DateTime == dto.DateTime && a.DoctorID == dto.DoctorID);

            if (isDoubleBooked)
            {
                return new JsonResult(new { success = false, message = "This doctor is already booked at this time." });
            }

            var newAppt = new Appointment
            {
                PatientID = dto.PatientID,
                DoctorID = dto.DoctorID,
                Reason = dto.Reason,
                Urgency = dto.Urgency ?? "Normal",
                Status = "Pending",
                DocAcknowledged = true,
                PatientAcknowledged = false,
                DateTime = dto.DateTime,
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
                .AnyAsync(a => a.DateTime == dto.DateTime && a.DoctorID == dto.DoctorID && a.AppointmentRequestID != dto.AppointmentRequestID);

            if (isDoubleBooked)
            {
                return new JsonResult(new { success = false, message = "This doctor is already booked at this time by another patient." });
            }

            var appt = await _context.Appointments.FindAsync(dto.AppointmentRequestID);
            if (appt == null) return NotFound();

            appt.PatientID = dto.PatientID;
            appt.DoctorID = dto.DoctorID;
            appt.Reason = dto.Reason;
            appt.Urgency = dto.Urgency ?? "Normal";
            appt.DateTime = dto.DateTime;
            appt.DocAcknowledged = true;
            appt.PatientAcknowledged = false;
            appt.Status = "Pending";

            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostAcceptAsync([FromBody] int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            var change = await _context.AppointmentChangeRequests
                .FirstOrDefaultAsync(r => r.AppointmentRequestID == id && r.Status == "Pending");

            if (change != null)
            {
                bool isDoubleBooked = await _context.Appointments
                    .AnyAsync(a => a.DateTime == change.RequestedDateTime && a.DoctorID == appt.DoctorID && a.AppointmentRequestID != id);

                if (isDoubleBooked)
                {
                    return new JsonResult(new { success = false, message = "The requested time is already booked for this doctor." });
                }

                appt.DateTime = change.RequestedDateTime;
                change.Status = "Approved";
                change.ReviewedAt = DateTime.Now;
            }

            appt.DocAcknowledged = true;
            appt.PatientAcknowledged = true;
            appt.Status = "Scheduled";

            await _context.SaveChangesAsync();

            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostRejectAsync([FromBody] int id)
        {
            var appt = await _context.Appointments.FindAsync(id);
            if (appt == null) return NotFound();

            var change = await _context.AppointmentChangeRequests
                .FirstOrDefaultAsync(r => r.AppointmentRequestID == id && r.Status == "Pending");

            if (change != null)
            {
                change.Status = "Rejected";
                change.ReviewedAt = DateTime.Now;
                appt.DocAcknowledged = true;
                appt.PatientAcknowledged = false;
                appt.Status = "Pending";
            }
            else
            {
                appt.Status = "Rejected";
            }

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
            public string DoctorID { get; set; } = string.Empty;
            public string Reason { get; set; } = string.Empty;
            public string Urgency { get; set; } = string.Empty;
            public DateTime DateTime { get; set; }
        }
    }
}