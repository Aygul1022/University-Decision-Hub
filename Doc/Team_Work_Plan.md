# University Decision Hub team work plan

## Starting point and tools

The course-only checkpoint is the shared starting point. It contains course CRUD, course DTOs, prerequisite relationships, the recommended-study-year field, migrations and course checks. Accounts, authentication, faculties/departments, instructors, offerings and reviews are not implemented; their folders are placeholders. The `AddRecommendedStudyYear` migration has not been applied to every local database yet.

- **Backend:** C# / ASP.NET Core (.NET 10) with Entity Framework Core and PostgreSQL, developed in **Visual Studio**.
- **Frontend:** HTML and CSS (plus small `fetch` scripts in `Frontend/js` to call the API), developed in **VS Code**.

The intended backend/frontend difficulty order is **Aygül > Sami > Ozan > Ümran**, with comparable overall workloads for the first three. The database work is divided separately (see below).

## Database work: Ümran 45%, Aygül 30%, Ozan 25%

These percentages apply only to the database work, not to the backend and frontend tasks. Sami does not take part in the database work; he uses the shared entities and sends schema or index requests to Aygül.

| Person | Share | Main responsibility |
|---|---:|---|
| **Ümran** | **45%** | Data requirements, data preparation, quality checks and normalization analysis |
| **Aygül** | **30%** | Technical implementation of the relational design, EF Core configuration and migration management |
| **Ozan** | **25%** | User and instructor data design, SQL queries and database tests |

### Ümran — 45%

1. Write the **data dictionary** for every table: field name, description, example value, whether it is required, and the expected format.
2. Collect faculty, department, course, instructor and course-period information, and record the source of each entry.
3. Organize the data in shared templates:
   - Faculties and departments
   - Courses and their recommended study years
   - Instructors
   - Course–academic year–semester records
   - Course–instructor assignments per semester
4. Check for missing values, duplicate course codes within a department, inconsistent names and wrong assignments.
5. Flag uncertain values instead of guessing them, and resolve them with the team.
6. Carry out the normalization analysis with Aygül: document repeated data, functional dependencies and design exceptions.
7. After import, compare the source data with the database and write a short data-quality report.

**Deliverables:** data dictionary, cleaned data files, source list, normalization notes and data-quality report. No data-science or machine-learning work is expected.

### Aygül — 30%

1. Turn Ümran's data dictionary and the approved requirements into the **ER diagram and relational schema**.
2. Lead the technical design of:
   - `Faculty`, `Department`, `Course`
   - `CourseOffering`, `OfferingInstructor`
   - `Review` relationships and database constraints
3. Apply the primary key, foreign key, composite key, unique and null rules.
4. Enforce **one review per user per course** in the database.
5. Build the relationships that keep a review's course, offering and instructor consistent with each other.
6. Manage the EF Core configuration and the migration order. Prepare the import/seed process for Ümran's data.
7. Check backend integration and that existing data is preserved after migrations.

**Deliverables:** ER diagram, relational schema, EF configurations, migration files and the data-loading method.

### Ozan — 25%

1. Design the fields and relationships of the `User` and `Instructor` tables, and contribute them to Ümran's data dictionary.
2. Agree with Aygül on user email uniqueness, student department/year fields and the instructor–department link.
3. Write SQL queries for the report:
   - Courses by department
   - Reviews by course and by instructor
   - Reviews by academic year
   - Instructors who teach a given offering
   - Combined search filters
4. Write and run database tests:
   - Is a duplicate course code in the same department rejected?
   - Can a foreign key point to a record that does not exist?
   - Can the same user add a second review for the same course?
   - Can a review be created for an instructor not assigned to the course?
   - Does deleting a record keep references intact?
5. Prepare the test data with Ümran and record expected and actual results.

**Deliverables:** user/instructor data design, SQL query files and the database test report.

### How the database work fits together

- **Ümran** owns what the data means and makes sure it is prepared correctly.
- **Aygül** turns that design into a working database and application model.
- **Ozan** checks that it is usable through queries and correct through tests.
- **Aygül coordinates all migrations in a single order.** The three of them do not create separate migrations and apply them to the same database.
- The student year/department eligibility check happens in the backend. The database protects relationships and uniqueness.
- Each person writes the report section for their own part.

**First shared step:** the three of them approve the table and field list. Then Ümran starts the data dictionary, Aygül the relationships, and Ozan the user/instructor fields and test scenarios.

## Backend and frontend work

### Aygül — catalog, review submission and integration

Backend:

1. Implement **Faculty and Department** and their relationship.
2. Maintain and extend the existing **Course** model and endpoints.
3. Implement **CourseOffering** and **OfferingInstructor** (academic year, semester, assigned instructors).
4. Implement the shared **Review model and create-review DTO**; agree the response format with Sami.
5. Implement **review submission**:
   - Identify the author from the authenticated account (Ozan's contract).
   - Check that the student and course departments match.
   - Check that the student's year meets the course's recommended year. A missing year never grants eligibility.
   - Reject a second review for the same course.
   - Validate that the selected instructor teaches the selected offering.
   - Save the review and publish it immediately.
6. Coordinate integration. Each teammate fixes their own module.

Frontend (later): **Write a Review** page with course → academic year → semester → instructor selection, validation messages and confirmation; simple administrator catalog and offering-assignment forms.

Working areas: `Features/Departments/`, `Features/Courses/`, `Features/Offerings/`, `Features/Reviews/Review.cs`, `ReviewCreateDto.cs`, `ReviewSubmissionService.cs`, `ReviewSubmissionController.cs`, `Data/`, `Migrations/`, `Frontend/Pages/reviews/review-form.html`.

Tests: department/course relationships and duplicate course codes; student-year and department eligibility; duplicate reviews, including concurrent submissions; invalid instructor/offering combinations; migration and integration checks.

### Sami — review reading, searching and administrator review management

Backend:

1. Implement **review list and review detail** endpoints. The full review collection requires authentication.
2. Filters: course code, department, instructor, academic year. Add pagination and a consistent empty-result response.
3. Define the review-reading DTOs with Aygül.
4. Implement **administrator-only review editing and deletion** using Ozan's authorization. Ordinary users cannot edit or delete reviews, including their own.
5. Agree deletion/resubmission behavior with Aygül before implementing it.

Frontend (later): **Read Reviews** page (search fields, filters, results, pagination, empty states), review detail, administrator review-management controls, the public landing page with the purpose statement and three sample reviews, and **More Reviews** navigation that sends visitors to login.

Working areas: `Features/Reviews/ReviewReadDto.cs`, `ReviewSearchParams.cs`, `ReviewQueryService.cs`, `ReviewReadController.cs`, `ReviewAdministrationController.cs`, `Frontend/Pages/reviews/`, `Frontend/index.html`. Agree routes with Aygül so controllers do not declare duplicate endpoints.

Tests: anonymous users cannot read the full collection; filters return the correct records; pagination and empty results; published reviews appear; ordinary users cannot edit/delete; administrators can perform the agreed operations.

### Ozan — login, registration, instructor records and detail pages

Backend:

1. Implement **registration and login** with ASP.NET Core Identity; passwords are hashed by the framework.
2. Student profile fields: full name, email, department, year of study.
3. Supply the authenticated user ID and profile to the other modules.
4. Provide the shared **administrator authorization mechanism**. Public registration cannot grant administrator privileges.
5. Implement **Instructor** records with list/detail/create/update endpoints. Writes are administrator-only. Instructor records are separate from login accounts.
6. Use Aygül's course endpoints rather than creating another Course model.

Frontend (later): **Register** and **Login** pages, **Course detail** page, **Instructor list and detail** pages, shared header/navigation and login-state display, links from course/instructor details to Sami's filtered review page.

Working areas: `Features/Authentication/`, `Features/Users/`, `Features/Instructors/`, `Frontend/Pages/auth/`, `Frontend/Pages/courses/course-detail.html`, `Frontend/Pages/reviews/instructors.html`, `Frontend/Pages/reviews/instructor-detail.html`, `Frontend/js/shared/`. The page placeholders end in `.html.txt`; convert only the pages you start implementing.

Tests: valid and invalid registration/login; duplicate accounts; password hashes never returned; registration cannot create an administrator; current-user profile returns the correct department/year; instructor validation and administrator-only writes; course/instructor detail screens use the correct records.

Email verification, alumni documents, password recovery and social login are not part of this first assignment.

### Ümran

Ümran's work in this release is the database work above. Not responsible for APIs, authentication, EF configuration, migrations or deployment. Do not prepare real student passwords, identity documents or private student records as sample data.

## How the review workflow connects

| Step | Owner |
|---|---|
| Identify the logged-in student and their profile | Ozan |
| Supply course, period and assigned-instructor information | Aygül |
| Validate eligibility and save a new review | Aygül |
| Retrieve, filter and display published reviews | Sami |
| Edit/delete reviews as an administrator | Sami, using Ozan's authorization |
| Enforce relationships and uniqueness in the database | Aygül |
| Test database constraints and report queries | Ozan |
| Check that imported data is complete and consistent | Ümran |

## Collaboration rules

- One shared User, Course, Instructor and Review model. No duplicate models.
- Agree DTOs and routes before working in parallel.
- Coordinate edits to `Program.cs`, `ApplicationDbContext.cs` and migrations. Aygül sets the migration order.
- Each person tests and documents their own work. Aygül coordinates integration but does not finish other people's tasks.
- Build the backend contracts first; start the matching pages once they are stable.
- Keep deferred features (planner, prerequisite checking, alumni verification, email-domain verification, comparison, career filtering, multilingual interface) out of this first release.

First shared milestone: **a registered student can log in, submit one eligible course review, and another registered user can find and read it.**

## Git collaboration

The checkpoint and this plan have not been pushed to GitHub yet. Use separate feature branches and small pull requests. Never force-push a shared branch, commit secrets or include IDE/build artifacts (`.vs/`, `bin/`, `obj/`). Each teammate applies approved migrations only to their own database; pushing code does not update anyone's database.
