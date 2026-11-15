# Catalogue version 2 and review integration

Updated 15 November 2026. This contract supersedes the legacy import instructions. Code is implemented and compiles; file-only validation and EF snapshot checks passed. User-supplied output confirms 79 CourseChecks and 134 in-memory SQLite/HTTP AccountChecks passed. Real PostgreSQL migration/import has NOT been performed.

## Migration prerequisite

Each teammate uses their own local database and private connection settings. Pending migrations are `20261114203731_AddProgrammeFoundation` and `20261114204921_AlignCatalogAndReviewWorkflow` (plus any older unapplied migrations). Review them before application. Do not start the new API against the old schema: mapped columns must exist. No automatic migration on startup was added.

Upgrade keeps existing tables, relationships, source keys and review history. DepartmentCode becomes nullable and nonunique. Course credits/ECTS become nullable numeric; flags become nullable. Existing zero/false values are preserved rather than guessed to mean unknown. New imports put curriculum-specific values in ProgrammeCourses, leaving legacy Course values null. Legacy review OfferingId remains stored but becomes optional; new reviews use a direct restricted InstructorId FK. Existing reviews get no invented attendance declaration/year/semester. Rollback is not routine: the migration refuses incompatible null/fractional/repeated-code or declared-review data instead of fabricating defaults. Back up first.

## JSON file format

Use UTF-8 JSON. `schemaVersion` must be 2. All six arrays must be present, but may be empty. The file is self-contained: include the parent rows referenced by child rows even when they already exist locally. Exact existing rows are skipped.

| Array | Required source fields | Optional fields |
| --- | --- | --- |
| faculties | facultyKey, facultyName | none |
| departments | departmentKey, facultyKey, departmentName | departmentCode |
| instructors | instructorKey, departmentKey, fullName | academicTitle |
| programmes | programmeKey, departmentKey, programmeName | degreeLevel, instructionLanguage, bolognaProgrammeId |
| courses | courseKey, departmentKey, courseCode, courseName | courseDescription |
| programmeCourses | curriculumKey, programmeKey, courseKey | catalogueYear, semester, recommendedStudyYear, isElective, courseCredit, ects |

Keys are source references, not local database IDs. Keys are trimmed and normalized to uppercase, max 100 characters, and unique within each entity list. Names/official codes are trimmed without replacing source language. CourseCode is max 20, DepartmentCode max 30, names max 200, instructor name max 150 and title max 50. Unknowns use JSON null or an omitted optional property, never literal placeholder text. Semester and recommendedStudyYear are positive integers when known; credits and ECTS are nonnegative decimals; isElective is true/false/null. CatalogueYear is source text (max 50), not proof of an actual offering.

CourseKey identifies a real course independently of its appearances in programmes. The existing department/course-code uniqueness remains; the same code is allowed in different departments. CurriculumKey identifies one source occurrence across reimports; different years/semesters can point to the same course. Keep both keys stable and preserve a conversion mapping outside the raw workbook. The final workbook-to-JSON conversion and identity reconciliation will be done when files arrive; Umran need not repeatedly rearrange sheets to match this JSON.

FacultyKey and InstructorKey map to existing ImportKey fields; DepartmentKey, ProgrammeKey, CourseKey and CurriculumKey map to their named fields. PostgreSQL generates local IDs and the importer resolves references. Faculty with no departments and department with no instructors are accepted. Unresolved parents are rejected: retain those rows in a pending source list, never assign a guessed department.

## Import behaviour

`validate-catalog <absolute-file>` performs no database access. Unknown fields, duplicate JSON properties, invalid values/keys and unresolved parents are rejected. Version 1 is rejected with an explicit conversion message; no silent fallback to official codes.

`import-catalog <absolute-file> --apply` requires a local UniversityDecisionHub database and fully applied migrations. It validates again and imports faculties, departments, instructors/programmes/courses, then curriculum entries in one serializable transaction. Identical rows skip. Conflicting source keys report entity, key and differing field names without overwriting existing records. Review/correct approved changes explicitly before retrying; automatic upsert/delete is intentionally not enabled. Missing source rows never delete database records. Ambiguous legacy identities require reconciliation. Concurrency conflicts roll back; connection failure during commit requires checking the outcome before retrying.

Example invocation from repository root (validation only):

```powershell
$catalogPath = (Resolve-Path 'path-to-your-private-catalog.json').Path
dotnet run --project Backend/UniversityDecisionHub.Api --no-launch-profile -- validate-catalog "$catalogPath"
```

The repository example remains synthetic and was updated to v2 only as a file-format example. It has not been imported into PostgreSQL. Do not import it as school data.

## Student profile and lookup

- GET /api/programmes: public programme lookup, optional positive departmentId filter. Returns programme ID/name, department, degree and language.
- GET /api/programmes/{id}/courses: authenticated curriculum lookup, all historical occurrences without active-course filtering. Unknown values are returned as JSON null. Frontend should display Unknown/Bilinmiyor, not insert these strings into the database.
- Registration accepts optional ProgrammeId while retaining the existing pre-registered name/email/department/year match. Omission preserves a preassigned programme; no programme is inferred from department alone.
- PUT /api/auth/me and administrator user POST/PUT accept optional ProgrammeId. A selected programme supplies DepartmentId; a conflicting supplied department or nonexistent programme returns 400. Profile output includes ProgrammeId. PUT is replacement: omitted/null ProgrammeId clears the selection. Frontend must send the selected value.
- Legacy users may still log in without a programme, but must select one and a study year before reviewing. CSV pre-registration remains supported without guessing a programme; the user can select it in their profile.

## Review submission contract for Sami and Ozan

POST /api/reviews requires authentication and the following body:

```json
{
  "courseId": 123,
  "instructorId": 456,
  "academicYear": "2025-2026",
  "semester": "Fall",
  "attendanceConfirmed": true,
  "reviewText": "Student review text"
}
```

IDs above are placeholders, not inserted records. AcademicYear uses consecutive YYYY-YYYY years; Semester uses Fall, Spring or Summer (localize display labels in the UI). AttendanceConfirmed must be true. OfferingId and UserId must not be submitted; unknown request fields are rejected. Author comes from the authenticated identity, time from the server.

Any curriculum in the student's own programme grants course membership, including historical electives and courses owned by another department. Active status, actual offerings and instructor assignments are not prerequisites. Any existing instructor may be selected. A known recommended year must be met. If all matching years are unknown, membership suffices. Conflicting known years are temporarily refused with a clarification message until the team approves a conflict policy. Unknown class does not waive the student's own programme/year profile requirement.

New reviews retain direct course/instructor/year/semester/attestation, with OfferingId null. Existing offering and assignment tables and legacy links are preserved. One user/course review remains unique pending the team's decision. Reading/moderation implementations must handle nullable offering and nullable historical declaration fields. No new read/moderation endpoints or frontend screens were implemented in this change.

## Verification

API, CourseChecks and AccountChecks compile. Eight file-only v2 CLI cases passed and EF reports no pending model changes. Earlier assistant runs were blocked by Application Control; a later assistant CourseChecks run passed 79 checks while AccountChecks reached a sandbox socket restriction (10013). On 15 November 2026, user-supplied output confirms all 79 CourseChecks and all 134 AccountChecks passed, including HTTP submission/profile flows and SQLite repeated import/conflict rollback. No repeat run was performed merely to record user evidence. Real PostgreSQL migration application/import, concurrency and workbook-specific identity resolution remain unverified. Publication status is recorded separately in the implementation log.
