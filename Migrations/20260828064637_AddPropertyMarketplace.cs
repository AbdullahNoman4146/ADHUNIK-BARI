using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHUNIK_BARI.Migrations
{
    /// <inheritdoc />
    public partial class AddPropertyMarketplace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PropertyListings",
                columns: table => new
                {
                    PropertyListingId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FlatId = table.Column<int>(type: "int", nullable: false),
                    ListingType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ShortDescription = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    AdvanceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Bedrooms = table.Column<int>(type: "int", nullable: false),
                    Bathrooms = table.Column<int>(type: "int", nullable: false),
                    Balconies = table.Column<int>(type: "int", nullable: false),
                    AreaSqFt = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FurnishingStatus = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Facing = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Features = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CoverImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RoomLayoutImagePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ListingStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyListings", x => x.PropertyListingId);
                    table.ForeignKey(
                        name: "FK_PropertyListings_AspNetUsers_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PropertyListings_Flats_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flats",
                        principalColumn: "FlatId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PropertyApplications",
                columns: table => new
                {
                    PropertyApplicationId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PropertyListingId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentAddress = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Profession = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    NumberOfOccupants = table.Column<int>(type: "int", nullable: true),
                    ExpectedMoveInDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ApplicationType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AdvanceAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StripePaymentIntentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    PaymentStatus = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReservationExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedResidentUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    EmailSent = table.Column<bool>(type: "bit", nullable: false),
                    EmailSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PropertyApplications", x => x.PropertyApplicationId);
                    table.ForeignKey(
                        name: "FK_PropertyApplications_AspNetUsers_CreatedResidentUserId",
                        column: x => x.CreatedResidentUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PropertyApplications_PropertyListings_PropertyListingId",
                        column: x => x.PropertyListingId,
                        principalTable: "PropertyListings",
                        principalColumn: "PropertyListingId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyApplications_CreatedResidentUserId",
                table: "PropertyApplications",
                column: "CreatedResidentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyApplications_PropertyListingId",
                table: "PropertyApplications",
                column: "PropertyListingId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyApplications_Status_PaymentStatus",
                table: "PropertyApplications",
                columns: new[] { "Status", "PaymentStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_PropertyApplications_StripePaymentIntentId",
                table: "PropertyApplications",
                column: "StripePaymentIntentId",
                unique: true,
                filter: "[StripePaymentIntentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyListings_CreatedByUserId",
                table: "PropertyListings",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyListings_FlatId",
                table: "PropertyListings",
                column: "FlatId",
                unique: true,
                filter: "[ListingStatus] <> 'Draft' AND [ListingStatus] <> 'Closed' AND [ListingStatus] <> 'Archived'");

            migrationBuilder.CreateIndex(
                name: "IX_PropertyListings_ListingStatus_ListingType",
                table: "PropertyListings",
                columns: new[] { "ListingStatus", "ListingType" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PropertyApplications");

            migrationBuilder.DropTable(
                name: "PropertyListings");
        }
    }
}
