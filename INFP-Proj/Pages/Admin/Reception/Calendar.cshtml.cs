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
            // 1. Fetch Patients
            PatientsList = await _context.Patients
                .Include(p => p.User)
                .Select(p => new SelectListItem
                {
                    Value = p.PatientID.ToString(),
                    Text = p.User != null ? $"{p.User.FirstName} {p.User.LastName} (ID: {p.PatientID})" : $"Patient ID: {p.PatientID}"
                })
                .ToListAsync();

            // 2. Fetch Doctors via UserID and Role
            var doctorRoleId = await _context.Roles
                .Where(r => r.Name == "Doctor")
                .Select(r => r.Id)
                .FirstOrDefaultAsync();

            if (!string.IsNullOrEmpty(doctorRoleId))
            {
                DoctorsList = await _context.Users
                    .Join(_context.UserRoles, user => user.Id, ur => ur.UserId, (user, ur) => new { user, ur })
                    .Where(joined => joined.ur.RoleId == doctorRoleId)
                    .Select(joined => new SelectListItem
                    {
                        Value = joined.user.Id, // This is the AppUser ID
                        Text = $"Dr. {joined.user.FirstName} {joined.user.LastName}"
                    })
                    .ToListAsync();
            }

            // 3. Generate Times
            for (int i = 9; i <= 17; i++)
            {
                DateTime time = DateTime.Today.AddHours(i);
                TimeSlots.Add(new SelectListItem { Value = time.ToString("HH:mm"), Text = time.ToString("hh:mm tt") });
            }
        }

        public async Task<JsonResult> OnGetFetchAppointmentsAsync()
        {
            // Fetch all appointments into memory first to parse the hijacked string
            var appointments = await _context.Appointments
                .Include(a => a.Patient).ThenInclude(p => p.User)
                .ToListAsync();

            // Create a dictionary of doctors for quick lookup
            var doctorRoleId = await _context.Roles.Where(r => r.Name == "Doctor").Select(r => r.Id).FirstOrDefaultAsync();
            var doctors = await _context.Users
                .Join(_context.UserRoles, u => u.Id, ur => ur.UserId, (u, ur) => new { u, ur })
                .Where(j => j.ur.RoleId == doctorRoleId)
                .ToDictionaryAsync(j => j.u.Id, j => $"Dr. {j.u.FirstName} {j.u.LastName}");

            var result = appointments.Select(a =>
            {
                string doctorId = null;
                string docName = "Unassigned";
                string actualResponse = a.DoctorResponse;

                // Decode the UserID from the DoctorResponse field
                if (!string.IsNullOrEmpty(a.DoctorResponse) && a.DoctorResponse.StartsWith("DOC:"))
                {
                    var parts = a.DoctorResponse.Split('|', 2);
                    doctorId = parts[0].Substring(4); // Remove "DOC:"
                    actualResponse = parts.Length > 1 ? parts[1] : null;

                    if (doctors.ContainsKey(doctorId))
                    {
                        docName = doctors[doctorId];
                    }
                }

                string patientName = a.Patient?.User != null ? $"{a.Patient.User.FirstName} {a.Patient.User.LastName}" : "Unknown";

                // Assign a color based on the appointment urgency
                string eventColor = a.Urgency switch
                {
                    "Emergency" => "#dc3545", // Red
                    "Urgent" => "#fd7e14",    // Orange
                    _ => "#0d6efd"            // Blue (Default/Normal)
                };

                return new
                {
                    id = a.AppointmentRequestID,
                    title = $"{patientName} -> {docName}",
                    start = a.AppointmentDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    color = eventColor, // FullCalendar will read this to color the block
                    extendedProps = new
                    {
                        patientId = a.PatientID,
                        patientName = patientName,
                        doctorId = doctorId,
                        doctorName = docName,
                        reason = a.Reason,
                        urgency = a.Urgency,
                        status = a.Status
                    }
                };
            });

            return new JsonResult(result);
        }

        public async Task<IActionResult> OnPostAddAsync([FromBody] AppointmentDto dto)
        {
            if (dto == null || dto.DateTime < DateTime.Now) return BadRequest();

            // Encode the Doctor ID into the response field
            string encodedDoctor = !string.IsNullOrEmpty(dto.DoctorID) ? $"DOC:{dto.DoctorID}|" : null;

            var newAppt = new Appointment
            {
                PatientID = dto.PatientID,
                Reason = dto.Reason,
                Urgency = dto.Urgency ?? "Normal",
                Status = "Pending",
                AppointmentDate = dto.DateTime,
                RequestedAt = DateTime.Now,
                DoctorResponse = encodedDoctor
            };

            _context.Appointments.Add(newAppt);
            await _context.SaveChangesAsync();
            return new JsonResult(new { success = true });
        }

        public async Task<IActionResult> OnPostUpdateAsync([FromBody] AppointmentUpdateDto dto)
        {
            if (dto == null || dto.AppointmentRequestID == 0 || dto.DateTime < DateTime.Now) return BadRequest();

            var appt = await _context.Appointments.FindAsync(dto.AppointmentRequestID);
            if (appt == null) return NotFound();

            // Safely extract the existing real response if one exists
            string actualResponse = "";
            if (!string.IsNullOrEmpty(appt.DoctorResponse) && appt.DoctorResponse.StartsWith("DOC:"))
            {
                var parts = appt.DoctorResponse.Split('|', 2);
                if (parts.Length > 1) actualResponse = parts[1];
            }
            else
            {
                actualResponse = appt.DoctorResponse ?? "";
            }

            // Re-encode with the updated doctor ID
            appt.DoctorResponse = !string.IsNullOrEmpty(dto.DoctorID) ? $"DOC:{dto.DoctorID}|{actualResponse}" : actualResponse;
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
            public string? DoctorID { get; set; }
            public string Reason { get; set; } = string.Empty;
            public string Urgency { get; set; } = string.Empty;
            public DateTime DateTime { get; set; }
        }

        public class AppointmentUpdateDto
        {
            public int AppointmentRequestID { get; set; }
            public string? DoctorID { get; set; }
            public string Urgency { get; set; } = string.Empty;
            public DateTime DateTime { get; set; }
        }
    }
}