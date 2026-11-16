using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniversityDecisionHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddGraduateStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGraduate",
                table: "UserAccounts",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddCheckConstraint(
                name: "CK_UserAccounts_GraduateHasNoStudyYear",
                table: "UserAccounts",
                sql: "NOT (\"IsGraduate\" AND \"StudyYear\" IS NOT NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UserAccounts_GraduateHasNoStudyYear",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "IsGraduate",
                table: "UserAccounts");
        }
    }
}
