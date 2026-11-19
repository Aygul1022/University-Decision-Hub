-- Salt okunur. Anket hesap aktarimi YAPMAZ. Beklenen 5 satir ve 23 yanit.
WITH expected(department_key,programme_key,response_count) AS (VALUES
('DEP-0064','PRG-0006',10),
('DEP-0067','PRG-0025',9),
('DEP-0071','PRG-0091',1),
('DEP-0119','PRG-0093',2),
('DEP-0075','PRG-0032',1))
SELECT e.*, d."DepartmentId",p."ProgrammeId",CASE WHEN d."DepartmentId" IS NULL THEN 'DEPARTMENT_MISSING' WHEN p."ProgrammeId" IS NULL THEN 'PROGRAMME_MISSING' WHEN p."DepartmentId"<>d."DepartmentId" THEN 'WRONG_DEPARTMENT' ELSE 'OK' END mapping_status
FROM expected e LEFT JOIN "Departments" d ON d."DepartmentKey"=e.department_key LEFT JOIN "Programmes" p ON p."ProgrammeKey"=e.programme_key ORDER BY e.department_key;
