using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlertAdded3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Alerts_Asset_Time",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_Mapping_Active",
                table: "Alerts");

            migrationBuilder.DropColumn(
                name: "RecommendedActions",
                table: "Alerts");

            migrationBuilder.AlterColumn<int>(
                name: "ReminderTimeHours",
                table: "Alerts",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 24);

            migrationBuilder.AlterColumn<bool>(
                name: "IsAnalyzed",
                table: "Alerts",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Alerts",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "bit",
                oldDefaultValue: false);

            migrationBuilder.CreateTable(
                name: "AlertAnalyses",
                columns: table => new
                {
                    AlertAnalysisId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FromUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ToUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RecommendedActions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AnalyzedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AlertAnalyses", x => x.AlertAnalysisId);
                    table.ForeignKey(
                        name: "FK_AlertAnalyses_Alerts_AlertId",
                        column: x => x.AlertId,
                        principalTable: "Alerts",
                        principalColumn: "AlertId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_AssetId_IsAnalyzed",
                table: "Alerts",
                columns: new[] { "AssetId", "IsAnalyzed" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_MappingId",
                table: "Alerts",
                column: "MappingId");

            migrationBuilder.CreateIndex(
                name: "IX_AlertAnalyses_AlertId",
                table: "AlertAnalyses",
                column: "AlertId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AlertAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_AssetId_IsAnalyzed",
                table: "Alerts");

            migrationBuilder.DropIndex(
                name: "IX_Alerts_MappingId",
                table: "Alerts");

            migrationBuilder.AlterColumn<int>(
                name: "ReminderTimeHours",
                table: "Alerts",
                type: "int",
                nullable: false,
                defaultValue: 24,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<bool>(
                name: "IsAnalyzed",
                table: "Alerts",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Alerts",
                type: "bit",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<string>(
                name: "RecommendedActions",
                table: "Alerts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Asset_Time",
                table: "Alerts",
                columns: new[] { "AssetId", "AlertStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Mapping_Active",
                table: "Alerts",
                columns: new[] { "MappingId", "IsActive" });
        }
    }
}
