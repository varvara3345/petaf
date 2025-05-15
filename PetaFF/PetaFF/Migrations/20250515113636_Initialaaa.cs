using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PetaFF.Migrations
{
    /// <inheritdoc />
    public partial class Initialaaa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PhotoPath",
                table: "PetAds",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateLost",
                table: "PetAds",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastSeenAddress",
                table: "PetAds",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateLost",
                table: "PetAds");

            migrationBuilder.DropColumn(
                name: "LastSeenAddress",
                table: "PetAds");

            migrationBuilder.AlterColumn<string>(
                name: "PhotoPath",
                table: "PetAds",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);
        }
    }
}
