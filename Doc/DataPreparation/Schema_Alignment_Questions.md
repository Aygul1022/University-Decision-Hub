# Schema Alignment Questions

**Prepared by:** Ümran  
**Purpose:** Record differences between the current code, data dictionary, and approved requirements before preparing import files.

## Confirmed items

- PostgreSQL and pgAdmin will be used.
- CourseCode is the full official course code, such as `COME 327`.
- OfferingInstructor is required to represent which instructor taught a specific course offering.
- Review must reference an instructor assigned to the selected offering.
- Real survey data and passwords must not be committed to GitHub.

## Decisions required from Aygül and Ozan

1. Should the final Course field be named `Credits` or `CourseCredit`?
2. Is `CourseDescription` part of the final Course table, or should it be removed?
3. Is `IsActive` retained in Course, or is course availability represented only by `CourseOffering.Status`?
4. Are CoursePrerequisite records deferred completely from the first release?
5. When will Faculty, Department, UserAccount, Instructor, CourseOffering, OfferingInstructor, and Review models be added to the shared codebase?
6. Which migration will create the approved final schema?
7. What is the final secure activation/password flow for pre-created user accounts?

## Data preparation rule

No official catalogue data, real student data, or CSV import file will be finalized until the final field names and migration structure are confirmed.

## Approved policy decisions 

### Pre-created users

- Some approved users from the controlled project dataset may be created in the system before they use the application.
- Real user data must remain private and must not be committed to GitHub.
- Plaintext passwords must never be stored in CSV files, source code, GitHub, reports, or database records.
- The exact first-login and password-creation method for pre-created accounts is still an open technical decision. Ozan will propose the secure implementation, and Aygül will coordinate the related database migration.

### Administrator access

- Authorized project-team accounts may receive the `Administrator` role through a controlled process.
- Public registration must never allow a user to assign themselves the `Administrator` role.
- Administrators may manage approved pre-created user accounts.
- Sensitive fields such as PasswordHash and other authentication secrets must never be returned by APIs or included in data files.

### Review moderation

- Administrators may review, edit, or delete reviews containing profanity, insults, or inappropriate content.
- Sami owns review moderation behavior; Ozan owns authentication and administrator authorization; Aygül owns the final database fields and migration coordination.
- The final database design must document how moderation actions are recorded.