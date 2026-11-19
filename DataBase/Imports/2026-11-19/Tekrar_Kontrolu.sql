SELECT 'FacultyKey' AS kontrol, count(*) AS tekrar_grubu FROM
 (SELECT "ImportKey" FROM "Faculties" WHERE "ImportKey" IS NOT NULL GROUP BY "ImportKey" HAVING count(*)>1) t
UNION ALL SELECT 'DepartmentKey',count(*) FROM
 (SELECT "DepartmentKey" FROM "Departments" GROUP BY "DepartmentKey" HAVING count(*)>1) t
UNION ALL SELECT 'InstructorKey',count(*) FROM
 (SELECT "ImportKey" FROM "Instructors" WHERE "ImportKey" IS NOT NULL GROUP BY "ImportKey" HAVING count(*)>1) t
UNION ALL SELECT 'ProgrammeKey',count(*) FROM
 (SELECT "ProgrammeKey" FROM "Programmes" GROUP BY "ProgrammeKey" HAVING count(*)>1) t
UNION ALL SELECT 'CourseKey',count(*) FROM
 (SELECT "CourseKey" FROM "Courses" GROUP BY "CourseKey" HAVING count(*)>1) t
UNION ALL SELECT 'CurriculumKey',count(*) FROM
 (SELECT "CurriculumKey" FROM "ProgrammeCourses" GROUP BY "CurriculumKey" HAVING count(*)>1) t
UNION ALL SELECT 'Ayni bolum ve ders kodu',count(*) FROM
 (SELECT "DepartmentId",upper(trim("CourseCode")) FROM "Courses" GROUP BY "DepartmentId",upper(trim("CourseCode")) HAVING count(*)>1) t
UNION ALL SELECT 'Ayni program,ders,yil,yariyil',count(*) FROM
 (SELECT "ProgrammeId","CourseId","CatalogueYear","Semester" FROM "ProgrammeCourses"
  GROUP BY "ProgrammeId","CourseId","CatalogueYear","Semester" HAVING count(*)>1) t
UNION ALL SELECT 'Ayni bolum ve hoca adi',count(*) FROM
 (SELECT "DepartmentId",upper(trim("FullName")) FROM "Instructors"
  GROUP BY "DepartmentId",upper(trim("FullName")) HAVING count(*)>1) t;
