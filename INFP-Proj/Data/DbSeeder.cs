using INFP_Proj.Model;
using INFP_Proj.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Data
{
    public class AppDbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using AppDbContext context = new AppDbContext(serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());
            UserManager<AppUser> userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
            RoleManager<IdentityRole> roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            if (userManager.Users.Any()) return;

            // Seed Roles
            string[] roles = { "User", "Nurse", "Doctor", "Reception", "Patient" };
            /*
             * Admin Roles: Doctor, Nurse, Reception
             * Patient is patient
             * User Role is non-admin non-patient
             */

            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));
            }

            // Seed Users
            async Task<AppUser> CreateUser(string firstName, string? middleName, string lastName, string email, string role)
            {
                AppUser user = new AppUser
                {
                    FirstName = firstName,
                    MiddleName = middleName,
                    LastName = lastName,
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, "Password@123");
                await userManager.AddToRoleAsync(user, role);
                return user;
            }

            AppUser user1 = await CreateUser("Kai", null, "Luo", "kai.luo@hospital.com", "Nurse");
            AppUser user2 = await CreateUser("Xavier", null, "Wee", "xavier.wee@hospital.com", "Doctor");
            AppUser user3 = await CreateUser("Evan", null, "IDK", "evan.idk@hospital.com", "Reception");
            AppUser user4 = await CreateUser("Sadev", null, "IDK", "sadev.idk@hospital.com", "Patient");
            AppUser user5 = await CreateUser("Skibidi", null, "Toilet", "skibidi.toilet@hospital.com", "Patient");
            AppUser user6 = await CreateUser("Yes", null, "No", "yes.no@hospital.com", "User");

            // Hospitals
            Hospitals hospital = new Hospitals
            {
                HospitalName = "City General Hospital",
                HospitalAddress = "123 Main Street"
            };
            context.Hospitals.Add(hospital);

            // Wards
            Wards ward = new Wards
            {
                WardName = "Ward A",
                MaxCapacity = 20
            };
            context.Wards.Add(ward);

            // Allergies
            Allergies allergy1 = new Allergies { Allergy = "Penicillin" };
            Allergies allergy2 = new Allergies { Allergy = "Peanuts" };
            context.Allergies.AddRange(allergy1, allergy2);

            // Diagnoses
            Diagnoses diagnosis = new Diagnoses { DiagnosisName = "Hypertension" };
            context.Diagnoses.Add(diagnosis);

            // Medications
            Medications medication = new Medications
            {
                MedicationName = "Paracetamol",
                ConsumptionTime = new TimeOnly(8, 0)
            };
            context.Medications.Add(medication);

            // Save so IDs are generated before we reference them
            await context.SaveChangesAsync();

            // Bracelet
            Bracelet bracelet = new Bracelet
            {
                PatientID = 0,
                Battery = 85.5f,
                Respiration = 18.0f,
                Location = "Ward A",
                Movement = 0.5f,
                BloodPressure = 120.0f,
                HeartRate = 72.0f
            };
            context.Bracelets.Add(bracelet);
            await context.SaveChangesAsync();

            // Patients
            Patients patient = new Patients
            {
                BraceletID = bracelet.BraceletID,
                UserID = user4.Id,
                Status = "Admitted"
            };
            context.Patients.Add(patient);
            await context.SaveChangesAsync();

            // Update bracelet to point to patient
            bracelet.PatientID = patient.PatientID;
            await context.SaveChangesAsync();

            // AllergyList
            context.AllergyLists.AddRange(
                new AllergyList { PatientID = patient.PatientID, AllergyID = allergy1.AllergyID },
                new AllergyList { PatientID = patient.PatientID, AllergyID = allergy2.AllergyID }
            );

            // MedicationList
            MedicationList medList = new MedicationList
            {
                PatientID = patient.PatientID,
                MedicationID = medication.MedicationID,
                Dosage = "500mg"
            };
            context.MedicationLists.Add(medList);
            await context.SaveChangesAsync();

            // Beds
            Beds bed = new Beds
            {
                PatientID = patient.PatientID,
                WardID = ward.WardID,
                Sector = "A",
                Floor = "1",
                Room = "101",
                Temperature = 36.5f,
                Weight = 70.0f,
                Location = "Near Window"
            };
            context.Beds.Add(bed);
            await context.SaveChangesAsync();

            // Records
            context.Records.Add(new Records
            {
                PatientID = patient.PatientID,
                BedID = bed.BedID,
                WardID = ward.WardID,
                HospitalID = hospital.HospitalID,
                DiagnosisID = diagnosis.DiagnosisID,
                MedicationListID = medList.MedicationListID,
                Description = "Patient admitted for monitoring",
                AdmissionDateTime = DateTime.UtcNow,
                DischargeDateTime = null
            });

            // Vitals
            DateTime vitalsBaseTime = DateTime.UtcNow.AddDays(-6);
            var vitalsReadings = new (float bp, float hr, float rr, float temp)[]
            {
                (118, 68, 16, 36.4f), (120, 70, 17, 36.5f), (122, 72, 18, 36.5f),
                (119, 74, 17, 36.6f), (121, 71, 18, 36.4f), (123, 75, 19, 36.7f),
                (120, 73, 18, 36.5f), (118, 69, 16, 36.3f), (122, 76, 19, 36.6f),
                (121, 72, 18, 36.5f), (119, 70, 17, 36.4f), (124, 77, 20, 36.8f),
                (120, 71, 18, 36.5f), (118, 68, 16, 36.4f)
            };

            for (int i = 0; i < vitalsReadings.Length; i++)
            {
                var reading = vitalsReadings[i];
                context.Vitals.Add(new Vitals
                {
                    PatientID = patient.PatientID,
                    BloodPressure = reading.bp,
                    HeartRate = reading.hr,
                    RespiratoryRate = reading.rr,
                    Temperature = reading.temp,
                    RecordedAt = vitalsBaseTime.AddHours(i * 12)
                });
            }

            // Relationships
            context.Relationships.AddRange(
                new Relationships { PatientID = patient.PatientID, UserID = user1.Id },
                new Relationships { PatientID = patient.PatientID, UserID = user2.Id }
            );

            // Logs
            DateTime logBaseTime = DateTime.UtcNow.AddDays(-3);
            context.Logs.AddRange(
                new Log { UserID = user1.Id, Event = "log test 1", Emergency = false, Timestamp = logBaseTime },
                new Log { UserID = user2.Id, Event = "log test 2", Emergency = false, Timestamp = logBaseTime.AddHours(4) },
                new Log { UserID = user1.Id, Event = "log test 3", Emergency = false, Timestamp = logBaseTime.AddDays(1) },
                new Log { UserID = user2.Id, Event = "log test 4", Emergency = true, Timestamp = logBaseTime.AddDays(1).AddHours(6) },
                new Log { UserID = user1.Id, Event = "log test 5", Emergency = false, Timestamp = DateTime.UtcNow },
                new Log { UserID = user4.Id, Event = "log test 6", Emergency = false, Timestamp = logBaseTime.AddMinutes(30) },
                new Log { UserID = user4.Id, Event = "log test 7", Emergency = false, Timestamp = logBaseTime.AddHours(5) },
                new Log { UserID = user4.Id, Event = "log test 8", Emergency = false, Timestamp = logBaseTime.AddDays(1).AddHours(2) },
                new Log { UserID = user4.Id, Event = "log test 9", Emergency = true, Timestamp = logBaseTime.AddDays(1).AddHours(7) }
            );

            await context.SaveChangesAsync();
        }
    }
}