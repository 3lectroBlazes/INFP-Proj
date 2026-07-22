using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFP_Proj.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
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
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsAdmin = table.Column<bool>(type: "bit", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MiddleName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bracelets",
                columns: table => new
                {
                    BraceletID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Battery = table.Column<float>(type: "real", nullable: true),
                    SystolicBloodPressure = table.Column<float>(type: "real", nullable: true),
                    DiastolicBloodPressure = table.Column<float>(type: "real", nullable: true),
                    HeartRate = table.Column<float>(type: "real", nullable: true),
                    RespiratoryRate = table.Column<float>(type: "real", nullable: true),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Movement = table.Column<float>(type: "real", nullable: true)
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
                    Approval = table.Column<bool>(type: "bit", nullable: false),
                    ConsumptionTime = table.Column<TimeOnly>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medications", x => x.MedicationID);
                });

            migrationBuilder.CreateTable(
                name: "Thresholds",
                columns: table => new
                {
                    ThresholdID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SBPUpperThreshold = table.Column<int>(type: "int", nullable: false),
                    SBPLowerThreshold = table.Column<int>(type: "int", nullable: false),
                    DBPUpperThreshold = table.Column<int>(type: "int", nullable: false),
                    DBPLowerThreshold = table.Column<int>(type: "int", nullable: false),
                    HeartRateUpperPercentageThreshold = table.Column<int>(type: "int", nullable: false),
                    HeartRateLowerPercentageThreshold = table.Column<int>(type: "int", nullable: false),
                    RespiratoryRateUpperPercentageThreshold = table.Column<int>(type: "int", nullable: false),
                    RespiratoryRateLowerThreshold = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Thresholds", x => x.ThresholdID);
                });

            migrationBuilder.CreateTable(
                name: "Wards",
                columns: table => new
                {
                    WardID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WardName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    CurrentOccupancy = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wards", x => x.WardID);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    PatientID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.PatientID);
                    table.ForeignKey(
                        name: "FK_Patients_AspNetUsers_UserID",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AllergyLists",
                columns: table => new
                {
                    AllergyListID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    AllergyID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AllergyLists", x => x.AllergyListID);
                    table.ForeignKey(
                        name: "FK_AllergyLists_Allergies_AllergyID",
                        column: x => x.AllergyID,
                        principalTable: "Allergies",
                        principalColumn: "AllergyID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AllergyLists_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    AppointmentRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Urgency = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoctorResponse = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    PatientAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    AppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.AppointmentRequestID);
                    table.ForeignKey(
                        name: "FK_Appointments_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Cascade);
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
                    Weight = table.Column<float>(type: "real", nullable: false),
                    Location = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Beds", x => x.BedID);
                    table.ForeignKey(
                        name: "FK_Beds_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID");
                    table.ForeignKey(
                        name: "FK_Beds_Wards_WardID",
                        column: x => x.WardID,
                        principalTable: "Wards",
                        principalColumn: "WardID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BraceletRelations",
                columns: table => new
                {
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    BraceletID = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BraceletRelations", x => new { x.PatientID, x.BraceletID });
                    table.ForeignKey(
                        name: "FK_BraceletRelations_Bracelets_BraceletID",
                        column: x => x.BraceletID,
                        principalTable: "Bracelets",
                        principalColumn: "BraceletID");
                    table.ForeignKey(
                        name: "FK_BraceletRelations_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID");
                });

            migrationBuilder.CreateTable(
                name: "DoctorRequests",
                columns: table => new
                {
                    DoctorRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    RequestMessage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReplyMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Completed = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorRequests", x => x.DoctorRequestID);
                    table.ForeignKey(
                        name: "FK_DoctorRequests_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Cascade);
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
                    Approved = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicationLists", x => x.MedicationListID);
                    table.ForeignKey(
                        name: "FK_MedicationLists_Medications_MedicationID",
                        column: x => x.MedicationID,
                        principalTable: "Medications",
                        principalColumn: "MedicationID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MedicationLists_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Cascade);
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
                        name: "FK_Relationships_AspNetUsers_UserID",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
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
                    SystolicBloodPressure = table.Column<float>(type: "real", nullable: true),
                    DiastolicBloodPressure = table.Column<float>(type: "real", nullable: true),
                    HeartRate = table.Column<float>(type: "real", nullable: true),
                    RespiratoryRate = table.Column<float>(type: "real", nullable: true),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vitals", x => x.VitalsID);
                    table.ForeignKey(
                        name: "FK_Vitals_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentChangeRequests",
                columns: table => new
                {
                    AppointmentChangeRequestID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentRequestID = table.Column<int>(type: "int", nullable: false),
                    PatientID = table.Column<int>(type: "int", nullable: false),
                    RequestedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReviewMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ReviewedByUserID = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentChangeRequests", x => x.AppointmentChangeRequestID);
                    table.ForeignKey(
                        name: "FK_AppointmentChangeRequests_Appointments_AppointmentRequestID",
                        column: x => x.AppointmentRequestID,
                        principalTable: "Appointments",
                        principalColumn: "AppointmentRequestID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentChangeRequests_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID");
                });

            migrationBuilder.CreateTable(
                name: "Logs",
                columns: table => new
                {
                    LogID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserID = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PatientID = table.Column<int>(type: "int", nullable: true),
                    Event = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    MedicationListID = table.Column<int>(type: "int", nullable: true),
                    Emergency = table.Column<bool>(type: "bit", nullable: false),
                    Resolved = table.Column<bool>(type: "bit", nullable: false),
                    selfAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    relativeAcknowledged = table.Column<bool>(type: "bit", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Logs", x => x.LogID);
                    table.ForeignKey(
                        name: "FK_Logs_AspNetUsers_UserID",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Logs_MedicationLists_MedicationListID",
                        column: x => x.MedicationListID,
                        principalTable: "MedicationLists",
                        principalColumn: "MedicationListID");
                    table.ForeignKey(
                        name: "FK_Logs_Patients_PatientID",
                        column: x => x.PatientID,
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
                    MedicationListID = table.Column<int>(type: "int", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdmissionDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DischargeDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DischargeReason = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Records", x => x.RecordID);
                    table.ForeignKey(
                        name: "FK_Records_Beds_BedID",
                        column: x => x.BedID,
                        principalTable: "Beds",
                        principalColumn: "BedID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Records_Diagnoses_DiagnosisID",
                        column: x => x.DiagnosisID,
                        principalTable: "Diagnoses",
                        principalColumn: "DiagnosisID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Records_Hospitals_HospitalID",
                        column: x => x.HospitalID,
                        principalTable: "Hospitals",
                        principalColumn: "HospitalID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Records_MedicationLists_MedicationListID",
                        column: x => x.MedicationListID,
                        principalTable: "MedicationLists",
                        principalColumn: "MedicationListID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Records_Patients_PatientID",
                        column: x => x.PatientID,
                        principalTable: "Patients",
                        principalColumn: "PatientID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Records_Wards_WardID",
                        column: x => x.WardID,
                        principalTable: "Wards",
                        principalColumn: "WardID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AllergyLists_AllergyID",
                table: "AllergyLists",
                column: "AllergyID");

            migrationBuilder.CreateIndex(
                name: "IX_AllergyLists_PatientID",
                table: "AllergyLists",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentChangeRequests_PatientID",
                table: "AppointmentChangeRequests",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "UX_AppointmentChangeRequests_OnePending",
                table: "AppointmentChangeRequests",
                column: "AppointmentRequestID",
                unique: true,
                filter: "[Status] = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientID",
                table: "Appointments",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Beds_PatientID",
                table: "Beds",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "IX_Beds_WardID",
                table: "Beds",
                column: "WardID");

            migrationBuilder.CreateIndex(
                name: "IX_BraceletRelations_BraceletID",
                table: "BraceletRelations",
                column: "BraceletID");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorRequests_PatientID",
                table: "DoctorRequests",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_MedicationListID",
                table: "Logs",
                column: "MedicationListID");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_PatientID",
                table: "Logs",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "IX_Logs_UserID",
                table: "Logs",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationLists_MedicationID",
                table: "MedicationLists",
                column: "MedicationID");

            migrationBuilder.CreateIndex(
                name: "IX_MedicationLists_PatientID",
                table: "MedicationLists",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_UserID",
                table: "Patients",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Records_BedID",
                table: "Records",
                column: "BedID");

            migrationBuilder.CreateIndex(
                name: "IX_Records_DiagnosisID",
                table: "Records",
                column: "DiagnosisID");

            migrationBuilder.CreateIndex(
                name: "IX_Records_HospitalID",
                table: "Records",
                column: "HospitalID");

            migrationBuilder.CreateIndex(
                name: "IX_Records_MedicationListID",
                table: "Records",
                column: "MedicationListID");

            migrationBuilder.CreateIndex(
                name: "IX_Records_PatientID",
                table: "Records",
                column: "PatientID");

            migrationBuilder.CreateIndex(
                name: "IX_Records_WardID",
                table: "Records",
                column: "WardID");

            migrationBuilder.CreateIndex(
                name: "IX_Relationships_UserID",
                table: "Relationships",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "IX_Vitals_PatientID",
                table: "Vitals",
                column: "PatientID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AllergyLists");

            migrationBuilder.DropTable(
                name: "AppointmentChangeRequests");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BraceletRelations");

            migrationBuilder.DropTable(
                name: "DoctorRequests");

            migrationBuilder.DropTable(
                name: "Logs");

            migrationBuilder.DropTable(
                name: "Records");

            migrationBuilder.DropTable(
                name: "Relationships");

            migrationBuilder.DropTable(
                name: "Thresholds");

            migrationBuilder.DropTable(
                name: "Vitals");

            migrationBuilder.DropTable(
                name: "Allergies");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Bracelets");

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
                name: "AspNetUsers");
        }
    }
}
