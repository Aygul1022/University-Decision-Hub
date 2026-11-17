# UniversityDecisionHub — catalogue preparation package

Prepared for Aygül — 15 November 2026

## Main working files

- `working_faculty.xlsx` — 9 academic upper units.
- `working_department_v2.xlsx` — 129 import-candidate Department records.
- `working_programme_v2.xlsx` — 92 Programme records, one row per degree/language combination.
- `working_academic_staff_v2.xlsx` — 449 Instructor records with approved Department keys.
- `course_staging_v2.xlsx` — 5,442 raw course rows preserved for later Course / ProgrammeCourse preparation.
- `course_programme_mapping_register_v2.xlsx` — 58 course-programme source groups: 56 single candidates and 2 pending groups.

## Supporting evidence and pending-record files

- `department_decisions_v2.xlsx` — 3 confirmed key mappings, 2 confirmed upper-unit corrections, 2 pending Department records.
- `programme_decisions_v2.xlsx` — Programme key register and 3 pending programme records.
- `pending_instructor_records_v2.xlsx` — 3 instructors whose linked Department remains pending.
- `official_programme_evidence_v2.xlsx` — public Bologna evidence for the EEM and Business programme records reviewed so far.

## Import boundaries

- No Course or ProgrammeCourse import file is included yet.
- Every course row is retained in staging; no responsible Department key was inferred from course code or curriculum membership.
- The 317 rows for Electric-Electronic Engineering and Business remain pending for programme identity confirmation.
- Existing source keys were retained and were not renumbered.
