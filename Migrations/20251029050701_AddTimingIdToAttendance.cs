using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddTimingIdToAttendance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimingId",
                table: "Attendances",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Attendances_TimingId",
                table: "Attendances",
                column: "TimingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendances_Timings_TimingId",
                table: "Attendances",
                column: "TimingId",
                principalTable: "Timings",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendances_Timings_TimingId",
                table: "Attendances");

            migrationBuilder.DropIndex(
                name: "IX_Attendances_TimingId",
                table: "Attendances");

            migrationBuilder.DropColumn(
                name: "TimingId",
                table: "Attendances");
        }
    }
}
