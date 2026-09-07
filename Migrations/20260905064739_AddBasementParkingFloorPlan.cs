using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHUNIK_BARI.Migrations
{
    /// <inheritdoc />
    public partial class AddBasementParkingFloorPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "SpotNumber",
                table: "ParkingSpots",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ParkingType",
                table: "ParkingSpots",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ListingNotes",
                table: "ParkingSpots",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ListingPrice",
                table: "ParkingSpots",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ParkingFloorId",
                table: "ParkingSpots",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ParkingSpots",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "ParkingActivityLogs",
                columns: table => new
                {
                    ActivityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParkingSpotId = table.Column<int>(type: "int", nullable: true),
                    Action = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Details = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingActivityLogs", x => x.ActivityId);
                    table.ForeignKey(
                        name: "FK_ParkingActivityLogs_ParkingSpots_ParkingSpotId",
                        column: x => x.ParkingSpotId,
                        principalTable: "ParkingSpots",
                        principalColumn: "ParkingSpotId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ParkingFloors",
                columns: table => new
                {
                    ParkingFloorId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FloorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FloorCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Capacity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingFloors", x => x.ParkingFloorId);
                });

            migrationBuilder.Sql(@"
UPDATE [ParkingSpots]
SET [Status] = CASE WHEN [FlatId] IS NOT NULL THEN 'Assigned' ELSE 'Available' END
WHERE [Status] = '' OR [Status] IS NULL;

IF EXISTS (SELECT 1 FROM [ParkingSpots] WHERE [ParkingFloorId] IS NULL)
BEGIN
    IF NOT EXISTS (SELECT 1 FROM [ParkingFloors] WHERE [FloorCode] = 'B1')
    BEGIN
        INSERT INTO [ParkingFloors] ([FloorName], [FloorCode], [Capacity], [CreatedAt])
        VALUES ('Basement 1', 'B1', 20, SYSUTCDATETIME());
    END
    DECLARE @DefaultFloorId INT = (SELECT TOP 1 [ParkingFloorId] FROM [ParkingFloors] WHERE [FloorCode] = 'B1');
    UPDATE [ParkingSpots] SET [ParkingFloorId] = @DefaultFloorId WHERE [ParkingFloorId] IS NULL;
END
");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSpots_ParkingFloorId",
                table: "ParkingSpots",
                column: "ParkingFloorId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingSpots_SpotNumber",
                table: "ParkingSpots",
                column: "SpotNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingActivityLogs_ParkingSpotId",
                table: "ParkingActivityLogs",
                column: "ParkingSpotId");

            migrationBuilder.AddForeignKey(
                name: "FK_ParkingSpots_ParkingFloors_ParkingFloorId",
                table: "ParkingSpots",
                column: "ParkingFloorId",
                principalTable: "ParkingFloors",
                principalColumn: "ParkingFloorId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSpots_ParkingFloors_ParkingFloorId",
                table: "ParkingSpots");

            migrationBuilder.DropTable(
                name: "ParkingActivityLogs");

            migrationBuilder.DropTable(
                name: "ParkingFloors");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSpots_ParkingFloorId",
                table: "ParkingSpots");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSpots_SpotNumber",
                table: "ParkingSpots");

            migrationBuilder.DropColumn(
                name: "ListingNotes",
                table: "ParkingSpots");

            migrationBuilder.DropColumn(
                name: "ListingPrice",
                table: "ParkingSpots");

            migrationBuilder.DropColumn(
                name: "ParkingFloorId",
                table: "ParkingSpots");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ParkingSpots");

            migrationBuilder.AlterColumn<string>(
                name: "SpotNumber",
                table: "ParkingSpots",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "ParkingType",
                table: "ParkingSpots",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }
    }
}
