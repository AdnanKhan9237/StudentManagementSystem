using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddEnrollmentIdToAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EnrollmentId",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_EnrollmentId",
                table: "Attendances",
                column: "EnrollmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Enrollments_EnrollmentId",
                table: "Attendances",
                column: "EnrollmentId",
                principalTable: "Enrollments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Enrollments_EnrollmentId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_EnrollmentId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "EnrollmentId",
                table: "Attendances");
        }
    }
}
