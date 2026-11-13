-- University Decision Hub: report queries (PostgreSQL)
--
-- Read-only SELECT statements for the project report. Table and column names are the ones
-- EF Core creates, so they are written in double quotes. The literal values ('CENG301', 1,
-- '2025-2026') are examples: replace them with the course, instructor, offering or year you want.
--
-- Tests/UniversityDecisionHub.DatabaseChecks runs every statement in this file against a small
-- synthetic data set and checks the number of rows each one returns.

-- Q1. Courses by department
SELECT d."DepartmentCode", d."DepartmentName", c."CourseCode", c."CourseName",
       c."RecommendedStudyYear", c."CourseCredit", c."Ects"
FROM "Departments" d
JOIN "Courses" c ON c."DepartmentId" = d."DepartmentId"
ORDER BY d."DepartmentName", c."CourseCode";

-- Q2. Number of courses in each department (departments without courses are listed with 0)
SELECT d."DepartmentCode", d."DepartmentName", COUNT(c."CourseId") AS "CourseCount"
FROM "Departments" d
LEFT JOIN "Courses" c ON c."DepartmentId" = d."DepartmentId"
GROUP BY d."DepartmentId", d."DepartmentCode", d."DepartmentName"
ORDER BY d."DepartmentName";

-- Q3. Reviews of one course, newest first
SELECT r."ReviewId", c."CourseCode", o."AcademicYear", o."Semester",
       i."FullName" AS "InstructorName", r."UserId", r."ReviewText", r."CreatedAt"
FROM "Reviews" r
JOIN "Courses" c ON c."CourseId" = r."CourseId"
JOIN "CourseOfferings" o ON o."OfferingId" = r."OfferingId"
JOIN "Instructors" i ON i."InstructorId" = r."InstructorId"
WHERE c."CourseCode" = 'CENG301'
ORDER BY r."CreatedAt" DESC, r."ReviewId" DESC;

-- Q4. Reviews of the courses taught by one instructor
SELECT r."ReviewId", i."FullName" AS "InstructorName", c."CourseCode", c."CourseName",
       o."AcademicYear", o."Semester", r."ReviewText", r."CreatedAt"
FROM "Reviews" r
JOIN "Instructors" i ON i."InstructorId" = r."InstructorId"
JOIN "Courses" c ON c."CourseId" = r."CourseId"
JOIN "CourseOfferings" o ON o."OfferingId" = r."OfferingId"
WHERE i."InstructorId" = 1
ORDER BY o."AcademicYear" DESC, c."CourseCode", r."ReviewId";

-- Q5. Number of reviews in each academic year
SELECT o."AcademicYear", COUNT(*) AS "ReviewCount"
FROM "Reviews" r
JOIN "CourseOfferings" o ON o."OfferingId" = r."OfferingId"
GROUP BY o."AcademicYear"
ORDER BY o."AcademicYear" DESC;

-- Q6. Reviews written for one academic year
SELECT r."ReviewId", o."AcademicYear", o."Semester", c."CourseCode",
       i."FullName" AS "InstructorName", r."ReviewText"
FROM "Reviews" r
JOIN "CourseOfferings" o ON o."OfferingId" = r."OfferingId"
JOIN "Courses" c ON c."CourseId" = r."CourseId"
JOIN "Instructors" i ON i."InstructorId" = r."InstructorId"
WHERE o."AcademicYear" = '2025-2026'
ORDER BY c."CourseCode", r."ReviewId";

-- Q7. Instructors who teach a given offering
SELECT o."OfferingId", c."CourseCode", o."AcademicYear", o."Semester",
       i."InstructorId", i."AcademicTitle", i."FullName"
FROM "OfferingInstructors" a
JOIN "CourseOfferings" o ON o."OfferingId" = a."OfferingId"
JOIN "Courses" c ON c."CourseId" = o."CourseId"
JOIN "Instructors" i ON i."InstructorId" = a."InstructorId"
WHERE a."OfferingId" = 1
ORDER BY i."FullName", i."InstructorId";

-- Q8. Combined search filters (the filters of the Read Reviews page)
-- Each filter is optional: write NULL inside its CAST to switch that filter off.
WITH filters AS (
    SELECT CAST('CENG301' AS text)    AS course_code,
           CAST(NULL AS integer)      AS department_id,
           CAST(1 AS integer)         AS instructor_id,
           CAST('2025-2026' AS text)  AS academic_year
)
SELECT r."ReviewId", c."CourseCode", d."DepartmentName", o."AcademicYear", o."Semester",
       i."FullName" AS "InstructorName", r."ReviewText", r."CreatedAt"
FROM "Reviews" r
JOIN "Courses" c ON c."CourseId" = r."CourseId"
JOIN "Departments" d ON d."DepartmentId" = c."DepartmentId"
JOIN "CourseOfferings" o ON o."OfferingId" = r."OfferingId"
JOIN "Instructors" i ON i."InstructorId" = r."InstructorId"
CROSS JOIN filters f
WHERE (f.course_code IS NULL OR LOWER(c."CourseCode") = LOWER(f.course_code))
  AND (f.department_id IS NULL OR c."DepartmentId" = f.department_id)
  AND (f.instructor_id IS NULL OR r."InstructorId" = f.instructor_id)
  AND (f.academic_year IS NULL OR o."AcademicYear" = f.academic_year)
ORDER BY r."CreatedAt" DESC, r."ReviewId" DESC;

-- Q9. Integrity check: users with more than one review for the same course
-- The unique index IX_Reviews_UserId_CourseId forbids this, so the result must be empty.
SELECT r."UserId", r."CourseId", COUNT(*) AS "ReviewCount"
FROM "Reviews" r
GROUP BY r."UserId", r."CourseId"
HAVING COUNT(*) > 1;
