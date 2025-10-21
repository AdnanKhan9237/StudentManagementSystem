using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTeacherManagementSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Batches_AspNetUsers_InstructorId",
                table: "Batches");

            migrationBuilder.RenameColumn(
                name: "InstructorId",
                table: "Batches",
                newName: "ApplicationUserId");

            migrationBuilder.RenameIndex(
                name: "IX_Batches_InstructorId",
                table: "Batches",
                newName: "IX_Batches_ApplicationUserId");

            migrationBuilder.AddColumn<int>(
                name: "PrimaryInstructorId",
                table: "Batches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecondaryInstructorId",
                table: "Batches",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeacherId",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Teachers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TeacherCode = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FatherName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CNIC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DateOfBirth = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PhoneNumber = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Address = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Country = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PostalCode = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    PhotoPath = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    HireDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Qualification = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Specialization = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Salary = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teachers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teachers_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Batches_PrimaryInstructorId",
                table: "Batches",
                column: "PrimaryInstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Batches_SecondaryInstructorId",
                table: "Batches",
                column: "SecondaryInstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_TeacherId",
                table: "Attendances",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_TeacherCode",
                table: "Teachers",
                column: "TeacherCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_UserId",
                table: "Teachers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Teachers_Username",
                table: "Teachers",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Teachers_TeacherId",
                table: "Attendances",
                column: "TeacherId",
                principalTable: "Teachers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_AspNetUsers_ApplicationUserId",
                table: "Batches",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_Teachers_PrimaryInstructorId",
                table: "Batches",
                column: "PrimaryInstructorId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_Teachers_SecondaryInstructorId",
                table: "Batches",
                column: "SecondaryInstructorId",
                principalTable: "Teachers",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Teachers_TeacherId",
                table: "Attendances");

            migrationBuilder.DropForeignKey(
                name: "FK_Batches_AspNetUsers_ApplicationUserId",
                table: "Batches");

            migrationBuilder.DropForeignKey(
                name: "FK_Batches_Teachers_PrimaryInstructorId",
                table: "Batches");

            migrationBuilder.DropForeignKey(
                name: "FK_Batches_Teachers_SecondaryInstructorId",
                table: "Batches");

            migrationBuilder.DropTable(
                name: "Teachers");

            migrationBuilder.DropIndex(
                name: "IX_Batches_PrimaryInstructorId",
                table: "Batches");

            migrationBuilder.DropIndex(
                name: "IX_Batches_SecondaryInstructorId",
                table: "Batches");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_TeacherId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "PrimaryInstructorId",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "SecondaryInstructorId",
                table: "Batches");

            migrationBuilder.DropColumn(
                name: "TeacherId",
                table: "Attendances");

            migrationBuilder.RenameColumn(
                name: "ApplicationUserId",
                table: "Batches",
                newName: "InstructorId");

            migrationBuilder.RenameIndex(
                name: "IX_Batches_ApplicationUserId",
                table: "Batches",
                newName: "IX_Batches_InstructorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Batches_AspNetUsers_InstructorId",
                table: "Batches",
                column: "InstructorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
