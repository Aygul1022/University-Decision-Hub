# Sami — Review Reading & Search (initial implementation)

**Branch:** `feature/sami-review-reading`  
**Base:** Aygül's catalog/submission checkpoint `3d625026` (`feature/integrationstart`).  
**Scope:** GET-only review reading. No shared files, POST handler, migrations or models changed.

## Routes and access

- `GET /api/reviews` — requires an authenticated user; default page 1, page size 20.
- `GET /api/reviews/{id}` — requires an authenticated user; 404 when not found.
- `POST /api/reviews` continues to belong exclusively to Aygül.
- Administrative PUT/DELETE deliberately **not implemented** until the team confirms deletion and resubmission rules.

## Optional list filters

| Query string | Meaning |
|---|---|
| `courseCode` | Case-insensitive substring of course code, e.g. `COME` |
| `departmentId` | Exact positive department identifier |
| `instructorId` | Exact positive instructor identifier |
| `academicYear` | Exact academic year such as `2026-2027` |
| `page` | 1–100000, default 1 |
| `pageSize` | 1–100, default 20 |

All supplied filters use **AND**. This combination rule is a proposal to confirm with Aygül before final acceptance.

Results are ordered by `createdAt` descending, then `reviewId` descending for stable page boundaries in an unchanged dataset.

## Response shape

The list returns `{ page, pageSize, totalCount, items }`; an empty result returns `items: []`, `totalCount: 0`. Every item contains `reviewId, courseId, courseCode, courseName, departmentId, offeringId, academicYear, semester, instructorId, instructorName, reviewText, createdAt`. Detail returns the same item shape.

Author id/name and author visibility are an open policy question, so this endpoint **does not reveal author identity**. Review text remains plain text; the future frontend must not render it as HTML.

## Integration boundaries

The controller reads Aygül's existing `Review`, `Course`, `CourseOffering` and `Instructor` models and creates no duplicate entity. It uses `[Authorize]` from ASP.NET Core; Ozan and Aygül must integrate JWT authentication/authorization and database mappings before HTTP behavior can be verified. Keep Aygül's `ReviewSubmissionController` and its POST route unchanged.

This branch should be merged or cherry-picked **after** Aygül/Ozan's integration branch is reviewed. Do not merge directly into `main` before that.

## Pending verification (not yet passed)

1. Build the merged solution with .NET 10.
2. In an approved disposable PostgreSQL test database, verify anonymous GET returns 401; authenticated GET returns 200.
3. Verify list/detail return the correct course, instructor and academic period; unknown detail returns 404.
4. Verify each filter and combinations; page boundaries, no-results response, invalid query values and deterministic ordering.
5. Verify a newly submitted review appears immediately via GET.
6. Verify no author identity is exposed and text is rendered safely.
7. Agree author visibility and AND filter semantics with Aygül. Implement and test admin edits/deletes **only after** deletion/resubmission policy is approved.

No live database, migrations or teammate-owned files were changed in this increment.
