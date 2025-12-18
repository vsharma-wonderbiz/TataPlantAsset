using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AlertAdded10 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AlertAnalyses_Alerts_AlertId",
                table: "AlertAnalyses");

            migrationBuilder.DropIndex(
                name: "IX_AlertAnalyses_AlertId",
                table: "AlertAnalyses");

            migrationBuilder.DropColumn(
                name: "AlertId",
                table: "AlertAnalyses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AlertId",
                table: "AlertAnalyses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_AlertAnalyses_AlertId",
                table: "AlertAnalyses",
                column: "AlertId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AlertAnalyses_Alerts_AlertId",
                table: "AlertAnalyses",
                column: "AlertId",
                principalTable: "Alerts",
                principalColumn: "AlertId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
