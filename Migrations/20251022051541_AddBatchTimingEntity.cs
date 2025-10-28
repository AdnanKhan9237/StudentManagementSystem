using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchTimingEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TimingId",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "BatchTimings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchId = table.Column<int>(type: "int", nullable: false),
                    TimingId = table.Column<int>(type: "int", nullable: false),
                    MaxStudents = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BatchTimings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BatchTimings_Batches_BatchId",
                        column: x => x.BatchId,
                        principalTable: "Batches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BatchTimings_Timings_TimingId",
                        column: x => x.TimingId,
                        principalTable: "Timings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Students_TimingId",
                table: "Students",
                column: "TimingId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchTimings_BatchId_TimingId",
                table: "BatchTimings",
                columns: new[] { "BatchId", "TimingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BatchTimings_TimingId",
                table: "BatchTimings",
                column: "TimingId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Timings_TimingId",
                table: "Students",
                column: "TimingId",
                principalTable: "Timings",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Timings_TimingId",
                table: "Students");

            migrationBuilder.DropTable(
                name: "BatchTimings");

            migrationBuilder.DropIndex(
                name: "IX_Students_TimingId",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "TimingId",
                table: "Students");
        }
    }
}
