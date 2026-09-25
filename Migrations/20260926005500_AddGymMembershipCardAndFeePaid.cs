using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHUNIK_BARI.Migrations
{
    /// <inheritdoc />
    public partial class AddGymMembershipCardAndFeePaid : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CardNumber",
                table: "GymMemberships",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FeePaidAt",
                table: "GymMemberships",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsFeePaid",
                table: "GymMemberships",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CardNumber",
                table: "GymMemberships");

            migrationBuilder.DropColumn(
                name: "FeePaidAt",
                table: "GymMemberships");

            migrationBuilder.DropColumn(
                name: "IsFeePaid",
                table: "GymMemberships");
        }
    }
}
