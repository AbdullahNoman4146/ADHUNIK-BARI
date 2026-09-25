using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHUNIK_BARI.Migrations
{
    /// <inheritdoc />
    public partial class AddParkingApplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'[ParkingSpots]') AND name = 'AssignedUserId')
BEGIN
    ALTER TABLE [ParkingSpots] ADD [AssignedUserId] nvarchar(450) NULL;
END;
");

            migrationBuilder.CreateTable(
                name: "ParkingApplications",
                columns: table => new
                {
                    ParkingApplicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParkingSpotId = table.Column<int>(type: "int", nullable: false),
                    FlatId = table.Column<int>(type: "int", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    VehicleType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    VehicleRegNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ApplicationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AdvanceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReservationExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    IsOutsider = table.Column<bool>(type: "bit", nullable: false),
                    EmailSent = table.Column<bool>(type: "bit", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParkingApplications", x => x.ParkingApplicationId);
                    table.ForeignKey(
                        name: "FK_ParkingApplications_AspNetUsers_CreatedUserId",
                        column: x => x.CreatedUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ParkingApplications_Flats_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flats",
                        principalColumn: "FlatId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ParkingApplications_ParkingSpots_ParkingSpotId",
                        column: x => x.ParkingSpotId,
                        principalTable: "ParkingSpots",
                        principalColumn: "ParkingSpotId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ParkingSpots_AssignedUserId' AND object_id = OBJECT_ID(N'[ParkingSpots]'))
BEGIN
    CREATE INDEX [IX_ParkingSpots_AssignedUserId] ON [ParkingSpots] ([AssignedUserId]);
END;
");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingApplications_CreatedUserId",
                table: "ParkingApplications",
                column: "CreatedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingApplications_FlatId",
                table: "ParkingApplications",
                column: "FlatId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingApplications_ParkingSpotId",
                table: "ParkingApplications",
                column: "ParkingSpotId");

            migrationBuilder.CreateIndex(
                name: "IX_ParkingApplications_Status_PaymentStatus",
                table: "ParkingApplications",
                columns: new[] { "Status", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_ParkingApplications_StripePaymentIntentId",
                table: "ParkingApplications",
                column: "StripePaymentIntentId",
                unique: true,
                filter: "[StripePaymentIntentId] IS NOT NULL");

            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ParkingSpots_AspNetUsers_AssignedUserId')
BEGIN
    ALTER TABLE [ParkingSpots] ADD CONSTRAINT [FK_ParkingSpots_AspNetUsers_AssignedUserId] 
    FOREIGN KEY ([AssignedUserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE NO ACTION;
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ParkingSpots_AspNetUsers_AssignedUserId",
                table: "ParkingSpots");

            migrationBuilder.DropTable(
                name: "ParkingApplications");

            migrationBuilder.DropIndex(
                name: "IX_ParkingSpots_AssignedUserId",
                table: "ParkingSpots");

            migrationBuilder.DropColumn(
                name: "AssignedUserId",
                table: "ParkingSpots");
        }
    }
}
