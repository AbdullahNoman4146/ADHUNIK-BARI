using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHUNIK_BARI.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ParkingSpots",
                columns: table => new
                {
                    ParkingSpotId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpotNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ParkingFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ParkingType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsAvailable = table.Column<bool>(type: "bit", nullable: false),
                    FlatId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingSpots", x => x.ParkingSpotId);
                    table.ForeignKey(
                        name: "FK_ParkingSpots_Flats_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flats",
                        principalColumn: "FlatId");
                });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSpots_FlatId",
                table: "ParkingSpots",
                column: "FlatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ParkingSpots");
        }
    }
}
