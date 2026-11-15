using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniversityDecisionHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlignCatalogAndReviewWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments");

            migrationBuilder.AlterColumn<int>(
                name: "OfferingId",
                table: "Reviews",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "AcademicYear",
                table: "Reviews",
                type: "character varying(9)",
                maxLength: 9,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AttendanceConfirmed",
                table: "Reviews",
                type: "boolean",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Semester",
                table: "Reviews",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurriculumKey",
                table: "ProgrammeCourses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DepartmentCode",
                table: "Departments",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<bool>(
                name: "IsElective",
                table: "Courses",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Courses",
                type: "boolean",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<decimal>(
                name: "Ects",
                table: "Courses",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "CourseCredit",
                table: "Courses",
                type: "numeric",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "CourseKey",
                table: "Courses",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reviews_InstructorId",
                table: "Reviews",
                column: "InstructorId");

            migrationBuilder.CreateIndex(
                name: "IX_ProgrammeCourses_CurriculumKey",
                table: "ProgrammeCourses",
                column: "CurriculumKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments",
                column: "DepartmentCode");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CourseKey",
                table: "Courses",
                column: "CourseKey",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Reviews_Instructors_InstructorId",
                table: "Reviews",
                column: "InstructorId",
                principalTable: "Instructors",
                principalColumn: "InstructorId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DO $$ BEGIN
                IF EXISTS (SELECT 1 FROM ""Reviews"" WHERE ""OfferingId"" IS NULL OR ""AttendanceConfirmed"" IS NOT NULL)
                    OR EXISTS (SELECT 1 FROM ""Departments"" WHERE ""DepartmentCode"" IS NULL)
                    OR EXISTS (SELECT 1 FROM ""Departments"" GROUP BY ""DepartmentCode"" HAVING COUNT(*) > 1)
                    OR EXISTS (SELECT 1 FROM ""Courses"" WHERE ""CourseCredit"" IS NULL OR ""Ects"" IS NULL
                        OR ""IsElective"" IS NULL OR ""IsActive"" IS NULL
                        OR ""CourseCredit"" <> trunc(""CourseCredit"") OR ""Ects"" <> trunc(""Ects"")
                        OR ""CourseCredit"" > 2147483647 OR ""Ects"" > 2147483647)
                THEN RAISE EXCEPTION 'Downgrade would lose or invent data. Reconcile records and back up before rollback.';
                END IF;
            END $$;");

            migrationBuilder.DropForeignKey(
                name: "FK_Reviews_Instructors_InstructorId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_Reviews_InstructorId",
                table: "Reviews");

            migrationBuilder.DropIndex(
                name: "IX_ProgrammeCourses_CurriculumKey",
                table: "ProgrammeCourses");

            migrationBuilder.DropIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments");

            migrationBuilder.DropIndex(
                name: "IX_Courses_CourseKey",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "AcademicYear",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "AttendanceConfirmed",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "Semester",
                table: "Reviews");

            migrationBuilder.DropColumn(
                name: "CurriculumKey",
                table: "ProgrammeCourses");

            migrationBuilder.DropColumn(
                name: "CourseKey",
                table: "Courses");

            migrationBuilder.AlterColumn<int>(
                name: "OfferingId",
                table: "Reviews",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "DepartmentCode",
                table: "Departments",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsElective",
                table: "Courses",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.AlterColumn<bool>(
                name: "IsActive",
                table: "Courses",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Ects",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "CourseCredit",
                table: "Courses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(decimal),
                oldType: "numeric",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Departments_DepartmentCode",
                table: "Departments",
                column: "DepartmentCode",
                unique: true);
        }
    }
}
