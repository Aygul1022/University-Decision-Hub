# Testing with accounts — step by step

How to create a student and an administrator in a local test environment and use them against the API. Use synthetic data only: no real names, emails or passwords.

The full contract is in `Authentication_Contract.md`.

## 0. Once per machine

1. The `UserAccounts` table must exist in your database (see "Migration status" in the contract — no migration has been created yet).
2. Give the backend a signing key so logins survive a restart:

   ```powershell
   cd Backend/UniversityDecisionHub.Api
   dotnet user-secrets set "Jwt:Key" "<any 32+ random characters>"
   ```

3. Start the backend with the `http` profile. The examples below use `http://localhost:5154`.

## 1. Create a student

```http
POST http://localhost:5154/api/auth/register
Content-Type: application/json

{
  "fullName": "Test Student",
  "email": "test.student@example.com",
  "password": "<choose a test password, 8+ characters>",
  "departmentId": 1,
  "studyYear": 3
}
```

- `201` returns `{ token, expiresAtUtc, user }`. The account is already logged in: `token` can be used straight away.
- `departmentId` and `studyYear` are stored on the account; review submission can read them from the database. To test an ineligible student, register another account with a different department or a lower year.
- There is no field that grants administrator access. Extra fields such as `isAdministrator` in the body are ignored.

The same can be done in the browser: open `Frontend/Pages/auth/register.html`.

## 2. Log in

```http
POST http://localhost:5154/api/auth/login
Content-Type: application/json

{
  "email": "test.student@example.com",
  "password": "<the test password>"
}
```

`200` returns a new `token`, valid for 120 minutes. Wrong email or password gives `401`.

## 3. Call a protected endpoint

Add the token to every request that needs a logged-in user:

```http
GET http://localhost:5154/api/auth/me
Authorization: Bearer <token>
```

`/api/auth/me` returns the stored profile (`userId`, `departmentId`, `studyYear`, `isAdministrator`) and is a quick way to check which account a token belongs to.

In Visual Studio's `.http` file, paste the token once into `@token = ...` and use `Authorization: Bearer {{token}}`.

## 4. Create an administrator

An administrator is an ordinary registered account that has been promoted on the machine running the backend.

1. Register the account as in step 1 (for example `test.admin@example.com`).
2. Stop the backend, then run:

   ```powershell
   dotnet run --project Backend/UniversityDecisionHub.Api -- make-admin test.admin@example.com
   ```

   It prints a confirmation, or an error if no account has that email.
3. Start the backend again and **log in again** as that account. Only a token issued after the promotion carries the `Administrator` role.

## 5. What each kind of request should return

| Request | Expected |
|---|---|
| Protected endpoint, no token | `401` |
| Protected endpoint, expired or altered token | `401` |
| Administrator-only endpoint, student token | `403` |
| Administrator-only endpoint, administrator token | success |
| More than 10 register/login attempts in a minute from one machine | `429` — wait a minute |

## 6. If something does not work

- **Every login is lost after restarting the backend** — `Jwt:Key` is not set (step 0.2).
- **`500` on register or login** — the `UserAccounts` table is missing or does not match the columns in the contract.
- **Administrator still gets `403`** — the token was issued before `make-admin`; log in again.
- **The browser pages cannot reach the backend** — they call `http://localhost:5154`; start the `http` profile, or set another address as described at the top of `Frontend/js/shared/api.js`.
