# University Decision Hub team work plan

Approved allocation updated 23 October 2026. This document supersedes the earlier task split.

## Current checkpoint

Only course CRUD and its DTOs, relationships, recommended-year migration, and course checks are implemented. Other feature folders are placeholders. The new migration has not been applied to the database by the assistant. The GitHub push was blocked when branch-creation permission was declined; do not assume the local checkpoint is on main or a published branch.

## Database allocation

These percentages apply only to database work: **Ümran 45%, Aygül 30%, Ozan 25%**. They are workload estimates, not percentages of the entire software project. Sami has no primary database allocation.

### Ümran 

1. Prepare a data dictionary for every table: field name, meaning, sample value, required/optional status, and format.
2. Collect source-documented faculties, departments, courses, recommended course years, instructors, teaching periods, and teaching assignments.
3. Organize the agreed sample dataset into consistent spreadsheets or CSV files.
4. Check missing values, duplicate codes within departments, inconsistent names, and invalid assignments. Flag uncertainty rather than inventing values.
5. Analyze normalization with Aygül: identify functional dependencies, repeated data, and documented exceptions.
6. Compare imported database records against the prepared source dataset and produce a data-quality report.

Deliverables: data dictionary, clean data files, source list, normalization notes, and data-quality report. No machine learning, API development, authentication, migration execution, or deployment responsibility. Do not collect real passwords or identity documents as sample data.

### Aygül

1. Translate approved data requirements into the ER diagram and relational schema.
2. Lead the technical design of Faculty, Department, Course, CourseOffering, OfferingInstructor, and Review relationships.
3. Implement EF mappings, primary/foreign/composite keys, nullability and unique constraints.
4. Enforce one review per author/course in the database, including consistency between the reviewed course, offering and assigned instructor.
5. Coordinate migrations and prepare the approved import/seed process for Ümran's data.
6. Verify migration compatibility, preservation of existing records, and backend/database integration.

Deliverables: ER diagram, relational schema, EF configuration, migrations and import procedure. Do not independently invent missing data. Coordinate all migration generation/application; other members propose changes rather than applying competing migrations.

### Ozan 

1. Design User and Instructor data fields and relationships with Aygül, contributing their definitions to Ümran's data dictionary.
2. Define email uniqueness, student department/study-year fields, and instructor department references.
3. Prepare report SQL queries for courses by department, reviews by course/instructor/year, offering instructors, and combined filters.
4. Prepare and execute database constraint tests: duplicate department/course codes, nonexistent references, repeat author/course reviews, invalid offering/instructor pairs, and agreed deletion behavior.
5. Prepare suitable test data with Ümran and document expected versus observed results.

Deliverables: User/Instructor data design, SQL query files and database test report. Migration coordination remains with Aygül.

## Software implementation allocation

The intended software difficulty order remains Aygül > Sami > Ozan, with similar overall workloads. The separate database allocation above gives Ümran the largest database workload.

### Aygül — catalog, review submission and integration

- Maintain existing Course code and implement Faculty/Department and offering/assignment APIs.
- Own Review model and create DTO in agreement with Sami, plus the review-submission service/controller.
- Enforce authenticated author identity, matching department, sufficient study year, one review per course, and valid assigned-instructor context before submission. Unknown course years must not grant eligibility.
- Coordinate integration; each teammate fixes their own module.
- Later UI: Write a Review, course/period/instructor selection, submission feedback, and administrator catalog/offering forms.
- Test catalog integrity, eligibility boundaries, invalid assignments, duplicate submissions and integration.

Primary areas: Features/Departments, Features/Courses, Features/Offerings; creation/model files in Features/Reviews; Data and Migrations.

### Sami — review reading, search and administrator review operations

- Own review list/detail APIs and reading/search DTOs; require authentication for the complete collection.
- Implement course-code, department, instructor and academic-year filtering, pagination and empty-result handling.
- Implement administrator-only review editing/deletion using Ozan's authorization mechanism. Ordinary authors cannot edit/delete their own reviews.
- Agree deletion/resubmission behavior with Aygül before implementation; no policy was finalized by the interrupted implementation.
- Later UI: Read Reviews, filters/results/details, administrator review controls, public landing page with three sample reviews and login redirect for More Reviews.
- Test reading access, filters, pagination, published-review visibility and administrator permissions.

Primary area: reading/query/administration files in Features/Reviews and corresponding frontend pages. Aygül owns submission; avoid duplicate routes or shared-file edits without coordination.

### Ozan — accounts, instructor records and detail pages

- Implement registration/login, secure password handling, student profiles and current-user identity.
- Supply shared administrator authorization. Public registration must not grant administrator access.
- Implement Instructor list/detail/create/update endpoints with administrator-only writes and shared department references.
- Later UI: Register, Login, Course Detail, Instructor List/Detail, shared navigation and login-state display.
- Reuse Aygül's Course APIs and Sami's filtered review APIs; do not duplicate Course or Review models.
- Test authentication, duplicate accounts, privilege boundaries, private-field exclusion, instructor validation and detail-page integration.

Primary areas: Features/Authentication, Features/Users, Features/Instructors, frontend auth/course-detail/instructor pages. Authentication configuration in Program.cs is coordinated with Aygül.

## Shared rules and handoffs

- Ümran owns data preparation/meaning; Aygül owns technical schema integration; Ozan verifies queries and constraints.
- One shared User and Instructor model owned by Ozan; one Department/Course/Offering model owned by Aygül; one Review model owned by Aygül with Sami's agreed reading/administration needs.
- Ozan supplies current-user profile and authorization; Aygül supplies catalog, teaching context and valid review creation; Sami supplies retrieval and management of published reviews.
- Backend logic enforces department/year eligibility. Database constraints enforce uniqueness and reference integrity. Both are required.
- Coordinate Program.cs, ApplicationDbContext.cs, migrations, dependencies and shared DTO contracts before editing/merging.
- Preserve the existing folder layout. New filenames and services are proposed responsibilities, not claims that those features already exist.
- Use approved test data and separate authorized test databases. Each owner records reproducible tests; unexecuted checks remain pending.
- Each person writes their own report section and fixes their own integration failures.

## Sequence

1. Approve table/field definitions: Ümran begins the data dictionary; Aygül models relationships; Ozan designs User/Instructor fields and database tests.
2. Build catalog and account foundations; Sami designs review reading contracts with Aygül while dependencies are developed.
3. Add offerings, review creation, reading/search and administrator operations; coordinate migrations and imports.
4. Test integrated backend behavior, then implement the assigned frontend views using stable API contracts.

Student planner, prerequisite checking, alumni/email verification, feedback delivery, multilingual support, comparison and career filtering remain deferred. No new implementation is authorized merely by recording this allocation.

## Git handoff

Publication remains pending. After authorized publication, agree whether to merge the checkpoint into main or branch from it explicitly. Use feature branches and reviewed pull requests, preserve teammates' work, and exclude credentials and IDE/build artifacts. Pushing source code does not apply database migrations.
