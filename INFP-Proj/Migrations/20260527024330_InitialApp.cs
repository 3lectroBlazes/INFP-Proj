using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFP_Proj.Migrations
{
    /// <inheritdoc />
    public partial class InitialApp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Allergies",
                columns: table => new
                {
                    AllergyID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Allergy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Allergies", x => x.AllergyID);
                });

            migrationBuilder.CreateTable(
                name: "AppUser",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bracelets",
                columns: table => new
                {
                    BraceletID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    Battery = table.Column<float>(type: "real", nullable: true),
                    Respiration = table.Column<float>(type: "real", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Movement = table.Column<float>(type: "real", nullable: true),
                    BloodPressure = table.Column<float>(type: "real", nullable: true),
                    HeartRate = table.Column<float>(type: "real", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bracelets", x => x.BraceletID);
                });

            migrationBuilder.CreateTable(
                name: "Diagnoses",
                columns: table => new
                {
                    DiagnosisID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DiagnosisName = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnoses", x => x.DiagnosisID);
                });

            migrationBuilder.CreateTable(
                name: "Hospitals",
                columns: table => new
                {
                    HospitalID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HospitalName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    HospitalAddress = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Hospitals", x => x.HospitalID);
                });

            migrationBuilder.CreateTable(
                name: "Medications",
                columns: table => new
                {
                    MedicationID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MedicationName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ConsumptionTime = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.MedicationID);
                });

            migrationBuilder.CreateTable(
                name: "Wards",
                columns: table => new
                {
                    WardID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WardName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wards", x => x.WardID);
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Event = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Emergency = table.Column<bool>(type: "bit", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_Logs_AppUser_UserID",
                        column: x => x.UserID,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BraceletID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientID);
                    table.ForeignKey(
                        name: "FK_Patients_AppUser_UserID",
                        column: x => x.UserID,
                        principalTable: "AppUser",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Patients_Bracelets_BraceletID",
                        column: x => x.BraceletID,
                        principalTable: "Bracelets",
                        principalColumn: "BraceletID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AllergyLists",
                columns: table => new
                {
                    AllergyListID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    AllergyID = table.Column<int>(type: "int", nullable: false),
                    PatientsPatientID = table.Column<int>(type: "int", nullable: true),
                    AllergiesAllergyID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllergyLists", x => x.AllergyListID);
                    table.ForeignKey(
                        name: "FK_AllergyLists_Allergies_AllergiesAllergyID",
                        column: x => x.AllergiesAllergyID,
                        principalTable: "Allergies",
                        principalColumn: "AllergyID");
                    table.ForeignKey(
                        name: "FK_AllergyLists_Patients_PatientsPatientID",
                        column: x => x.PatientsPatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID");
                });

            migrationBuilder.CreateTable(
                name: "Beds",
                columns: table => new
                {
                    BedID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: true),
                    WardID = table.Column<int>(type: "int", nullable: false),
                    Sector = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Floor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Room = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Temperature = table.Column<float>(type: "real", nullable: false),
                    Weight = table.Column<float>(type: "real", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientsPatientID = table.Column<int>(type: "int", nullable: true),
                    WardsWardID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beds", x => x.BedID);
                    table.ForeignKey(
                        name: "FK_Beds_Patients_PatientsPatientID",
                        column: x => x.PatientsPatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID");
                    table.ForeignKey(
                        name: "FK_Beds_Wards_WardsWardID",
                        column: x => x.WardsWardID,
                        principalTable: "Wards",
                        principalColumn: "WardID");
                });

            migrationBuilder.CreateTable(
                name: "MedicationLists",
                columns: table => new
                {
                    MedicationListID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    MedicationID = table.Column<int>(type: "int", nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatientsPatientID = table.Column<int>(type: "int", nullable: true),
                    MedicationsMedicationID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationLists", x => x.MedicationListID);
                    table.ForeignKey(
                        name: "FK_MedicationLists_Medications_MedicationsMedicationID",
                        column: x => x.MedicationsMedicationID,
                        principalTable: "Medications",
                        principalColumn: "MedicationID");
                    table.ForeignKey(
                        name: "FK_MedicationLists_Patients_PatientsPatientID",
                        column: x => x.PatientsPatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID");
                });

            migrationBuilder.CreateTable(
                name: "Relationships",
                columns: table => new
                {
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Relationships", x => new { x.PatientID, x.UserID });
                    table.ForeignKey(
                        name: "FK_Relationships_AppUser_UserID",
                        column: x => x.UserID,
                        principalTable: "AppUser",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Relationships_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID");
                });

            migrationBuilder.CreateTable(
                name: "Vitals",
                columns: table => new
                {
                    VitalsID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    BloodPressure = table.Column<float>(type: "real", nullable: true),
                    HeartRate = table.Column<float>(type: "real", nullable: true),
                    RespiratoryRate = table.Column<float>(type: "real", nullable: true),
                    Temperature = table.Column<float>(type: "real", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PatientsPatientID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vitals", x => x.VitalsID);
                    table.ForeignKey(
                        name: "FK_Vitals_Patients_PatientsPatientID",
                        column: x => x.PatientsPatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID");
                });

            migrationBuilder.CreateTable(
                name: "Records",
                columns: table => new
                {
                    RecordID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    BedID = table.Column<int>(type: "int", nullable: false),
                    WardID = table.Column<int>(type: "int", nullable: false),
                    HospitalID = table.Column<int>(type: "int", nullable: false),
                    DiagnosisID = table.Column<int>(type: "int", nullable: false),
                    MedicationListID = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdmissionDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DischargeDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PatientsPatientID = table.Column<int>(type: "int", nullable: true),
                    BedsBedID = table.Column<int>(type: "int", nullable: true),
                    WardsWardID = table.Column<int>(type: "int", nullable: true),
                    HospitalsHospitalID = table.Column<int>(type: "int", nullable: true),
                    DiagnosesDiagnosisID = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Records", x => x.RecordID);
                    table.ForeignKey(
                        name: "FK_Records_Beds_BedsBedID",
                        column: x => x.BedsBedID,
                        principalTable: "Beds",
                        principalColumn: "BedID");
                    table.ForeignKey(
                        name: "FK_Records_Diagnoses_DiagnosesDiagnosisID",
                        column: x => x.DiagnosesDiagnosisID,
                        principalTable: "Diagnoses",
                        principalColumn: "DiagnosisID");
                    table.ForeignKey(
                        name: "FK_Records_Hospitals_HospitalsHospitalID",
                        column: x => x.HospitalsHospitalID,
                        principalTable: "Hospitals",
                        principalColumn: "HospitalID");
                    table.ForeignKey(
                        name: "FK_Records_MedicationLists_MedicationListID",
                        column: x => x.MedicationListID,
                        principalTable: "MedicationLists",
                        principalColumn: "MedicationListID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Records_Patients_PatientsPatientID",
                        column: x => x.PatientsPatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID");
                    table.ForeignKey(
                        name: "FK_Records_Wards_WardsWardID",
                        column: x => x.WardsWardID,
                        principalTable: "Wards",
                        principalColumn: "WardID");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllergyLists_AllergiesAllergyID",
                table: "AllergyLists",
                column: "AllergiesAllergyID");

            migrationBuilder.CreateIndex(
                name: "IX_AllergyLists_PatientsPatientID",
                table: "AllergyLists",
                column: "PatientsPatientID");

            migrationBuilder.CreateIndex(
                name: "IX_Beds_PatientsPatientID",
                table: "Beds",
                column: "PatientsPatientID");

            migrationBuilder.CreateIndex(
                name: "IX_Beds_WardsWardID",
                table: "Beds",
                column: "WardsWardID");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_UserID",
                table: "Logs",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationLists_MedicationsMedicationID",
                table: "MedicationLists",
                column: "MedicationsMedicationID");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationLists_PatientsPatientID",
                table: "MedicationLists",
                column: "PatientsPatientID");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_BraceletID",
                table: "Patients",
                column: "BraceletID",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserID",
                table: "Patients",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Records_BedsBedID",
                table: "Records",
                column: "BedsBedID");

            migrationBuilder.CreateIndex(
                name: "IX_Records_DiagnosesDiagnosisID",
                table: "Records",
                column: "DiagnosesDiagnosisID");

            migrationBuilder.CreateIndex(
                name: "IX_Records_HospitalsHospitalID",
                table: "Records",
                column: "HospitalsHospitalID");

            migrationBuilder.CreateIndex(
                name: "IX_Records_MedicationListID",
                table: "Records",
                column: "MedicationListID");

            migrationBuilder.CreateIndex(
                name: "IX_Records_PatientsPatientID",
                table: "Records",
                column: "PatientsPatientID");

            migrationBuilder.CreateIndex(
                name: "IX_Records_WardsWardID",
                table: "Records",
                column: "WardsWardID");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_UserID",
                table: "Relationships",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Vitals_PatientsPatientID",
                table: "Vitals",
                column: "PatientsPatientID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllergyLists");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "Records");

            migrationBuilder.DropTable(
                name: "Relationships");

            migrationBuilder.DropTable(
                name: "Vitals");

            migrationBuilder.DropTable(
                name: "Allergies");

            migrationBuilder.DropTable(
                name: "Beds");

            migrationBuilder.DropTable(
                name: "Diagnoses");

            migrationBuilder.DropTable(
                name: "Hospitals");

            migrationBuilder.DropTable(
                name: "MedicationLists");

            migrationBuilder.DropTable(
                name: "Wards");

            migrationBuilder.DropTable(
                name: "Medications");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "AppUser");

            migrationBuilder.DropTable(
                name: "Bracelets");
        }
    }
}
