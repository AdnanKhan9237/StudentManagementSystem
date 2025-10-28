using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixStudentRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Sessions_SessionId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Sessions_SessionId1",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Trades_TradeId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Trades_TradeId1",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_SessionId1",
                table: "Students");

            migrationBuilder.DropIndex(
                name: "IX_Students_TradeId1",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "SessionId1",
                table: "Students");

            migrationBuilder.DropColumn(
                name: "TradeId1",
                table: "Students");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Sessions_SessionId",
                table: "Students",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Trades_TradeId",
                table: "Students",
                column: "TradeId",
                principalTable: "Trades",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_Sessions_SessionId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_Students_Trades_TradeId",
                table: "Students");

            migrationBuilder.AddColumn<int>(
                name: "SessionId1",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TradeId1",
                table: "Students",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Students_SessionId1",
                table: "Students",
                column: "SessionId1");

            migrationBuilder.CreateIndex(
                name: "IX_Students_TradeId1",
                table: "Students",
                column: "TradeId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Sessions_SessionId",
                table: "Students",
                column: "SessionId",
                principalTable: "Sessions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Sessions_SessionId1",
                table: "Students",
                column: "SessionId1",
                principalTable: "Sessions",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Trades_TradeId",
                table: "Students",
                column: "TradeId",
                principalTable: "Trades",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_Trades_TradeId1",
                table: "Students",
                column: "TradeId1",
                principalTable: "Trades",
                principalColumn: "Id");
        }
    }
}
