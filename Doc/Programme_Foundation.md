# Programme foundation

> Current status (14 November 2026): this earlier checkpoint is superseded by [Catalogue version 2 and review integration](Catalog_V2_Contract.md). The new code now supports stable department keys, nullable codes, programmes/curricula and declaration-based reviews. Migrations are NOT applied; user-reported 79 model checks and 134 SQLite/HTTP checks passed on 15 November 2026. Earlier text below is retained as history.


14 November 2026. This is a small additive preparation, not the completed catalogue/review transition.

## Implemented

- Programmes: ProgrammeId, unique ProgrammeKey (100 characters), DepartmentId, ProgrammeName, nullable DegreeLevel, InstructionLanguage and BolognaProgrammeId.
- ProgrammeCourses: ProgrammeCourseId, ProgrammeId, CourseId, nullable CatalogueYear, Semester, RecommendedStudyYear, IsElective, CourseCredit and Ects. Credits use decimal/numeric, with no forced rounding. Repeated programme/course pairs are allowed to preserve curriculum occurrences.
- Departments.DepartmentKey: nullable unique source key, separate from the existing official code. Existing records are not assigned guessed keys.
- UserAccounts.ProgrammeId: nullable restricted foreign key. Existing profile APIs do not yet assign this field.
- ProgrammeReviewEligibility: isolated policy for any historical curriculum in the student's own programme, including electives. No active-course, department or instructor-assignment filter. Unknown recommended year permits review when membership/profile are valid. Known insufficient year rejects. Conflicting known years return a clarification error; minimum-year selection is not approved. This policy is not yet called by the existing submission service.

The migration 20261114203731_AddProgrammeFoundation adds these objects only. Its Up operation does not delete existing tables/columns, insert sample data or infer source relationships. Its Down removes the new objects and would lose their data; do not roll it back after import without a recovery plan.

## Not activated yet

The existing review endpoint still requires an offering and an assigned instructor, and uses the legacy department rule. Review count remains one per user/course pending the team decision. No new HTTP endpoints were added. Offering/assignment tables are retained.

DepartmentCode is still required and unique in the legacy schema and importer. Existing faculty/department APIs and validator still enforce minimum child counts. Course-level numeric fields are not yet nullable. These restrictions must be migrated together with their consumers; this foundation does not make the latest workbooks importable.

## Remaining integration before real import

1. Adapt the catalogue file contract/importer to DepartmentKey, nullable/nonunique official codes and optional child collections. Preserve immutable keys and reject ambiguous mappings.
2. Add programme/curriculum validation and import, with an explicit stable identity for repeated curriculum rows. Do not infer course ownership or collapse rows by course code alone.
3. Update course DTOs/model/import for unknown source values without zeros or false defaults. Do not move legacy curriculum values into programmes without evidence.
4. Add programme selection/profile validation with Ozan; reconcile legacy profiles explicitly. Expose safe programme DTOs without account secrets.
5. Switch review DTO/service/model together to course, instructor, academic year and semester plus user attestation; optional offering context must not manufacture assignments. Integrate the new programme policy only in this coordinated step.
6. Confirm review multiplicity and conflicting curriculum-year policy. Update meaningful tests for the activated flow.
7. Review and apply pending migrations separately on each teammate's own database before running changed API code. UserAccount reads now require ProgrammeId, so the old schema is not runtime-compatible with this build until migration application.

## Validation and rollout

API build succeeded with zero warnings/errors. EF generated the migration and snapshot successfully. CourseChecks was extended for the new policy, nullable fields, restrictive references and non-destructive upgrade; execution was blocked by Windows Application Control (0x800711C7), so these checks are not reported as passing. No database connection, migration application, data import, commit or push was performed for this batch.
