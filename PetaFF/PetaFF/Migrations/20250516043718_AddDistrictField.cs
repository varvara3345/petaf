using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetaFF.Migrations
{
    /// <inheritdoc />
    public partial class AddDistrictField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "PetAds",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "District",
                table: "PetAds");
        }
    }
}
