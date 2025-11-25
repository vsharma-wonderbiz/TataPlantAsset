using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class init2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SignalData",
                columns: table => new
                {
                    SignalDataId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignalTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DevicePortId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignalName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SignalUnit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RegisterAddress = table.Column<int>(type: "int", nullable: true),
                    BucketStartUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Count = table.Column<int>(type: "int", nullable: false),
                    Sum = table.Column<double>(type: "float", nullable: false),
                    MinValue = table.Column<double>(type: "float", nullable: true),
                    MaxValue = table.Column<double>(type: "float", nullable: true),
                    AvgValue = table.Column<double>(type: "float", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignalData", x => x.SignalDataId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Mapping_Asset_Signal",
                table: "MappingTable",
                columns: new[] { "AssetId", "SignalTypeId" });

            migrationBuilder.CreateIndex(
                name: "IX_Mapping_Device_Port",
                table: "MappingTable",
                columns: new[] { "DeviceId", "DevicePortId" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalData_Asset_Bucket",
                table: "SignalData",
                columns: new[] { "AssetId", "BucketStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalData_Device_Bucket",
                table: "SignalData",
                columns: new[] { "DeviceId", "DevicePortId", "BucketStartUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SignalData_SignalType_Bucket",
                table: "SignalData",
                columns: new[] { "SignalTypeId", "BucketStartUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_SignalData_BucketKey",
                table: "SignalData",
                columns: new[] { "AssetId", "SignalTypeId", "DeviceId", "DevicePortId", "BucketStartUtc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignalData");

            migrationBuilder.DropIndex(
                name: "IX_Mapping_Asset_Signal",
                table: "MappingTable");

            migrationBuilder.DropIndex(
                name: "IX_Mapping_Device_Port",
                table: "MappingTable");
        }
    }
}
