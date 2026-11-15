using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniversityDecisionHub.Api.Migrations;

// Prepared explicitly because local Windows Application Control blocked EF scaffolding.
public partial class AddCatalogImportKeys : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(name: "ImportKey", table: "Faculties",
            type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.AddColumn<string>(name: "ImportKey", table: "Instructors",
            type: "character varying(100)", maxLength: 100, nullable: true);
        migrationBuilder.CreateIndex(name: "IX_Faculties_ImportKey", table: "Faculties", column: "ImportKey", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Instructors_ImportKey", table: "Instructors", column: "ImportKey", unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Faculties_ImportKey", table: "Faculties");
        migrationBuilder.DropIndex(name: "IX_Instructors_ImportKey", table: "Instructors");
        migrationBuilder.DropColumn(name: "ImportKey", table: "Faculties");
        migrationBuilder.DropColumn(name: "ImportKey", table: "Instructors");
    }
}
