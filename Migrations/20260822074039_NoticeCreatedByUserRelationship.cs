using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHUNIK_BARI.Migrations
{
    /// <inheritdoc />
    public partial class NoticeCreatedByUserRelationship : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notices_AspNetUsers_CreatedById",
                table: "Notices");

            migrationBuilder.DropIndex(
                name: "IX_Notices_CreatedById",
                table: "Notices");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Notices");

            migrationBuilder.AlterColumn<string>(
                name: "NoticeType",
                table: "Notices",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "Notices",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Notices_CreatedByUserId",
                table: "Notices",
                column: "CreatedByUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_AspNetUsers_CreatedByUserId",
                table: "Notices",
                column: "CreatedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notices_AspNetUsers_CreatedByUserId",
                table: "Notices");

            migrationBuilder.DropIndex(
                name: "IX_Notices_CreatedByUserId",
                table: "Notices");

            migrationBuilder.AlterColumn<string>(
                name: "NoticeType",
                table: "Notices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedByUserId",
                table: "Notices",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Notices",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notices_CreatedById",
                table: "Notices",
                column: "CreatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_Notices_AspNetUsers_CreatedById",
                table: "Notices",
                column: "CreatedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }
    }
}
