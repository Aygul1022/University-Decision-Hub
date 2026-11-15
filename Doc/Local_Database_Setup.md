# Local databases for team development

Each teammate runs PostgreSQL locally. Share source code and migrations through Git, not database files, passwords or local User Secrets. Each database has its own records; applying migrations does not copy another teammate's data.

## Personal connection settings

Create an empty local database named `UniversityDecisionHub`. In Visual Studio, right-click `UniversityDecisionHub.Api` and select **Manage User Secrets**. Merge this setting with any existing secrets, using your own local password:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=127.0.0.1;Port=5432;Database=UniversityDecisionHub;Username=postgres;Password=YOUR_LOCAL_PASSWORD"
  }
}
```

The example password is a placeholder. Never commit the actual settings. User Secrets are local development configuration, not an encrypted production secret store. Each teammate should also configure their own private `Jwt:Key` of at least 32 random characters; without it, development login tokens stop working after an API restart.

## Apply the shared migrations

After pulling the integrated code, select `UniversityDecisionHub.Api` as the startup project. In Visual Studio's **Package Manager Console**, select the same project as the default project and run:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
Update-Database -Context ApplicationDbContext
```

This uses the EF Tools package already referenced by the API. Apply existing migrations; do not generate a new initial migration or delete migration history just to set up another computer. Refresh Tables in pgAdmin afterwards.

Expected schema: Courses, CoursePrerequisites, Faculties, Departments, Instructors, CourseOfferings, OfferingInstructors, UserAccounts and Reviews, plus __EFMigrationsHistory. Five migrations currently end at `20261110152153_IntegrateAuthenticationAccounts`. Account registration requires predefined accounts; migration application alone creates no student or administrator accounts.

## Current local status

Catalog import update: a sixth migration, `20261110193326_AddCatalogImportKeys`, has now been applied and verified on Aygul's local database. Teammates must still apply it individually. It adds nullable import keys and unique indexes to Faculties and Instructors. The five-migration results below are the previous verified state. See Catalog_Import.md before using the importer.

On 10 November 2026 all five migrations were applied to Aygul's local database and the migration history and ten public tables were queried successfully. This does not establish the state of anyone else's database. No school data was imported.

The legacy `FK_Courses_Departments_DepartmentId` constraint remains marked NOT VALID by the shared migration. It enforces new references, but historical-row validation remains a separate rollout step. Do not assume all constraints have been validated merely because migration application succeeded.

`Scripts/Initialize-LocalPostgreSQL.ps1` was prepared for Aygul's approved recovery of a stopped PostgreSQL 18 service. It changes that service's data directory. It is not a routine setup script for teammates with working PostgreSQL installations.
