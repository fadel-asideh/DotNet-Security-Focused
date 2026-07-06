# Security Tasks

This project works through .NET/ASP.NET Core security concepts one task at a time. Each task hardens a specific attack surface and leaves behind working code + tests that demonstrate the fix. This file is the index — what each task covers, where the code lives, and its current status.

---

## Task 1: JWT Authentication with RBAC

**Status:** Done

### 🎯 Objective
Add authentication and role-based authorization to the API using JWT bearer tokens.

### 🛡 Security Focus
- Stateless authentication via signed JWTs (HMAC-SHA256)
- Role-based access control (RBAC) enforced per-endpoint
- Prevent user enumeration on login (same error for unknown user vs. wrong password)
- No exception details leaked to clients; errors logged server-side only

### 🛠 Implementation
- `Controllers/AuthController.cs` — `POST /auth/register`, `POST /auth/login`
- `Controllers/SecretsController.cs` — protected endpoints demonstrating `Admin`, `User`, `Manager`, OR-role, and AND-role authorization
- `Data/AppDBContext.cs`, `Data/DBSeeder.cs` — SQLite + ASP.NET Core Identity, roles seeded on startup
- Register runs in a DB transaction (validate roles → create user → assign roles → commit/rollback)
- JWT secrets stored in User Secrets (`Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience`)

### 📖 Outcome
21 passing integration tests (`DotNetSecurityFocused.Tests/Tests/AuthTests.cs`, `SecretsTests.cs`) covering registration, login, missing/expired tokens, and every role/endpoint combination.

---

## Task 2: Prevent SQL Injection in EF Core

**Status:** Done

### 🎯 Objective
Harden the data access layer against injection attacks by standardizing on parameterized queries.

### 🛡 Security Focus
Neutralize input-based injection vectors by strictly using parameterized queries instead of string concatenation in raw SQL.

### 🛠 Implementation
- `Product` entity/table dedicated to this exercise, seeded with sample rows plus one sensitive-looking row ("Admin Override Key") that a legitimate search should never surface
- `Services/ProductSearchService.cs`:
  - `SearchByNameVulnerableAsync` — deliberately unsafe `FromSqlRaw` with string interpolation, kept as a documented reference example, never wired to a controller route
  - `SearchByNameSafeAsync` — safe LINQ equivalent
  - `SearchByNameSafeRawAsync` — safe `FromSqlInterpolated` variant for cases where raw SQL is unavoidable
- `Controllers/ProductsController.cs` — `GET /products/search?name=...`, wired only to the safe method
- `DotNetSecurityFocused.Tests/Tests/SqlInjectionTests.cs`:
  - Proves the vulnerable method returns all rows (including "Admin Override Key") for the payload `x' OR '1'='1' -- ` — the trailing SQL comment neutralizes the query template's own closing `%'`, which a plain `' OR '1'='1` payload doesn't survive
  - Proves the safe method and the live HTTP endpoint both treat the same payload as literal text (no match)
  - Confirms a legitimate search term still works

### 📖 Outcome
All data access paths use parameterized queries. Injection payloads are handled as literal input and cannot alter query logic — verified by 4 new tests (25 total passing across the project).

---

## Task 3: Implement Input Validation & Sanitization

**Status:** Done

### 🎯 Objective
Add rigorous validation to all incoming API requests before they reach business logic.

### 🛡 Security Focus
Prevent malformed data and malicious payloads from propagating beyond the API boundary.

### 🛠 Implementation
- `Models/AuthModels.cs` — Data Annotations on `RegisterRequest`/`LoginRequest` (`[Required]`, `[EmailAddress]`, `[MinLength]`/`[MaxLength]`), auto-enforced pre-action by `[ApiController]`'s built-in model-state validation
- `RegisterRequest.ConfirmPassword` added specifically to exercise a cross-field rule
- `Validators/RegisterRequestValidator.cs` — FluentValidation rules for `ConfirmPassword == Password` and a non-empty `Roles` array
- `Controllers/AuthController.cs` — `_registerValidator.ValidateAsync(request)` called explicitly at the top of `Register`, before the existing role-existence checks (manual invocation, not a global filter)
- `DotNetSecurityFocused.Tests/Tests/ValidationTests.cs` — 5 new tests covering missing email, too-short password, mismatched confirm password, empty roles array, and a valid-request regression check

### 📖 Outcome
Invalid or malformed registration requests are rejected with 400 Bad Request before reaching role-assignment/user-creation logic. Data Annotation failures are caught automatically by the framework; FluentValidation catches the cross-field/collection rules that annotations can't express — verified by 5 new tests (30 total passing across the project).

---

## Task 4: Secure Secrets Management

**Status:** Done

### 🎯 Objective
Decouple sensitive credentials from the source code and version control.

### 🛡 Security Focus
Prevent accidental credential exposure in public repositories and commit history.

### 🛠 Implementation
- `.NET Secret Manager` (`dotnet user-secrets`) already configured for local development since Task 1 — `Jwt:SecretKey`, `Jwt:Issuer`, `Jwt:Audience` live outside the repo in `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`
- `appsettings.json` verified clean — only `Logging`/`AllowedHosts`, no keys or connection strings
- `appsettings.Development.json` added to `.gitignore` and untracked (`git rm --cached`) — it holds no secrets today, but can no longer be accidentally committed with any in the future
- `appsettings.Development.json.template` committed (mirrors the existing `secrets.template.json` pattern) so a fresh clone knows the file is expected

New clone setup: copy `appsettings.Development.json.template` → `appsettings.Development.json` (gitignored) for local logging overrides; use `dotnet user-secrets set` for actual credentials.

### Secrets by environment
- **Development**: `dotnet user-secrets set "Jwt:SecretKey" "..."` (see `secrets.template.json` for the required keys). ASP.NET Core only wires up the User Secrets configuration provider when `ASPNETCORE_ENVIRONMENT=Development`.
- **Any other environment**: set `Jwt__SecretKey`, `Jwt__Issuer`, `Jwt__Audience` as environment variables (double underscore `__` represents the `:` nesting separator, since most shells disallow `:` in variable names), or source them from a secrets vault (Azure Key Vault, AWS Secrets Manager, etc.) that populates environment variables at deploy time. Production never reads from `secrets.json`, even if one happened to exist on the machine.

### 📖 Outcome
No secrets are committed to the repository. Local development reads credentials from the Secret Manager, and production reads them from the environment or a secrets vault — verified end-to-end by running the app with `ASPNETCORE_ENVIRONMENT=Production` and `Jwt__SecretKey`/`Jwt__Issuer`/`Jwt__Audience` set as environment variables only (no `secrets.json` involved): register/login succeeded and issued a correctly-signed JWT. `dotnet test` remains green at 30/30.
