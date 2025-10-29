using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudentManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionRegistrationWindowFix : Migration
    {
        /// <inheritdoc />
protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add missing columns if they do not already exist
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Sessions', 'RegistrationStartDate') IS NULL
    ALTER TABLE dbo.Sessions ADD RegistrationStartDate datetime2 NULL;
IF COL_LENGTH('dbo.Sessions', 'RegistrationEndDate') IS NULL
    ALTER TABLE dbo.Sessions ADD RegistrationEndDate datetime2 NULL;
");
        }

        /// <inheritdoc />
protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Safe drop if exists
            migrationBuilder.Sql(@"
IF COL_LENGTH('dbo.Sessions', 'RegistrationStartDate') IS NOT NULL
    ALTER TABLE dbo.Sessions DROP COLUMN RegistrationStartDate;
IF COL_LENGTH('dbo.Sessions', 'RegistrationEndDate') IS NOT NULL
    ALTER TABLE dbo.Sessions DROP COLUMN RegistrationEndDate;
");
        }
    }
}
