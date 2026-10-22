# University Decision Hub team work plan

## Starting point

The course-only checkpoint is the shared starting point. It contains course CRUD, description and recommended-year fields, DTO validation, the existing prerequisite relationships, migrations, manual requests and 16 executable course/model checks. Accounts, authentication, faculties/departments, instructors, offerings and reviews are not implemented. Their folders are placeholders.

The planned difficulty order is Aygül > Sami > Ozan > Ümran. Aygül, Sami and Ozan have comparable overall workloads; the distribution reflects complexity rather than a count of files. Ümran has a deliberately smaller data-support role. Backend work comes first; frontend assignments below are a later phase.

## Aygül — catalog, teaching periods and integration

Primary folders: `Backend/UniversityDecisionHub.Api/Features/Departments`, `Features/Courses`, `Features/Offerings`, and coordinated changes in `Data`, `Migrations`, and `DataBase`.

1. Implement Faculty and Department records, their relationship, DTOs and catalog endpoints. Keep the existing Course implementation and extend its department relationship rather than duplicating it.
2. Complete catalog validation: department existence, unique department/course-code pairs, explicit course study years, credit/ECTS rules, and useful error responses. Review existing unknown study years with Ümran; do not invent them.
3. Implement CourseOffering and OfferingInstructor, with academic year, semester and status. Coordinate instructor IDs with Sami. Provide the endpoint that lists only instructors assigned to a selected offering.
4. Own the reusable review-eligibility logic, initially in `Features/Reviews/ReviewEligibilityService.cs`: authenticated student profile, matching department, sufficient study year, existing author/course review, and valid offering/instructor context. Ozan calls this logic rather than copying it into his controller. Agree the service input/output before coding.
5. Coordinate EF mappings, foreign/composite keys and the one-review-per-author/course database constraint. Review migrations after integrating model changes. Migration creation and application must be coordinated, not performed concurrently by everyone.
6. Validate and import Ümran's prepared sample data after the schema is approved. Coordinate integration and end-to-end tests, while each feature owner fixes their own failures.
7. Later frontend: course/catalog pages and the administrator course/offering assignment screens. Work with Sami on shared navigation and Ozan on the review selection flow.

Tests owned: catalog validation and uniqueness; valid/missing departments; course-year eligibility boundaries; offering/instructor mismatches; preservation of historical references; migration compatibility; end-to-end integration with teammates.

Deliverable: a stable catalog and teaching context, a documented eligibility service, coordinated schema changes, and an integration checklist. Aygül does not implement every teammate's controller or fix all their bugs.

## Sami — accounts, permissions and instructor profiles

Primary folders: `Backend/UniversityDecisionHub.Api/Features/Authentication`, `Features/Users`, and `Features/Instructors`.

1. Implement registration and login using an agreed ASP.NET Core authentication approach. Hash passwords; keep credentials out of source control. Registration must not allow users to assign themselves administrator privileges.
2. Implement student profile information: full name, email, department ID and study year. Use the shared Department entity from Aygül. Profile information is self-declared in this release; do not add email-domain verification or document uploads.
3. Provide a reliable current-user identity/profile contract for the review workflow, plus reusable administrator authorization. Configure the shared authentication/authorization pipeline in Program.cs in coordination with Aygül.
4. Plan explicit initial-account setup with local credentials. Agree profile-update rules and account activation before implementing them. Do not seed a shared default password.
5. Implement instructor catalog records and list/detail/create/update endpoints. Administrator-only writes; use department IDs from the shared catalog. An instructor record is not automatically a user account. Offerings and instructor assignments remain Aygül's responsibility.
6. Provide small data support when needed: check that department references and instructor names in prepared data match the approved catalog. Ümran and Aygül remain the main data contributors.
7. Later frontend: registration, login, shared navigation and access-state messages; instructor list/detail pages. Full review content remains behind login even when linked from instructor pages.

Tests owned: successful/invalid registration and login; duplicate accounts; password handling; unauthenticated access; ordinary user versus administrator authorization; instructor input and department validation; current-user profile responses without password hashes.

Deliverable: working identity and authorization contracts plus instructor catalog APIs, documented setup and reproducible feature tests. Aygül and Ozan apply Sami's authorization mechanism to their own endpoints.

## Ozan — review operations, reading and search

Primary folder: `Backend/UniversityDecisionHub.Api/Features/Reviews`, excluding Aygül's agreed eligibility-service file.

1. Implement Review and its create/read/update DTOs with the agreed author, course, offering and instructor references, text and timestamps. Coordinate the EF mapping with Aygül.
2. Implement review submission: obtain the author from Sami's authenticated identity, call Aygül's eligibility logic, save once, and return an appropriate response. Do not accept a client-selected author ID or duplicate the eligibility rules.
3. Publish successful submissions immediately. Return helpful errors for invalid input and duplicate submissions. The database uniqueness constraint must also protect concurrent requests.
4. Implement authenticated review reading, detail retrieval, filters by course code, department, instructor and academic year, and a useful empty-result response. Agree combined-filter behavior and pagination with the team.
5. Implement administrator-only review edit/delete endpoints using Sami's authorization. Ordinary users cannot edit/delete even their own review. Finalize hard versus soft deletion and resubmission policy with the team before coding deletion behavior.
6. Later frontend: review-writing form, review list/detail/search views, administrator review management, and the landing-page content/three clearly labeled sample reviews. The form consumes Aygül's offering/assignment APIs and Sami's identity/instructor APIs.
7. Keep review endpoint examples and test records current. Write the review section of the report and supply actual screenshots/results when available.

Tests owned: review request validation; successful save/read; unauthenticated reading rejection; service rejection responses; one-review rule across different offerings; search combinations; ordinary-user edit/delete rejection; administrator operations.

Deliverable: review endpoints and later pages that use the shared identity/catalog/eligibility contracts. Ozan is not responsible for authentication internals, migration coordination or course-offering modeling.

## Ümran — data preparation and quality checks

This role is unchanged and intentionally smaller.

1. Prepare a small agreed spreadsheet of departments, courses, instructors and teaching assignments.
2. Check missing values, duplicate course codes within departments and consistent names.
3. Record the source of each entry and flag uncertain values instead of guessing them.
4. Review the prepared dataset with Aygül and provide a short data-quality summary.

No responsibility for API development, authentication, migrations, overall database architecture or production deployment. Do not prepare real student passwords, identity documents or private student records as sample data.

## Shared contracts and editing boundaries

- One shared User model: Sami owns it. One Department/Course model: Aygül owns it. One Instructor model: Sami owns it. One Offering/Assignment model: Aygül owns it. One Review model: Ozan owns it.
- Instructor APIs describe people; offering APIs describe who taught which course and when. Review filters must preserve that distinction.
- Aygül owns eligibility decisions in one service; Ozan owns the request/save/display workflow. The database still enforces the author/course unique pair. Missing recommended years must not grant review eligibility.
- Every owner protects their own catalog/review writes with the shared administrator policy. Full review reads require authentication. Public landing samples must not expose the complete review collection.
- Coordinate changes to Program.cs, ApplicationDbContext.cs, solution/project dependencies, migrations and shared DTO contracts before merging. No mass folder restructuring or replacement of teammates' code.
- Keep the existing folder layout. Add feature-specific DTOs/services/configuration files only as needed. Do not create a second API project or parallel catalog.

## Work sequence

1. All: read the checkpoint and approved requirements; confirm route/DTO conventions and unresolved policies. Establish local configuration without sharing passwords.
2. Aygül: department/course foundations. Sami: authentication/profile foundations and instructor records. Ozan: review contracts, validation and search design; use agreed sample responses while dependent APIs are in development. Ümran: spreadsheet preparation.
3. Aygül: offerings, assignments and eligibility service. Sami: identity/administrator integration. Ozan: integrate submission, reading, filters and administrator review actions.
4. Integrate backend modules, review migrations together, test against an agreed test database, and resolve each owner's failures.
5. Begin the assigned frontend work only after the supporting API contracts are stable.

## Completion criteria for each task

Working code in the agreed folder; successful build; reproducible positive and negative tests; request/response examples; documentation of new configuration; and a reviewed pull request. Mark unexecuted tests as pending rather than passed. Each person writes their own report section and responds to integration issues in their module.

Future features remain deferred: student planner, prerequisite checking, alumni-document verification, email-domain verification, feedback delivery, multilingual interface, comparison and career filtering. Difficulty should be adjusted by moving small interface/documentation tasks, not by adding these features to the first release.

## Git collaboration

The published checkpoint is a review branch until merged into main. Do not assume main already contains it. Review/merge it as a team or explicitly branch from the checkpoint. Use separate feature branches and small pull requests. Never force-push a shared branch, commit local secrets, or include IDE/build artifacts. Each teammate applies approved migrations only to their own authorized database; pushing code does not update databases.
