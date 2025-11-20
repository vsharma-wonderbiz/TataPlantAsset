using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AssetconfigprimaryKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AssetConfigurations",
                table: "AssetConfigurations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssetConfigurations",
                table: "AssetConfigurations",
                column: "AssetConfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AssetConfigurations",
                table: "AssetConfigurations");

            migrationBuilder.AddPrimaryKey(
                name: "PK_AssetConfigurations",
                table: "AssetConfigurations",
                columns: new[] { "AssetId", "SignaTypeID" });
        }
    }
}
