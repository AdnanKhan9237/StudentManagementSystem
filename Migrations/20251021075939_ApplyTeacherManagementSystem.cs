using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class ApplyTeacherManagementSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Batches_Teachers_PrimaryInstructorId",
                table: "Batches");

            migrationBuilder.DropForeignKey(
                name: "FK_Batches_Teachers_SecondaryInstructorId",
                table: "Batches");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_Teachers_PrimaryInstructorId",
                table: "Batches",
                column: "PrimaryInstructorId",
                principalTable: "Teachers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_Teachers_SecondaryInstructorId",
                table: "Batches",
                column: "SecondaryInstructorId",
                principalTable: "Teachers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Batches_Teachers_PrimaryInstructorId",
                table: "Batches");

            migrationBuilder.DropForeignKey(
                name: "FK_Batches_Teachers_SecondaryInstructorId",
                table: "Batches");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_Teachers_PrimaryInstructorId",
                table: "Batches",
                column: "PrimaryInstructorId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_Teachers_SecondaryInstructorId",
                table: "Batches",
                column: "SecondaryInstructorId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
