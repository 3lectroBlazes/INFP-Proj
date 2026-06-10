using INFP_Proj.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Data
{
    public class AppDbSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            UserManager<AppUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            RoleManager<AppRole> roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

            // NOTE: ANY EXTRA SEEDING LOGIC SHOULD BE PLACED BELOW THIS CHECK TO AVOID DUPLICATE SEEDING
            //
            // User uses special logic as below 
            // AppUser EXTRA = await CreateUser("First", null, "last", "extra@hospital.com", "User");
            //
            // Example seeding logic for reference:
            // <tableName> <name> = new <tableName> { <tableData> };
            //  E.G Allergies Penicillin = new Allergies { Allergy = "Penicillin" };



            // ^^ Extra seeds above. Please delete after use. ^^ 

            if (userManager.Users.Any()) return;

            // Runs all code ONCE to seed the database with initial data. Extra seeds above ^^

            // Seed Roles
            string[] roles = {"Nurse", "Doctor", "Reception", "User"};

            async Task<AppRole> CreateRole(string role)
            {
                AppRole roles = new AppRole
                {
                    Name = role,
                    IsAdmin = true ? role != "User" : false

                };
                await roleManager.CreateAsync(roles);
                return roles;
            }

            foreach (string role in roles)
            {
                await CreateRole(role);

            }

            // Seed Users
            async Task<AppUser> CreateUser(string firstName, string? middleName, string lastName, string email, string phoneNumber,string role)
            {
                AppUser user = new AppUser
                {
                    FirstName = firstName,
                    MiddleName = middleName,
                    LastName = lastName,
                    UserName = email,
                    Email = email,
                    PhoneNumber = phoneNumber,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, "Password@123");
                await context.SaveChangesAsync();
                await userManager.AddToRoleAsync(user, role);
                return user;
            }

            AppUser kailuo = await CreateUser("Kai", null, "Luo", "kai.luo@hospital.com", "12345678", "Nurse");
            AppUser xavier = await CreateUser("Xavier", null, "Wee", "xavier.wee@hospital.com", "12345678", "Doctor");
            AppUser evan = await CreateUser("Evan", null, "IDK", "evan.idk@hospital.com", "12345678", "Reception");
            AppUser sadev = await CreateUser("Sadev", null, "Mawadavilage", "sadev.mawadavilage@hospital.com", "12345678", "User");
            AppUser miku = await CreateUser("Hatsune", null, "Miku", "hatsune.miku@hospital.com", "12345678", "User");
            AppUser sasha = await CreateUser("Sasha", null, "Sasthi", "sasha.sasthi@hospital.com", "12345678", "User");
            AppUser teto = await CreateUser("Kasane", null, "Teto", "kasane.teto@hospital.com", "12345678", "User");

            // Hospitals
            Hospitals hospital = new Hospitals
            {
                HospitalName = "City General Hospital",
                HospitalAddress = "123 Main Street"
            };
            context.Hospitals.Add(hospital);

            // Wards
            Wards AE = new Wards
            {
                WardName = "A&E",
                MaxCapacity = 20
            };
            Wards general = new Wards
            {
                WardName = "General",
                MaxCapacity = 50
            };
            Wards disease = new Wards
            {
                WardName = "Diseases",
                MaxCapacity = 30
            };
            Wards special = new Wards
            {
                WardName = "Specialized",
                MaxCapacity = 10
            };
            context.Wards.AddRange(AE, general, disease, special);

            // Allergies
            Allergies Penicillin = new Allergies { Allergy = "Penicillin" };
            Allergies Peanuts = new Allergies { Allergy = "Peanuts" };
            Allergies Grass = new Allergies { Allergy = "Grass" };
            context.Allergies.AddRange(Penicillin, Peanuts, Grass);

            // Diagnoses
            Diagnoses diagnosis = new Diagnoses { DiagnosisName = "Hypertension" };
            Diagnoses autism = new Diagnoses { DiagnosisName = "Autism" };
            context.Diagnoses.AddRange(diagnosis, autism);

            // Medications
            Medications paracetamol = new Medications
            {
                MedicationName = "Paracetamol",
                ConsumptionTime = new TimeOnly(8, 0)
            };
            Medications brainrot = new Medications
            {
                MedicationName = "Brainrot",
                Approval = true,
                ConsumptionTime = new TimeOnly(20, 0)
            };
            context.Medications.AddRange(paracetamol, brainrot);

            // Save so IDs are generated before we reference them
            await context.SaveChangesAsync();

            // Bracelet
            Bracelet sadevBracelet = new Bracelet
            {
                Battery = 85.5f,
                Respiration = 18.0f,
                Location = "Ward A",
                Movement = 0.5f,
                BloodPressure = 120.0f,
                HeartRate = 72.0f
            };
            Bracelet mikuBracelet = new Bracelet
            {
                Battery = 90.0f,
                Respiration = 16.0f,
                Location = "Ward A",
                Movement = 0.3f,
                BloodPressure = 110.0f,
                HeartRate = 68.0f
            };

            Bracelet testBracelet = new Bracelet
            {
                Battery = 100.0f,
                Respiration = 16.0f,
                Location = "Ward A",
                Movement = 0.0f,
                BloodPressure = 110.0f,
                HeartRate = 68.0f
            };
            context.Bracelets.AddRange(sadevBracelet, mikuBracelet, testBracelet);
            await context.SaveChangesAsync();

            // Patients
            Patients sadevPatient = new Patients
            {
                UserID = sadev.Id,
                Status = "Admitted"
            };
            Patients tetoPatient = new Patients
            {
                UserID = teto.Id,
                Status = "Admitted"
            };
            context.Patients.AddRange(sadevPatient, tetoPatient);
            await context.SaveChangesAsync();


            // AllergyList
            context.AllergyLists.AddRange(
                new AllergyList { PatientID = sadevPatient.PatientID, AllergyID = Penicillin.AllergyID },
                new AllergyList { PatientID = sadevPatient.PatientID, AllergyID = Peanuts.AllergyID },
                new AllergyList { PatientID = tetoPatient.PatientID, AllergyID = Grass.AllergyID }
            );

            // MedicationList
            MedicationList sadevList = new MedicationList
            {
                PatientID = sadevPatient.PatientID,
                MedicationID = paracetamol.MedicationID,
                Dosage = "500mg"
            };
            MedicationList tetoList = new MedicationList
            {
                PatientID = tetoPatient.PatientID,
                MedicationID = brainrot.MedicationID,
                Dosage = "250mg"
            };
            context.MedicationLists.AddRange(sadevList, tetoList);
            await context.SaveChangesAsync();

            // Beds
            Beds sadevBed = new Beds
            {
                PatientID = sadevPatient.PatientID,
                WardID = general.WardID,
                Sector = "A",
                Floor = "1",
                Room = "101",
                Temperature = 36.5f,
                Weight = 70.0f,
                Location = "Near Window"
            };
            Beds mikuBed = new Beds
            {
                PatientID = tetoPatient.PatientID,
                WardID = general.WardID,
                Sector = "A",
                Floor = "1",
                Room = "102",
                Temperature = 36.6f,
                Weight = 60.0f,
                Location = "Near Door"
            };
            Beds testBed = new Beds
            {
                WardID = general.WardID,
                Sector = "A",
                Floor = "1",
                Room = "103",
                Temperature = 36.6f,
                Weight = 60.0f,
                Location = "Middle"
            };

            context.Beds.AddRange(sadevBed, mikuBed, testBed);
            await context.SaveChangesAsync();

            // Records
            context.Records.Add(new Records
            {
                PatientID = sadevPatient.PatientID,
                BedID = sadevBed.BedID,
                WardID = general.WardID,
                HospitalID = hospital.HospitalID,
                DiagnosisID = diagnosis.DiagnosisID,
                MedicationListID = sadevList.MedicationListID,
                Description = "Patient mentions Severe Chest Pain",
                AdmissionDateTime = DateTime.UtcNow,
                DischargeDateTime = null
            });
            context.Records.Add(new Records
            {
                PatientID = tetoPatient.PatientID,
                BedID = mikuBed.BedID,
                WardID = general.WardID,
                HospitalID = hospital.HospitalID,
                DiagnosisID = autism.DiagnosisID,
                MedicationListID = tetoList.MedicationListID,
                Description = "Patient is constantly singing",
                AdmissionDateTime = DateTime.UtcNow,
                DischargeDateTime = null
            });

            // Vitals
            DateTime vitalsBaseTime = DateTime.Now.AddDays(-6);
            var goodReadings = new (float bp, float hr, float rr, float temp)[]
            {
                (119, 70, 16, 36.4f), (120, 71, 17, 36.5f), (121, 70, 16, 36.4f),
                (119, 72, 17, 36.5f), (120, 71, 16, 36.5f), (121, 70, 17, 36.4f),
                (120, 72, 16, 36.5f), (119, 71, 17, 36.6f), (121, 70, 16, 36.5f),
                (120, 72, 17, 36.4f), (119, 71, 16, 36.5f), (121, 70, 17, 36.5f),
                (120, 72, 16, 36.4f), (119, 71, 17, 36.5f),
            };
            var badReadings = new (float bp, float hr, float rr, float temp)[]
            {
                (118, 72, 17, 36.6f), (116, 74, 18, 36.7f), (114, 76, 18, 36.8f),
                (112, 78, 19, 36.9f), (110, 80, 19, 37.1f), (108, 82, 20, 37.3f),
                (106, 84, 20, 37.5f), (104, 86, 21, 37.6f), (102, 88, 22, 37.8f),
                (100, 90, 22, 37.9f), ( 98, 92, 23, 38.1f), ( 96, 94, 24, 38.3f),
                ( 94, 96, 24, 38.5f), ( 92, 98, 25, 38.7f),
            };

            for (var i = 0; i < goodReadings.Length; i++)
            {
                var sadevReading = goodReadings[i];
                var tetoReading = badReadings[i]; 

                foreach (var (patient, reading) in new[]
                    {
                        (sadevPatient, sadevReading),
                        (tetoPatient, tetoReading)
                    })
                {
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
            }

            // Relationships
            context.Relationships.AddRange(
                new Relationships { PatientID = sadevPatient.PatientID, UserID = sasha.Id },
                new Relationships { PatientID = tetoPatient.PatientID, UserID = teto.Id }
            );

            context.BraceletRelations.AddRange(
                new BraceletRelation { BraceletID = sadevBracelet.BraceletID, PatientID = sadevPatient.PatientID },
                new BraceletRelation { BraceletID = mikuBracelet.BraceletID, PatientID = tetoPatient.PatientID }
            );

            // Logs
            DateTime logBaseTime = DateTime.UtcNow.AddDays(-3);
            context.Logs.AddRange(
                new Log { UserID = xavier.Id, Event = "log test 1", Emergency = false, Timestamp = logBaseTime },
                new Log { UserID = kailuo.Id, Event = "log test 2", Emergency = false, Timestamp = logBaseTime.AddHours(4) },
                new Log { UserID = xavier.Id, Event = "log test 3", Emergency = false, Timestamp = logBaseTime.AddDays(1) },
                new Log { UserID = kailuo.Id, Event = "log test 4", Emergency = true, Timestamp = logBaseTime.AddDays(1).AddHours(6) },
                new Log { UserID = evan.Id, Event = "log test 5", Emergency = false, Timestamp = DateTime.UtcNow },
                new Log { UserID = sadev.Id, Event = "log test 6", Emergency = false, Timestamp = logBaseTime.AddMinutes(30) },
                new Log { UserID = evan.Id, Event = "log test 7", Emergency = false, Timestamp = logBaseTime.AddHours(5) },
                new Log { UserID = sadev.Id, Event = "log test 8", Emergency = false, Timestamp = logBaseTime.AddDays(1).AddHours(2) },
                new Log { UserID = miku.Id, Event = "log test 9", Emergency = true, Timestamp = logBaseTime.AddDays(1).AddHours(7) }
            );

            await context.SaveChangesAsync();
        }
    }
}
