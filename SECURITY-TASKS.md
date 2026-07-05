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

**Status:** Planned

### 🎯 Objective
Harden the data access layer against injection attacks by standardizing on parameterized queries.

### 🛡 Security Focus
Neutralize input-based injection vectors by strictly using parameterized queries instead of string concatenation in raw SQL.

### 🛠 Implementation Plan
- Add a `Product` entity/table dedicated to this exercise
- `Services/ProductSearchService.cs`:
  - `SearchByNameVulnerable` — deliberately unsafe `FromSqlRaw` with string interpolation, kept as a documented reference example, never wired to a controller route
  - `SearchByNameSafe` — safe LINQ equivalent (and a `FromSqlInterpolated` variant for cases where raw SQL is unavoidable)
- `Controllers/ProductsController.cs` — exposes only the safe search endpoint
- `DotNetSecurityFocused.Tests/Tests/SqlInjectionTests.cs`:
  - Proves the vulnerable method returns all rows for a payload like `' OR '1'='1`
  - Proves the safe method/endpoint treats the same payload as literal text (no match)
  - Confirms a legitimate search term still works

### 📖 Expected Outcome
All data access paths use parameterized queries. Injection payloads are handled as literal input and cannot alter query logic.
