using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddBatchTimingsAndStudentTiming : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TimingId to Students table
            migrationBuilder.AddColumn<int>(
                name: "TimingId",
                table: "Students",
                type: "INTEGER",
                nullable: true);

            // Create BatchTimings junction table for many-to-many relationship
            migrationBuilder.CreateTable(
                name: "BatchTimings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    BatchId = table.Column<int>(type: "INTEGER", nullable: false),
                    TimingId = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxStudents = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 50),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP"),
                    CreatedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ModifiedDate = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ModifiedBy = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsDeleted = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false)
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

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_Students_TimingId",
                table: "Students",
                column: "TimingId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchTimings_BatchId",
                table: "BatchTimings",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_BatchTimings_TimingId",
                table: "BatchTimings",
                column: "TimingId");

            // Create unique constraint to prevent duplicate batch-timing combinations
            migrationBuilder.CreateIndex(
                name: "IX_BatchTimings_BatchId_TimingId",
                table: "BatchTimings",
                columns: new[] { "BatchId", "TimingId" },
                unique: true);

            // Add foreign key for Student.TimingId
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
