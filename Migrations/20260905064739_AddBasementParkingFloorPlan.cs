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

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ParkingSpots]') AND name = 'ListingNotes')
BEGIN
    ALTER TABLE [ParkingSpots] ADD [ListingNotes] nvarchar(500) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ParkingSpots]') AND name = 'ListingPrice')
BEGIN
    ALTER TABLE [ParkingSpots] ADD [ListingPrice] decimal(18,2) NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ParkingSpots]') AND name = 'ParkingFloorId')
BEGIN
    ALTER TABLE [ParkingSpots] ADD [ParkingFloorId] int NULL;
END;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ParkingSpots]') AND name = 'Status')
BEGIN
    ALTER TABLE [ParkingSpots] ADD [Status] nvarchar(50) NOT NULL DEFAULT N'';
END;
");

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

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ParkingSpots_ParkingFloorId' AND object_id = OBJECT_ID(N'[ParkingSpots]'))
BEGIN
    CREATE INDEX [IX_ParkingSpots_ParkingFloorId] ON [ParkingSpots] ([ParkingFloorId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ParkingSpots_SpotNumber' AND object_id = OBJECT_ID(N'[ParkingSpots]'))
BEGIN
    CREATE INDEX [IX_ParkingSpots_SpotNumber] ON [ParkingSpots] ([SpotNumber]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ParkingActivityLogs_ParkingSpotId' AND object_id = OBJECT_ID(N'[ParkingActivityLogs]'))
BEGIN
    CREATE INDEX [IX_ParkingActivityLogs_ParkingSpotId] ON [ParkingActivityLogs] ([ParkingSpotId]);
END;

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ParkingSpots_ParkingFloors_ParkingFloorId')
BEGIN
    ALTER TABLE [ParkingSpots] ADD CONSTRAINT [FK_ParkingSpots_ParkingFloors_ParkingFloorId] 
    FOREIGN KEY ([ParkingFloorId]) REFERENCES [ParkingFloors] ([ParkingFloorId]) ON DELETE SET NULL;
END;
");
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
