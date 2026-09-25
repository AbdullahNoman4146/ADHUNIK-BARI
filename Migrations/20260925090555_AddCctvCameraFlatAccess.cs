using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHUNIK_BARI.Migrations
{
    /// <inheritdoc />
    public partial class AddCctvCameraFlatAccess : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessType",
                table: "CctvCameras",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "All");

            migrationBuilder.CreateTable(
                name: "CctvCameraFlatAccesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CameraId = table.Column<int>(type: "int", nullable: false),
                    FlatId = table.Column<int>(type: "int", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CctvCameraFlatAccesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CctvCameraFlatAccesses_CctvCameras_CameraId",
                        column: x => x.CameraId,
                        principalTable: "CctvCameras",
                        principalColumn: "CameraId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CctvCameraFlatAccesses_Flats_FlatId",
                        column: x => x.FlatId,
                        principalTable: "Flats",
                        principalColumn: "FlatId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CctvCameraFlatAccesses_CameraId_FlatId",
                table: "CctvCameraFlatAccesses",
                columns: new[] { "CameraId", "FlatId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CctvCameraFlatAccesses_FlatId",
                table: "CctvCameraFlatAccesses",
                column: "FlatId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CctvCameraFlatAccesses");

            migrationBuilder.DropColumn(
                name: "AccessType",
                table: "CctvCameras");
        }
    }
}
