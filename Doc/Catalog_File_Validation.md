# Catalog file validation

> Current status (14 November 2026): this earlier checkpoint is superseded by [Catalogue version 2 and review integration](Catalog_V2_Contract.md). The new code now supports stable department keys, nullable codes, programmes/curricula and declaration-based reviews. Migrations are NOT applied; user-reported 79 model checks and 134 SQLite/HTTP checks passed on 15 November 2026. Earlier text below is retained as history.


This first data-loading step checks a faculty, department and instructor file before any import is attempted. It does not connect to PostgreSQL, create accounts, insert records or change migrations. Courses, offerings and assignments are outside this first file format.

## Why file references are different from database IDs

Each teammate has a local database. The same department may receive a different DepartmentId on each computer. A shared data file must therefore not depend on locally generated IDs.

The sample uses FacultyKey to identify a faculty within the file, DepartmentCode to identify a department, and InstructorKey to distinguish instructor records. The importer now persists FacultyKey and InstructorKey as normalized ImportKey values to recognize future imports. They are source identifiers, not local database IDs. For example, a department with FacultyKey FAC-DEMO refers to the faculty with that key. An instructor with DepartmentCode DEMO refers to the department with that code.

Use stable school identifiers where available, or agreed non-sensitive source references. Never guess a missing identifier. Two different instructors can have the same name; a name alone is not a reliable unique identifier. Repeated InstructorKey values are rejected even when assigned to different departments because an instructor belongs to exactly one department in the approved design.

The validator compares references ignoring surrounding whitespace and letter case. It does not rewrite the source file. Import keys have a maximum length of 100 characters and are saved trimmed and uppercased. The importer maps source references to local IDs; see Catalog_Import.md for its rules and migration prerequisite.

## File format

Copy `Doc/Templates/catalog.example.json` to a private working location and replace its synthetic values. The original example contains no school data. Save as UTF-8 JSON without a byte-order mark.

The file contains three nonempty lists:

| List | Fields |
|---|---|
| faculties | facultyKey, facultyName |
| departments | departmentCode, departmentName, facultyKey |
| instructors | instructorKey, fullName, academicTitle, departmentCode |

academicTitle is optional: omit it, use null or leave it blank. The other fields are required. Property names are case-insensitive; misspelled or unknown properties and duplicate JSON properties are rejected. Null records and wrong JSON types are rejected.

Name and length rules reuse the existing catalog DTO definitions: faculty and department names up to 200 characters, department code up to 30, instructor name up to 150 and academic title up to 50. Required text cannot consist only of whitespace.

Every department must reference a faculty in the same file; every instructor must reference a department in the same file. Every listed faculty needs at least one department and every listed department needs at least one instructor. This is a self-contained batch, not a partial file that relies on records already in a database. Duplicate faculty or instructor names are not automatically rejected because names are not the chosen keys.

## Run it

From the repository root:

```powershell
$catalogPath = (Resolve-Path "Doc/Templates/catalog.example.json").Path
dotnet run --project Backend/UniversityDecisionHub.Api --no-launch-profile -- validate-catalog "$catalogPath"
```

Expected sample output:

```text
Validation passed: 1 faculties, 1 departments, 1 instructors. No database connection or writes.
```

Resolve-Path converts the file location to an absolute path before dotnet launches the application. This avoids resolving Doc relative to the API project directory. To check a private file, replace the Resolve-Path argument with its full path. Keep actual personal or school files outside Git until sharing permission is established. The validator does not print names or complete source records in its error reports.

## Read errors and fix the source

`Departments[0].FacultyKey` means the faculty reference in the first department record. Array positions start at zero. Semantic errors identify list positions and fields; invalid JSON reports a one-based line number and byte position. Correct the file and run the command again.

Exit code 0 means validation passed. Exit code 1 means the command usage or file access failed. Exit code 2 means file content was rejected. In PowerShell, inspect `$LASTEXITCODE` immediately after the command.

Useful manual exercises on a copy of the example:

1. Change a department's facultyKey to MISSING: expect rejection for an unresolved reference.
2. Copy an instructor without changing instructorKey: expect duplicate-reference rejection.
3. Remove all instructors: expect an empty-list error and a department-without-instructor error.
4. Correct the file: expect a successful report, but no new pgAdmin rows.

## How the code is organized

`Features/CatalogImport/CatalogFile.cs` describes the JSON shape. These classes represent input files, not EF entities.

`CatalogFileValidator.cs` checks values and relationships in memory. HashSet collections track keys efficiently and detect duplicates. DataAnnotations validate names and lengths through the existing FacultyInput, DepartmentInput and InstructorInput DTOs.

`CatalogValidationCommand.cs` reads the file, catches input errors, prints the report and returns an exit code. Program.cs dispatches this command before WebApplication.CreateBuilder, so it does not require a connection string, JWT key or a running database. The normal API startup and existing user-import command remain separate.

## What a successful result does not prove

Validation cannot prove that source information is accurate, that the school approved it, or that it matches existing local records. Umran still reviews sources and data quality. Database import is a separate explicit command described in Catalog_Import.md; validate-catalog itself still performs no database operations.
