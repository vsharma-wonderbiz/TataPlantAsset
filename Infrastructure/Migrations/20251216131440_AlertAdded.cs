using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlertAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    AlertId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SignalTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MappingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AlertStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AlertEndUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MinThreshold = table.Column<double>(type: "float", nullable: false),
                    MaxThreshold = table.Column<double>(type: "float", nullable: false),
                    MinObservedValue = table.Column<double>(type: "float", nullable: true),
                    MaxObservedValue = table.Column<double>(type: "float", nullable: true),
                    ReminderTimeHours = table.Column<int>(type: "int", nullable: false, defaultValue: 24),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsAnalyzed = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RecommendedActions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.AlertId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Asset_Time",
                table: "Alerts",
                columns: new[] { "AssetId", "AlertStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_Mapping_Active",
                table: "Alerts",
                columns: new[] { "MappingId", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");
        }
    }
}
