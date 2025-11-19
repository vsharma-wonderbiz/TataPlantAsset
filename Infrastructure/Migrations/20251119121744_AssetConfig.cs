using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssetConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssetConfigurations",
                columns: table => new
                {
                    AssetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SignaTypeID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssetConfigId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetConfigurations", x => new { x.AssetId, x.SignaTypeID });
                    table.ForeignKey(
                        name: "FK_AssetConfigurations_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "AssetId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AssetConfigurations_SignalTypes_SignaTypeID",
                        column: x => x.SignaTypeID,
                        principalTable: "SignalTypes",
                        principalColumn: "SignalTypeID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssetConfigurations_AssetId_SignaTypeID",
                table: "AssetConfigurations",
                columns: new[] { "AssetId", "SignaTypeID" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssetConfigurations_SignaTypeID",
                table: "AssetConfigurations",
                column: "SignaTypeID");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetConfigurations");
        }
    }
}
