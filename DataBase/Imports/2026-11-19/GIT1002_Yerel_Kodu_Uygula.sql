-- User-approved temporary local alias; official source code remains GİT 1002.
-- Execution evidence is tracked separately in Doc/Verification/git1002-local-aliases-2026-11-19/.
-- Back up and confirm the intended target first on every other database.
-- For a target containing the existing course: run this before import-catalog.
-- For a fresh target without this course: import the updated catalogue directly.
-- No source keys, IDs, review links or curriculum links are changed here.
BEGIN;
LOCK TABLE public."Courses" IN SHARE ROW EXCLUSIVE MODE;
DO $alias$
DECLARE
  course_id integer;
  department_id integer;
  current_code text;
BEGIN
  SELECT c."CourseId", c."DepartmentId", c."CourseCode"
  INTO course_id, department_id, current_code
  FROM public."Courses" c
  JOIN public."Departments" d ON d."DepartmentId" = c."DepartmentId"
  WHERE c."CourseKey" = 'CRS-DOU-B9423D9526C039D81F082152'
    AND c."CourseName" = 'Yaratıcı Fotoğraf'
    AND d."DepartmentKey" = 'DEP-0076';
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Expected source course not found; verify target and identity before applying.';
  END IF;
  IF current_code NOT IN ('GİT 1002', 'GİT 1002-0') OR current_code IS NULL THEN
    RAISE EXCEPTION 'Unexpected existing course code; no changes applied.';
  END IF;
  IF EXISTS (SELECT 1 FROM public."Courses"
             WHERE "DepartmentId" = department_id AND "CourseCode" = 'GİT 1002-0'
               AND "CourseId" <> course_id) THEN
    RAISE EXCEPTION 'Local alias already belongs to another course; no changes applied.';
  END IF;
  UPDATE public."Courses" SET "CourseCode" = 'GİT 1002-0'
  WHERE "CourseId" = course_id AND "CourseCode" = 'GİT 1002';
END
$alias$;
COMMIT;
