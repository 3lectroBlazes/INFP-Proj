using Microsoft.EntityFrameworkCore;

namespace INFP_Proj.Data
{
    public class AppDbSeeder
    {
        public static void Seed(IServiceProvider serviceProvider)
        {
            using var context = new AppDbContext(serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());

            // If any users exist, already seeded
            if (context.Users.Any()) return;

            // Hospitals
            var hospital = new Hospitals
            {
                HospitalID = 1,
                HospitalName = "City General Hospital",
                HospitalAddress = "123 Main Street"
            };
            context.Hospitals.Add(hospital);

            // Wards
            var ward = new Wards
            {
                WardID = 1,
                WardName = "Ward A",
                MaxCapacity = 20
            };
            context.Wards.Add(ward);

            // Users
            var user1 = new User
            {
                UserID = 1,
                FirstName = "Kai",
                MiddleName = null,
                LastName = "Luo",
                Email = "kai.luo@hospital.com",
                Role = "Nurse",
                PasswordHash = "hashed_password_here"
            };
            var user2 = new User
            {
                UserID = 2,
                FirstName = "Xavier",
                MiddleName = null,
                LastName = "Wee",
                Email = "xavier.wee@hospital.com",
                Role = "Doctor",
                PasswordHash = "hashed_password_here"
            };
            var user3 = new User
            {
                UserID = 3,
                FirstName = "Evan",
                MiddleName = null,
                LastName = "IDK",
                Email = "evan.idk@hospital.com",
                Role = "Reception",
                PasswordHash = "hashed_password_here"
            };
            var user4 = new User
            {
                UserID = 4,
                FirstName = "Sadev",
                MiddleName = null,
                LastName = "IDK",
                Email = "sadev.idk@hospital.com",
                Role = "Patient",
                PasswordHash = "hashed_password_here"
            };
            var user5 = new User
            {
                UserID = 5,
                FirstName = "Skibidi",
                MiddleName = null,
                LastName = "Toilet",
                Email = "skibidi.toilet@hospital.com",
                Role = "Patient",
                PasswordHash = "hashed_password_here"
            };
            context.Users.AddRange(user1, user2, user3, user4, user5);

            // Allergies
            var allergy1 = new Allergies { AllergyID = 1, Allergy = "Penicillin" };
            var allergy2 = new Allergies { AllergyID = 2, Allergy = "Peanuts" };
            context.Allergies.AddRange(allergy1, allergy2);

            // Diagnoses
            var diagnosis = new Diagnoses { DiagnosisID = 1, DiagnosisName = "Hypertension" };
            context.Diagnoses.Add(diagnosis);

            // Medications
            var medication = new Medications
            {
                MedicationID = 1,
                MedicationName = "Paracetamol",
                ConsumptionTime = new TimeOnly(8, 0)
            };
            context.Medications.Add(medication);

            // Bracelet
            var bracelet = new Bracelet
            {
                BraceletID = 1,
                PatientID = 1,
                Battery = 85.5f,
                Respiration = 18.0f,
                Location = "Ward A",
                Movement = 0.5f,
                BloodPressure = 120.0f,
                HeartRate = 72.0f
            };
            context.Bracelets.Add(bracelet);

            // Patients
            var patient = new Patients
            {
                PatientID = 1,
                BraceletID = 1,
                UserID = 4,
                Status = "Admitted"
            };
            context.Patients.Add(patient);

            // AllergyList
            context.AllergyLists.AddRange(
                new AllergyList { AllergyListID = 1, PatientID = 1, AllergyID = 1 },
                new AllergyList { AllergyListID = 2, PatientID = 1, AllergyID = 2 }
            );

            // MedicationList
            var medList = new MedicationList
            {
                MedicationListID = 1,
                PatientID = 1,
                MedicationID = 1,
                Dosage = "500mg"
            };
            context.MedicationLists.Add(medList);

            // Beds
            var bed = new Beds
            {
                BedID = 1,
                PatientID = 1,
                WardID = 1,
                Sector = "A",
                Floor = "1",
                Room = "101",
                Temperature = 36.5f,
                Weight = 70.0f,
                Location = "Near Window"
            };
            context.Beds.Add(bed);

            // Records
            var record = new Records
            {
                RecordID = 1,
                PatientID = 1,
                BedID = 1,
                WardID = 1,
                HospitalID = 1,
                DiagnosisID = 1,
                MedicationListID = 1,
                Description = "Patient admitted for monitoring",
                AdmissionDateTime = DateTime.UtcNow,
                DischargeDateTime = null
            };
            context.Records.Add(record);

            // Vitals (time series for tracker charts)
            var vitalsBaseTime = DateTime.UtcNow.AddDays(-6);
            var vitalsReadings = new (float bp, float hr, float rr, float temp)[]
            {
                (118, 68, 16, 36.4f),
                (120, 70, 17, 36.5f),
                (122, 72, 18, 36.5f),
                (119, 74, 17, 36.6f),
                (121, 71, 18, 36.4f),
                (123, 75, 19, 36.7f),
                (120, 73, 18, 36.5f),
                (118, 69, 16, 36.3f),
                (122, 76, 19, 36.6f),
                (121, 72, 18, 36.5f),
                (119, 70, 17, 36.4f),
                (124, 77, 20, 36.8f),
                (120, 71, 18, 36.5f),
                (118, 68, 16, 36.4f)
            };

            for (var i = 0; i < vitalsReadings.Length; i++)
            {
                var reading = vitalsReadings[i];
                context.Vitals.Add(new Vitals
                {
                    VitalsID = i + 1,
                    PatientID = 1,
                    BloodPressure = reading.bp,
                    HeartRate = reading.hr,
                    RespiratoryRate = reading.rr,
                    Temperature = reading.temp,
                    RecordedAt = vitalsBaseTime.AddHours(i * 12)
                });
            }

            // Relationships
            context.Relationships.AddRange(
                new Relationships { PatientID = 1, UserID = 1 },
                new Relationships { PatientID = 1, UserID = 2 }
            );

            // Logs
            var logBaseTime = DateTime.UtcNow.AddDays(-3);
            context.Logs.AddRange(
                new Log
                {
                    LogID = 1,
                    UserID = 1,
                    Event = "log test 1",
                    Emergency = false,
                    Timestamp = logBaseTime
                },
                new Log
                {
                    LogID = 2,
                    UserID = 2,
                    Event = "log test 2",
                    Emergency = false,
                    Timestamp = logBaseTime.AddHours(4)
                },
                new Log
                {
                    LogID = 3,
                    UserID = 1,
                    Event = "log test 3",
                    Emergency = false,
                    Timestamp = logBaseTime.AddDays(1)
                },
                new Log
                {
                    LogID = 4,
                    UserID = 2,
                    Event = "log test 4",
                    Emergency = true,
                    Timestamp = logBaseTime.AddDays(1).AddHours(6)
                },
                new Log
                {
                    LogID = 5,
                    UserID = 1,
                    Event = "log test 5",
                    Emergency = false,
                    Timestamp = DateTime.UtcNow
                },
                new Log
                {
                    LogID = 6,
                    UserID = 4,
                    Event = "log test 6",
                    Emergency = false,
                    Timestamp = logBaseTime.AddMinutes(30)
                },
                new Log
                {
                    LogID = 7,
                    UserID = 4,
                    Event = "log test 7",
                    Emergency = false,
                    Timestamp = logBaseTime.AddHours(5)
                },
                new Log
                {
                    LogID = 8,
                    UserID = 4,
                    Event = "log test 8",
                    Emergency = false,
                    Timestamp = logBaseTime.AddDays(1).AddHours(2)
                },
                new Log
                {
                    LogID = 9,
                    UserID = 4,
                    Event = "log test 9",
                    Emergency = true,
                    Timestamp = logBaseTime.AddDays(1).AddHours(7)
                }
            );

            context.SaveChanges();
        }
    }
}