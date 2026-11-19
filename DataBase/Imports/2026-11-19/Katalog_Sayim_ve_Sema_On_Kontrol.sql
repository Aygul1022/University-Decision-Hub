-- Yalnizca okur. Kullanici eklemez, migration veya katalog aktarimi yapmaz.
BEGIN TRANSACTION READ ONLY;
SELECT current_database() AS database_name, inet_server_port() AS port;

WITH actual AS (
 SELECT 'Faculties' AS table_name,count(*) AS actual_count FROM "Faculties"
 UNION ALL SELECT 'Departments',count(*) FROM "Departments"
 UNION ALL SELECT 'Instructors',count(*) FROM "Instructors"
 UNION ALL SELECT 'Programmes',count(*) FROM "Programmes"
 UNION ALL SELECT 'Courses',count(*) FROM "Courses"
 UNION ALL SELECT 'ProgrammeCourses',count(*) FROM "ProgrammeCourses"
), expected(table_name,expected_count) AS (VALUES
 ('Faculties',9),('Departments',135),('Instructors',456),('Programmes',104),('Courses',9346),('ProgrammeCourses',12591)
)
SELECT a.*,e.expected_count,CASE WHEN a.actual_count=e.expected_count THEN 'COUNTS_MATCH' ELSE 'REVIEW_REQUIRED' END AS status
FROM actual a JOIN expected e USING(table_name) ORDER BY a.table_name;

SELECT column_name AS expected_review_column,
 EXISTS(SELECT 1 FROM information_schema.columns c WHERE c.table_schema='public' AND c.table_name='Reviews' AND c.column_name=expected.column_name) AS exists_in_target
FROM (VALUES ('IsHidden'),('ModeratedAt'),('ModeratedByUserId')) expected(column_name);

SELECT p."ProgrammeKey",d."DepartmentKey",p."ProgrammeName",p."DegreeLevel",p."InstructionLanguage"
FROM "Programmes" p JOIN "Departments" d USING("DepartmentId")
WHERE p."ProgrammeKey" IN ('PRG-0076','PRG-DOU-BOLOGNA-4141','PRG-DOU-BOLOGNA-4142');
COMMIT;
