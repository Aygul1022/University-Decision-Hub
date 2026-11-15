using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UniversityDecisionHub.Api.Migrations
{
    /// <inheritdoc />
    public partial class IntegrateAuthenticationAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Do not silently truncate legacy data or merge accounts during normalization.
            migrationBuilder.Sql("""
                DO $migration$
                BEGIN
                    IF EXISTS (SELECT 1 FROM "UserAccounts" WHERE length("FullName") > 150 OR length("Email") > 254)
                       OR EXISTS (SELECT 1 FROM "Instructors" WHERE length("FullName") > 150 OR length("AcademicTitle") > 50) THEN
                        RAISE EXCEPTION 'Legacy names/email/title exceed new limits. Review records before migrating.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM "UserAccounts" WHERE btrim("Email") = '' OR "Email" ~ '[^ -~]') THEN
                        RAISE EXCEPTION 'Legacy emails require manual normalization review before migrating.';
                    END IF;
                    IF EXISTS (SELECT 1 FROM "UserAccounts" GROUP BY upper(btrim("Email") COLLATE "C") HAVING count(*) > 1) THEN
                        RAISE EXCEPTION 'Legacy emails collide after normalization. Resolve duplicate accounts before migrating.';
                    END IF;
                END $migration$;
                """);

            migrationBuilder.DropIndex(
                name: "IX_UserAccounts_Email",
                table: "UserAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "UserAccounts",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "UserAccounts",
                type: "character varying(254)",
                maxLength: 254,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "UserAccounts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "UserAccounts",
                type: "character varying(254)",
                maxLength: 254,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PasswordHash",
                table: "UserAccounts",
                type: "text",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Instructors",
                type: "character varying(150)",
                maxLength: 150,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "AcademicTitle",
                table: "Instructors",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            // For legacy accounts, this timestamp records migration, not original registration.
            migrationBuilder.Sql("""
                UPDATE "UserAccounts"
                SET "NormalizedEmail" = upper(btrim("Email") COLLATE "C"),
                    "CreatedAtUtc" = CURRENT_TIMESTAMP;
                ALTER TABLE "UserAccounts" ALTER COLUMN "NormalizedEmail" SET NOT NULL;
                ALTER TABLE "UserAccounts" ALTER COLUMN "CreatedAtUtc" SET NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_NormalizedEmail",
                table: "UserAccounts",
                column: "NormalizedEmail",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserAccounts_NormalizedEmail",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "UserAccounts");

            migrationBuilder.DropColumn(
                name: "PasswordHash",
                table: "UserAccounts");

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "UserAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "UserAccounts",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(254)",
                oldMaxLength: 254);

            migrationBuilder.AlterColumn<string>(
                name: "FullName",
                table: "Instructors",
                type: "text",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150);

            migrationBuilder.AlterColumn<string>(
                name: "AcademicTitle",
                table: "Instructors",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserAccounts_Email",
                table: "UserAccounts",
                column: "Email",
                unique: true);
        }
    }
}
