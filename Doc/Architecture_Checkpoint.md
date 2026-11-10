# Course implementation checkpoint

This checkpoint adapts the existing course CRUD work to the revised plan. It does not implement the next team assignments.

## Implemented at this checkpoint

- Existing Course, CoursePrerequisite, DTOs, controller, EF context and original migration remain in their original locations.
- `RecommendedStudyYear` is stored on courses and returned by the API. Create/update requests require a positive explicit value. Existing rows remain null until a person supplies the correct year; course numbers are not used to guess it.
- Positive department IDs, nonnegative ECTS, and the original credit limits are validated. Department existence is not yet verified because there is no Department entity.
- Course plus prerequisite creation uses one atomic EF save. Updating prerequisites retains unchanged links. Nonexistent, duplicate and self-referencing prerequisite IDs are rejected. Student prerequisite validation and full cycle checking remain deferred.
- Duplicate key and reference conflicts return HTTP 409 instead of an unhandled database exception.
- Unsupported legacy search fields return HTTP 400 instead of being silently ignored. Name/code, department ID and credits filters remain available.
- The existing manual HTTP file has valid JSON, collection retrieval and year-validation examples.

## Architecture placeholders only

The following folders contain `.gitkeep` markers, not new feature code:

- `Features/Departments` — Aygül: faculty/department catalog and relationships.
- `Features/Users`, `Features/Authentication` — Ozan: accounts, profiles and access control.
- `Features/Instructors` — Ozan: instructor records and detail APIs.
- `Features/Offerings` — Aygül: teaching periods and assignments.
- `Features/Reviews` — Aygül: model/submission/eligibility; Sami: reading/search and administrator operations.
- `Data/Configurations`, `Data/Seeding` — future coordinated database work.

Database workload approved on 23 October 2026: Ümran 45%, Aygül 30%, Ozan 25%. This supersedes Ümran's earlier smaller data-support allocation and applies only to database work. See `Team_Work_Plan.md` for detailed responsibilities.

## Existing fields retained deliberately

Approved 24 October 2026: keep `CourseId` as the internal identifier, `CourseCode` as the full official code (for example `COME 327`), and `RecommendedStudyYear` as the eligibility value. Do not introduce a separate `CourseNumber` field. The existing `(DepartmentId, CourseCode)` uniqueness rule remains unchanged. This decision needs no code or database migration.

`CourseCode`, `CourseCredit`, `Ects`, `IsActive`, `IsElective`, `CourseDescription`, and prerequisite collections keep their existing names. The department/code unique index remains intact. `IsActive` is still the legacy catalog field; semester-specific availability will be implemented with offerings later. `DepartmentName` remains unpopulated until the department relationship exists.

No login, administrator enforcement, review eligibility, reviews, instructors, offerings, faculty or department entities are implemented at this checkpoint. Course mutation endpoints remain unprotected development endpoints. This is not a deployment-ready application.

## Database migration — pending user approval

`20261022000000_AddRecommendedStudyYear` adds one nullable integer column. It does not delete or rewrite existing records. The original migration is unchanged.

The new API needs this column before database-backed course requests can run. Do not run these commands until the database owner approves application:

```powershell
Update-Database -Migration AddRecommendedStudyYear -Context ApplicationDbContext -Args '--environment Development'
```

This command is for Visual Studio Package Manager Console with UniversityDecisionHub.Api selected. A fresh database applies both migrations. Existing databases apply only the pending migration. Each teammate supplies their own `ConnectionStrings:DefaultConnection` using local User Secrets. Passwords must not be committed.

## Verification

```powershell
dotnet build UniversityDecisionHub.slnx
dotnet run --project Tests/UniversityDecisionHub.CourseChecks/UniversityDecisionHub.CourseChecks.csproj
```

CourseChecks is a small executable verification project, not a `dotnet test` suite. It checks DTO validation, the two-entity model, migration discovery, snapshot consistency, and generated PostgreSQL migration SQL without connecting to a database. Runtime HTTP/database integration tests remain a separate step after approved migration application. Manual requests are in the existing API `.http` file; a positive POST inserts sample data, so run deliberately.

Verification recorded for this checkpoint: 16 checks passed; all four manual request JSON bodies parsed; solution build succeeded with no compilation errors. NuGet reported NU1900 because its vulnerability-data service could not be reached. No live database integration test was run. An explicit EF Core Relational 10.0.12 runtime reference aligns the API and check project with the already-used EF tooling version.

## Git handoff

The shared remote is https://github.com/Aygul1022/University-Decision-Hub.git. Aygül approved publication, but the branch-creation permission request was declined; publication remains pending. Merging into main and applying database migrations are separate steps. A new `.gitignore` prevents new IDE/build artifacts from being added, but previously tracked `.vs`, `obj`, and `.csproj.user` files are still tracked. Do not use `git add .`; stage only reviewed source, migration, placeholder, documentation, and test files. Tracked artifact cleanup is a separate decision. Existing uncommitted work predates this checkpoint and must be preserved.
