using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using UniversityDecisionHub.Api.Data;

namespace UniversityDecisionHub.Api.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261022000000_AddRecommendedStudyYear")]
public partial class AddRecommendedStudyYear : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Do not infer a study year for existing courses from their code.
        migrationBuilder.AddColumn<int>(
            name: "RecommendedStudyYear", table: "Courses", type: "integer", nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "RecommendedStudyYear", table: "Courses");
    }
}
