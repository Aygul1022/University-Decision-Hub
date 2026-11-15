# Review reading and search

The review reading API and browser page use the catalogue and review workflow published in `feature/backend` at `9fb1a60`. The read endpoints require authentication. Review submission remains `POST /api/reviews` under the shared [catalogue and review contract](Catalog_V2_Contract.md).

## Routes

- `GET /api/reviews` lists reviews. An empty search returns `items: []` and the matching total.
- `GET /api/reviews/{id}` returns one review, or 404 when it does not exist.

| Query | Behavior |
|---|---|
| `courseId` | Exact positive course ID. Course detail links can use this with the code and department. |
| `courseCode` | Case-insensitive partial match. Include `departmentId` to narrow a code shared across departments. |
| `departmentId` | Exact course department ID. |
| `instructorId` | Exact instructor ID. |
| `academicYear` | Exact year in YYYY-YYYY format. |
| `semester` | Exact `Fall`, `Spring` or `Summer`. |
| `page` | 1–100000, default 1. |
| `pageSize` | 1–100, default 20. |

Filters combine with AND. Results are ordered by `createdAt` descending, then `reviewId` descending.

## Response and compatibility

The list returns `{ page, pageSize, totalCount, items }`. The fields on each item and on the detail response are `reviewId`, `courseId`, `courseCode`, `courseName`, `departmentId`, `offeringId`, `academicYear`, `semester`, `instructorId`, `instructorName`, `reviewText` and `createdAt`. Author identity is private.

New reviews carry their own academic year and semester and return `offeringId: null`. Each missing period field on a legacy review falls back to its linked offering. A field without a source stays null. Year and semester filters use the same resolved fields that the API displays. No review requires an offering or assignment to be returned.

`Frontend/Pages/reviews/reviews.html` preserves course, instructor and period filters through sign-in and pagination. Editing the course code or department starts a new search and removes the course ID from the link. It renders review text as plain text. The catalogue team coordinates PostgreSQL migration rollout. Administrator moderation and any changes to review uniqueness depend on the team decision.
