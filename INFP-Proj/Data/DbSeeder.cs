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

            // Vitals — matches Vitals table: PatientID (FK), nullable real metrics, RecordedAt (datetime2 UTC)
            // VitalsID is database-generated (identity); do not set it in seed data.
            SeedVitalsForPatient(context, patient, DateTime.UtcNow.AddDays(-6));

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

        /// <summary>
        /// Seeds Vitals rows for a patient per schema: PatientID FK, optional floats, RecordedAt.
        /// </summary>
        private static void SeedVitalsForPatient(AppDbContext context, Patients patient, DateTime baseRecordedAtUtc)
        {
            (float? bloodPressure, float? heartRate, float? respiratoryRate, float? temperature)[] readings =
            {
                (118f, 68f, 16f, 36.4f),
                (120f, 70f, 17f, 36.5f),
                (122f, 72f, 18f, 36.5f),
                (119f, 74f, 17f, 36.6f),
                (121f, 71f, 18f, 36.4f),
                (123f, 75f, 19f, 36.7f),
                (120f, 73f, 18f, 36.5f),
                (118f, 69f, 16f, 36.3f),
                (122f, 76f, 19f, 36.6f),
                (121f, 72f, 18f, 36.5f),
                (119f, 70f, 17f, 36.4f),
                (124f, 77f, 20f, 36.8f),
                (120f, 71f, 18f, 36.5f),
                (118f, 68f, 16f, 36.4f)
            };

            List<Vitals> vitals = new List<Vitals>();
            for (int i = 0; i < readings.Length; i++)
            {
                (float? bloodPressure, float? heartRate, float? respiratoryRate, float? temperature) reading = readings[i];
                vitals.Add(new Vitals
                {
                    PatientID = patient.PatientID,
                    Patients = patient,
                    BloodPressure = reading.bloodPressure,
                    HeartRate = reading.heartRate,
                    RespiratoryRate = reading.respiratoryRate,
                    Temperature = reading.temperature,
                    RecordedAt = DateTime.SpecifyKind(baseRecordedAtUtc.AddHours(i * 12), DateTimeKind.Utc)
                });
            }

            context.Vitals.AddRange(vitals);
        }

        /// <summary>
        /// When users were seeded earlier but vitals are missing, backfill vitals for the first patient.
        /// </summary>
        public static async Task SeedVitalsIfMissingAsync(IServiceProvider serviceProvider)
        {
            using AppDbContext context = new AppDbContext(serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());

            if (await context.Vitals.AnyAsync())
            {
                return;
            }

            Patients? patient = await context.Patients.OrderBy(p => p.PatientID).FirstOrDefaultAsync();
            if (patient == null)
            {
                return;
            }

            SeedVitalsForPatient(context, patient, DateTime.UtcNow.AddDays(-6));
            await context.SaveChangesAsync();
        }
    }
}