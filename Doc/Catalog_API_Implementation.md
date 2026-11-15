# Academic catalog API implementation

The existing Course controller, DTOs, model and prerequisite code are preserved. The DbContext now adds Faculty, Department, Instructor, CourseOffering and OfferingInstructor. No frontend, login or review implementation is added.

## Routes

| Route | Methods | Purpose |
|---|---|---|
| /api/faculties | GET, POST | List faculties; create a faculty with initial departments/instructors |
| /api/faculties/{id} | GET, PUT, DELETE | Read/rename; deletion is blocked while departments exist |
| /api/departments?facultyId=1 | GET | List/filter departments |
| /api/departments | POST | Create a department with at least one instructor in an existing faculty |
| /api/departments/{id} | GET, PUT, DELETE | Read with instructors; edit code/name; guarded deletion |
| /api/offerings?courseId=1 | GET | List/filter offerings |
| /api/offerings | POST | Create an offering for an existing course |
| /api/offerings/{id} | GET, PUT, DELETE | Read/edit period/status; deletion blocked by assignments |
| /api/offerings/{id}/instructors | GET, POST | Read assigned instructor details or assign an existing instructor |
| /api/offerings/{id}/instructors/{instructorId} | DELETE | Remove an assignment |

All new write actions require an authenticated principal with the Administrator role. The current app has no authentication handler: these actions intentionally return 401 until the authentication module is integrated. Authenticated nonadministrators receive 403. No client-supplied administrator flag or test bypass is implemented. Existing Course endpoints retain their current behavior and still need the team's later authorization integration.

## Requests

Create a faculty and its initial catalog in one atomic save:

```json
{
  "facultyName": "Engineering",
  "departments": [
    {
      "departmentCode": "COME",
      "departmentName": "Computer Engineering",
      "instructors": [{ "fullName": "Example Instructor", "academicTitle": "Professor" }]
    }
  ]
}
```

Create another department: use one department object above plus facultyId. Faculty PUT accepts facultyName only. Department PUT accepts departmentCode and departmentName only; moving departments between faculties is not implemented. Instructor editing/moving/deletion and standalone instructor APIs remain outside this increment; initial instructors are created within the department request.

Create an offering:

```json
{ "courseId": 1, "academicYear": "2026-2027", "semester": "Fall", "status": "Active" }
```

Offering PUT accepts academicYear, semester and status. Its course cannot be reassigned. Period/status values must be nonblank but no fixed vocabulary or offering uniqueness rule is imposed before those requirements are agreed. Course.IsActive remains unchanged for compatibility; it is not automatically synchronized with offering status.

Assign an instructor with POST /api/offerings/1/instructors:

```json
{ "instructorId": 1 }
```

Responses: successful creation 201, update/delete 204, missing resource 404, invalid request/reference 400, duplicate or in-use record 409. Concurrent serializable department deletion conflicts return 409; reload before retrying.

## Integrity and migration

- Faculty creation requires at least one department; department creation requires at least one instructor. Nested records are saved atomically.
- Department deletion uses a serializable transaction and rejects deleting the faculty's last department. It removes that department's instructors in the same transaction, but existing courses or teaching assignments block the entire deletion. No partial delete is committed.
- Faculty deletion never cascades departments. Remove/reorganize rules for deleting an entire faculty are not implemented.
- Minimum-child rules are enforced through these API workflows, not database triggers. Direct SQL/imports must preserve these rules too. Future instructor removal must guard against removing the last instructor.
- DepartmentCode is unique; the assignment primary key is (OfferingId, InstructorId). All new foreign keys restrict deletion. No cross-department instructor restriction was invented.
- The migration is prepared, NOT applied. The Course-to-Department foreign key is added NOT VALID to preserve old Course rows with legacy department IDs. New or changed department references are enforced. Reconcile every old course with real catalog data before declaring migration rollout complete.
- After reconciliation, explicitly validate the constraint:

```sql
ALTER TABLE "Courses" VALIDATE CONSTRAINT "FK_Courses_Departments_DepartmentId";
```

Inspect unresolved legacy IDs first:

```sql
SELECT c."CourseId", c."DepartmentId"
FROM "Courses" c
LEFT JOIN "Departments" d ON d."DepartmentId" = c."DepartmentId"
WHERE d."DepartmentId" IS NULL;
```

No fabricated catalog records or automatic database updates are included. Rolling back the catalog migration drops the new catalog tables and their data; it does not drop Courses.

## Verification

The existing console checks now cover the expanded model, DTO validation, migration SQL/snapshot, administrator filter, protected write actions and rejection of empty nested catalog requests. Run:

```text
dotnet run --project Tests/UniversityDecisionHub.CourseChecks
```

These are not PostgreSQL integration tests. Before deployment, use a disposable database to test the migration with existing course rows, authenticated CRUD, duplicate assignments, invalid references, blocked deletes, rollback of nested creation, and concurrent department deletes. No live database test or migration application is claimed.
