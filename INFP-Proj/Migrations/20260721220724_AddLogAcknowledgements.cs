using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFP_Proj.Migrations
{
    /// <inheritdoc />
    public partial class AddLogAcknowledgements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LogAcknowledgements",
                columns: table => new
                {
                    LogAcknowledgementID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LogID = table.Column<int>(type: "int", nullable: false),
                    UserID = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AcknowledgedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LogAcknowledgements", x => x.LogAcknowledgementID);
                    table.ForeignKey(
                        name: "FK_LogAcknowledgements_AspNetUsers_UserID",
                        column: x => x.UserID,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_LogAcknowledgements_Logs_LogID",
                        column: x => x.LogID,
                        principalTable: "Logs",
                        principalColumn: "LogID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LogAcknowledgements_UserID",
                table: "LogAcknowledgements",
                column: "UserID");

            migrationBuilder.CreateIndex(
                name: "UX_LogAcknowledgements_LogID_UserID",
                table: "LogAcknowledgements",
                columns: new[] { "LogID", "UserID" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LogAcknowledgements");
        }
    }
}
