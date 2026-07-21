using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFP_Proj.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentChangeRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentChangeRequests");
        }
    }
}
