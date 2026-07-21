using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace INFP_Proj.Migrations
{
    /// <inheritdoc />
    public partial class Thresholds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "SBPUpperThreshold",
                table: "Thresholds",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SBPLowerThreshold",
                table: "Thresholds",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RespiratoryRateUpperPercentageThreshold",
                table: "Thresholds",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "RespiratoryRateLowerThreshold",
                table: "Thresholds",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HeartRateUpperPercentageThreshold",
                table: "Thresholds",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "HeartRateLowerPercentageThreshold",
                table: "Thresholds",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DBPUpperThreshold",
                table: "Thresholds",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DBPLowerThreshold",
                table: "Thresholds",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(float),
                oldType: "real",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "SBPUpperThreshold",
                table: "Thresholds",
                type: "real",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<float>(
                name: "SBPLowerThreshold",
                table: "Thresholds",
                type: "real",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<float>(
                name: "RespiratoryRateUpperPercentageThreshold",
                table: "Thresholds",
                type: "real",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<float>(
                name: "RespiratoryRateLowerThreshold",
                table: "Thresholds",
                type: "real",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<float>(
                name: "HeartRateUpperPercentageThreshold",
                table: "Thresholds",
                type: "real",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<float>(
                name: "HeartRateLowerPercentageThreshold",
                table: "Thresholds",
                type: "real",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<float>(
                name: "DBPUpperThreshold",
                table: "Thresholds",
                type: "real",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<float>(
                name: "DBPLowerThreshold",
                table: "Thresholds",
                type: "real",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }
    }
}
