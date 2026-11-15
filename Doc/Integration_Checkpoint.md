# Authentication integration checkpoint

10 November 2026. Current shared merge base: 62f052e on feature/integrationstart. This note supersedes older statements that authentication or UserAccounts/Instructors migrations do not exist.

## Completed

- Ozan account/authentication module is integrated with the existing catalog and review submission service.
- User DepartmentId and StudyYear remain nullable. Login is possible without them; reviewing requires a complete eligible profile.
- Course POST/PUT/DELETE require Administrator. Review POST requires authentication and still checks the database profile and authenticated author.
- Instructor name/title edits are supported. Department reassignment returns 409 until a coordinated transfer workflow can preserve minimum instructor counts.
- Catalog nested instructor input limits match the account branch mappings.

## Migration rollout pending approval

Apply existing migrations in order, then 20261110152153_IntegrateAuthenticationAccounts. The new migration extends existing tables; it does not recreate them. It adds PasswordHash (nullable), NormalizedEmail (unique) and CreatedAtUtc. No account receives a default password.

Preflight rejects oversized existing names/email/title, blank or non-ASCII legacy emails and normalized-email duplicates. Resolve those records deliberately before retrying; no rows are silently truncated or merged. Legacy ASCII emails are normalized with C collation to avoid database-locale-dependent ASCII casing. Existing CreatedAtUtc values are populated with the migration timestamp, not a claimed original registration date. NormalizedEmail is populated before NOT NULL and uniqueness are imposed.

The earlier Course-to-Department NOT VALID constraint still requires real department mapping and explicit validation. No migration has been applied by this task. Down removes new authentication fields including hashes; review rollback consequences before any use.

## Verified

- .NET 10 solution build: successful, zero warnings/errors.
- CourseChecks: 76 checks passed, no database connection.
- AccountChecks: initial 98/100 passed; final retest 110/110 passed after correcting old instructor-move expectations and adding Course authorization checks.
- AccountChecks uses real local HTTP with in-memory SQLite and synthetic data. Includes successful registration, login and review submission. This does not validate PostgreSQL migration execution or PostgreSQL concurrency behavior.

## Still pending

- Approved PostgreSQL test database setup, migration application and database-specific tests.
- Department names/selectors on frontend pages and offering-based instructor course displays.
- Sami review reading/moderation pages and agreement on review-link query parameters.
- Reviewed school-data import and email-ownership/activation policy before real users. The inherited self-registration flow matches stored profile details; it does not verify mailbox ownership.

This turn did not push or apply database migrations. Earlier implementation/test records are retained as historical evidence.

## Local rollout update on 10 November 2026

Following explicit approval, all five migrations were applied to Aygul's local UniversityDecisionHub database. Five history rows and nine application tables plus the history table were verified. Earlier pending statements above describe the previous checkpoint. Other teammates must apply the same migrations to their own local databases using private settings; see Local_Database_Setup.md. The legacy Course department FK remains NOT VALID. No accounts or school data were imported; no push occurred. PostgreSQL-backed API workflows remain pending.
