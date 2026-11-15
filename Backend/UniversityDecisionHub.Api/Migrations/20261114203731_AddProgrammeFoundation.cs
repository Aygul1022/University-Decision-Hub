using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace UniversityDecisionHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddProgrammeFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ProgrammeId",
                table: "UserAccounts",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DepartmentKey",
                table: "Departments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Programmes",
                columns: table => new
                {
                    ProgrammeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProgrammeKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DepartmentId = table.Column<int>(type: "integer", nullable: false),
                    ProgrammeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DegreeLevel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    InstructionLanguage = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BolognaProgrammeId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Programmes", x => x.ProgrammeId);
                    table.ForeignKey(
                        name: "FK_Programmes_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "DepartmentId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ProgrammeCourses",
                columns: table => new
                {
                    ProgrammeCourseId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProgrammeId = table.Column<int>(type: "integer", nullable: false),
                    CourseId = table.Column<int>(type: "integer", nullable: false),
                    CatalogueYear = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Semester = table.Column<int>(type: "integer", nullable: true),
                    RecommendedStudyYear = table.Column<int>(type: "integer", nullable: true),
                    IsElective = table.Column<bool>(type: "boolean", nullable: true),
                    CourseCredit = table.Column<decimal>(type: "numeric", nullable: true),
                    Ects = table.Column<decimal>(type: "numeric", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgrammeCourses", x => x.ProgrammeCourseId);
                    table.CheckConstraint("CK_ProgrammeCourses_Credit", "\"CourseCredit\" IS NULL OR \"CourseCredit\" >= 0");
                    table.CheckConstraint("CK_ProgrammeCourses_Ects", "\"Ects\" IS NULL OR \"Ects\" >= 0");
                    table.CheckConstraint("CK_ProgrammeCourses_Semester", "\"Semester\" IS NULL OR \"Semester\" > 0");
                    table.CheckConstraint("CK_ProgrammeCourses_Year", "\"RecommendedStudyYear\" IS NULL OR \"RecommendedStudyYear\" > 0");
                    table.ForeignKey(
                        name: "FK_ProgrammeCourses_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "CourseId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProgrammeCourses_Programmes_ProgrammeId",
                        column: x => x.ProgrammeId,
                        principalTable: "Programmes",
                        principalColumn: "ProgrammeId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_ProgrammeId",
                table: "UserAccounts",
                column: "ProgrammeId");

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentKey",
                table: "Departments",
                column: "DepartmentKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeCourses_CourseId",
                table: "ProgrammeCourses",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeCourses_ProgrammeId_CourseId",
                table: "ProgrammeCourses",
                columns: new[] { "ProgrammeId", "CourseId" });

            migrationBuilder.CreateIndex(
                name: "IX_Programmes_DepartmentId",
                table: "Programmes",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Programmes_ProgrammeKey",
                table: "Programmes",
                column: "ProgrammeKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserAccounts_Programmes_ProgrammeId",
                table: "UserAccounts",
                column: "ProgrammeId",
                principalTable: "Programmes",
                principalColumn: "ProgrammeId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserAccounts_Programmes_ProgrammeId",
                table: "UserAccounts");

            migrationBuilder.DropTable(
                name: "ProgrammeCourses");

            migrationBuilder.DropTable(
                name: "Programmes");

            migrationBuilder.DropIndex(
                name: "IX_UserAccounts_ProgrammeId",
                table: "UserAccounts");

            migrationBuilder.DropIndex(
                name: "IX_Departments_DepartmentKey",
                table: "Departments");

            migrationBuilder.DropColumn(
                name: "ProgrammeId",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "DepartmentKey",
                table: "Departments");
        }
    }
}
