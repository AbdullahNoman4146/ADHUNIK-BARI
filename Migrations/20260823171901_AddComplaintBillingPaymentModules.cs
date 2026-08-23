using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ADHUNIK_BARI.Migrations
{
    /// <inheritdoc />
    public partial class AddComplaintBillingPaymentModules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_Flats_FlatId",
                table: "Bills");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Flats_FlatId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Payments_FlatId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_Bills_FlatId",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "FlatId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "Bills");

            migrationBuilder.RenameColumn(
                name: "FlatId",
                table: "Bills",
                newName: "BillYear");

            migrationBuilder.RenameColumn(
                name: "DueDate",
                table: "Bills",
                newName: "Deadline");

            migrationBuilder.RenameColumn(
                name: "Amount",
                table: "Bills",
                newName: "TotalAmount");

            migrationBuilder.AddColumn<int>(
                name: "AssignmentId",
                table: "Bills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "BillMonth",
                table: "Bills",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "DueAmount",
                table: "Bills",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "PaidAmount",
                table: "Bills",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateIndex(
                name: "IX_Bills_AssignmentId",
                table: "Bills",
                column: "AssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_FlatAssignments_AssignmentId",
                table: "Bills",
                column: "AssignmentId",
                principalTable: "FlatAssignments",
                principalColumn: "AssignmentId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bills_FlatAssignments_AssignmentId",
                table: "Bills");

            migrationBuilder.DropIndex(
                name: "IX_Bills_AssignmentId",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "AssignmentId",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "BillMonth",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "DueAmount",
                table: "Bills");

            migrationBuilder.DropColumn(
                name: "PaidAmount",
                table: "Bills");

            migrationBuilder.RenameColumn(
                name: "TotalAmount",
                table: "Bills",
                newName: "Amount");

            migrationBuilder.RenameColumn(
                name: "Deadline",
                table: "Bills",
                newName: "DueDate");

            migrationBuilder.RenameColumn(
                name: "BillYear",
                table: "Bills",
                newName: "FlatId");

            migrationBuilder.AddColumn<int>(
                name: "FlatId",
                table: "Payments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "Bills",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Payments_FlatId",
                table: "Payments",
                column: "FlatId");

            migrationBuilder.CreateIndex(
                name: "IX_Bills_FlatId",
                table: "Bills",
                column: "FlatId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bills_Flats_FlatId",
                table: "Bills",
                column: "FlatId",
                principalTable: "Flats",
                principalColumn: "FlatId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Payments_Flats_FlatId",
                table: "Payments",
                column: "FlatId",
                principalTable: "Flats",
                principalColumn: "FlatId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
