# Testing with accounts — step by step

How to create a student and an administrator in a local test environment and use them against the API. Use synthetic data for shared testing: no real names, emails or passwords in the repository, in chat or in a shared database.

The full contract is in `Authentication_Contract.md`.

## 0. Once per machine

1. The `UserAccounts` table must exist in your database (see "Migration status" in the contract — no migration has been created yet).
2. Give the backend a signing key so logins survive a restart:

   ```powershell
   cd Backend/UniversityDecisionHub.Api
   dotnet user-secrets set "Jwt:Key" "<any 32+ random characters>"
   ```

3. The examples below use the `http` profile, `http://localhost:5154`.

## 1. Put people on the list

Nobody can register unless an account was defined for them first. With the backend stopped:

```powershell
dotnet run --project Backend/UniversityDecisionHub.Api -- import-users "Doc\\users_import_template.csv"
```

That loads the two synthetic students from the template. For your own test people, copy the template somewhere **outside the repository** and edit it. The command prints how many rows were added, skipped (email already present) and rejected (with line numbers).

An administrator can also add one person at a time — see step 5.

## 2. Complete registration as a student

```http
POST http://localhost:5154/api/auth/register
Content-Type: application/json

{
  "fullName": "Test Student One",
  "email": "test.student.one@example.com",
  "departmentId": 1,
  "studyYear": 3,
  "password": "<choose a test password, 8+ characters>"
}
```

- Name, email, department and year must match the imported row. The name may differ in letter case, spacing and Turkish diacritics.
- `201` returns `{ token, expiresAtUtc, user }`; the account is logged in straight away.
- `404` — the details do not match anyone on the list (the answer is the same whichever field is wrong).
- `409` — this account already completed registration; log in instead.

In the browser: `Frontend/Pages/auth/register.html`.

## 3. Log in and call a protected endpoint

```http
POST http://localhost:5154/api/auth/login
Content-Type: application/json

{
  "email": "test.student.one@example.com",
  "password": "<the test password>"
}
```

`200` returns a `token`, valid for 120 minutes. Add it to every request that needs a logged-in user:

```http
GET http://localhost:5154/api/auth/me
Authorization: Bearer <token>
```

`/api/auth/me` returns the stored profile (`userId`, `departmentId`, `studyYear`, `isAdministrator`). In Visual Studio's `.http` file, paste the token once into `@token = ...` and use `Authorization: Bearer {{token}}`.

To test an ineligible student, change the profile and submit again:

```http
PUT http://localhost:5154/api/auth/me
Content-Type: application/json
Authorization: Bearer <token>

{
  "fullName": "Test Student One",
  "departmentId": 2,
  "studyYear": 1
}
```

## 4. Create an administrator

1. Complete registration for the account as in step 2 (for example Test Student Two).
2. Stop the backend, then run:

   ```powershell
   dotnet run --project Backend/UniversityDecisionHub.Api -- make-admin test.student.two@example.com
   ```

3. Start the backend again and **log in again** as that account. Only a token issued after the promotion carries the `Administrator` role.

## 5. Manage users as an administrator

```http
### Everyone on the list, with hasCompletedRegistration
GET http://localhost:5154/api/admin/users
Authorization: Bearer <administrator token>

### Add one person (no password — they complete registration themselves)
POST http://localhost:5154/api/admin/users
Content-Type: application/json
Authorization: Bearer <administrator token>

{
  "fullName": "Test Student Three",
  "email": "test.student.three@example.com",
  "departmentId": 1,
  "studyYear": 2
}

### Correct someone's details
PUT http://localhost:5154/api/admin/users/3
Content-Type: application/json
Authorization: Bearer <administrator token>

{
  "fullName": "Test Student Three",
  "email": "test.student.three@example.com",
  "departmentId": 1,
  "studyYear": 3
}
```

## 6. What each kind of request should return

| Request | Expected |
|---|---|
| Protected endpoint, no token | `401` |
| Protected endpoint, expired or altered token | `401` |
| Login for an account that has not completed registration | `401` |
| Administrator-only endpoint, student token | `403` |
| Administrator-only endpoint, administrator token | success |
| More than 10 register/login attempts in a minute from one machine | `429` — wait a minute |

## 7. If something does not work

- **Register answers `404` for someone who should be on the list** — compare the request with the imported row: email, department number and year must be identical. `GET /api/admin/users` shows the stored values.
- **Every login is lost after restarting the backend** — `Jwt:Key` is not set (step 0.2).
- **`500` on register or login** — the `UserAccounts` table is missing or does not match the columns in the contract.
- **Administrator still gets `403`** — the token was issued before `make-admin`; log in again.
- **The browser pages cannot reach the backend** — they call `http://localhost:5154`; start the `http` profile, or set another address as described at the top of `Frontend/js/shared/api.js`.
