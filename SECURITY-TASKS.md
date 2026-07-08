# Security Tasks

This repo is a hands-on tour of .NET/ASP.NET Core security engineering — one real attack surface at a time. Each task takes on a specific vulnerability class, grounded in the OWASP Top 10 and beyond, hardens it in working code, and proves the fix holds with tests — not just a description of what "should" be secure.

This file is the index: what each task covers, which OWASP category it maps to, where the implementation lives, and its current status (Done / Planned).

---

## Task 1: JWT Authentication with RBAC

**Status:** Done

### 🎯 Objective
Add authentication and role-based authorization to the API using JWT bearer tokens.

### 🛡 Security Focus
Covers OWASP A07:2021 – Identification and Authentication Failures, and A01:2021 – Broken Access Control (via RBAC).
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
Covers OWASP A03:2021 – Injection. Neutralize input-based injection vectors by strictly using parameterized queries instead of string concatenation in raw SQL.

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
Supports OWASP A03:2021 – Injection (a common defense-in-depth control, alongside Task 2's parameterized queries) and A04:2021 – Insecure Design (rejecting malformed input by design rather than downstream). Prevent malformed data and malicious payloads from propagating beyond the API boundary.

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
Covers OWASP A05:2021 – Security Misconfiguration (hardcoded credentials/secrets in source or config is a listed failure mode of this category). Prevent accidental credential exposure in public repositories and commit history.

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

---

## Task 5: Implement API Rate Limiting

**Status:** Done

### 🎯 Objective
Protect the API from abuse and resource exhaustion by throttling excessive requests.

### 🛡 Security Focus
Covers OWASP A07:2021 – Identification and Authentication Failures (brute-force login attempts, explicitly listed under this category) and OWASP API Security Top 10 API4:2023 – Unrestricted Resource Consumption. Mitigate Denial of Service (DoS) and brute-force attempts by capping request frequency per client.

### 🛠 Implementation
- `Extensions/RateLimitingServiceExtensions.cs` — `AddAppRateLimiting()` registers a named `"ip-sliding"` policy: `SlidingWindowRateLimiterOptions` partitioned by client IP (`PermitLimit = 20`, `Window = 10s`, `SegmentsPerWindow = 4`), with `OnRejected` returning 429
- `Program.cs` — `AddAppRateLimiting()` registered as a service, `app.UseRateLimiter()` added to the pipeline before `MapControllers()`
- `Controllers/AuthController.cs` — `[EnableRateLimiting("ip-sliding")]` applied only to `Login` (not `Register`, not the whole controller), scoping the blast radius to the brute-force scenario without affecting other endpoints or other tests' shared `AuthHelper` usage
- `Program.cs`/DI setup refactored into `Extensions/AuthenticationServiceExtensions.cs` and `Extensions/RateLimitingServiceExtensions.cs` as part of this task, keeping the composition root thin
- `DotNetSecurityFocused.Tests/Tests/RateLimitTests.cs` — loops up to 25 failed login attempts against a nonexistent user/wrong password and asserts a 429 once the limit is exceeded; isolated to its own `ApiFactory`/limiter state so it can't affect or be affected by other test classes

### 📖 Outcome
Clients exceeding 20 login attempts per 10-second window receive 429 Too Many Requests; normal traffic (including the existing 30 tests, none of which approach that threshold on `/auth/login`) is unaffected — verified by `dotnet test` at 31/31 passing.

---

## Task 6: Resolve NuGet Vulnerability Warnings

**Status:** Done

### 🎯 Objective
Eliminate known-vulnerable dependencies flagged by NuGet's built-in security audit (`NU1903`) during restore/build.

### 🛡 Security Focus
Covers OWASP A06:2021 – Vulnerable and Outdated Components. Unpatched dependencies are a supply-chain risk even when the vulnerable code path is never directly exercised by this project — treat build-time vulnerability warnings as defects to fix, not noise to suppress.

### 🛠 Implementation
Two advisories surfaced on `dotnet build`/`dotnet restore`:

1. **`Microsoft.OpenApi` — GHSA-v5pm-xwqc-g5wc (CVE-2026-49451, high, CVSS 7.5)**
   Circular `$ref` schemas in an OpenAPI document can crash the process parsing it (stack overflow / DoS). Pulled in transitively by `Microsoft.AspNetCore.OpenApi 10.0.9`'s `AddOpenApi()`/`MapOpenApi()`, which this project had wired up from scaffolding but never actually used (no Swagger UI, no `/openapi` consumers).
   There is no patched `Microsoft.AspNetCore.OpenApi 10.0.x` yet that bumps its `Microsoft.OpenApi` dependency past the vulnerable range — a version bump isn't available today.
   **Fix:** removed `AddOpenApi()`/`MapOpenApi()` from `Program.cs` and dropped the `Microsoft.AspNetCore.OpenApi` package reference entirely. Since the feature was unused, deleting it is the correct fix, not a workaround — if OpenAPI docs are needed later, re-add the package then and re-check for a patched version at that time.

2. **`SQLitePCLRaw.lib.e_sqlite3` — GHSA-2m69-gcr7-jv3q (CVE-2025-6965, high, CVSS 9.8)**
   Bundled SQLite version has a memory-corruption bug (aggregate terms exceeding available columns). Pulled in transitively by `Microsoft.EntityFrameworkCore.Sqlite 10.0.9`, which has no newer `10.0.x` release with a patched bundle.
   **Fix:** added a direct `PackageReference` to `SQLitePCLRaw.bundle_e_sqlite3` version `3.0.3` in `DotNetSecurityFocused.csproj`. NuGet's "nearest wins" resolution means an explicit direct reference overrides the version requested by a transitive dependency, without needing to touch the EF Core package itself. The override only needed to be added once — `DotNetSecurityFocused.Tests.csproj` inherits it through its `ProjectReference` to the main project, since restore resolves the whole dependency graph together.

### 📖 Outcome
`dotnet build` reports 0 warnings (previously 2 `NU1903` advisories). `dotnet test` confirms no regression from the SQLite native-interop major-version bump — 31/31 passing.

---

## Task 7: Authorization Beyond Roles (IDOR)

**Status:** Done

### 🎯 Objective
Ensure endpoints that return or modify a specific resource verify the caller owns/may access *that* resource, not just that they hold the right role.

### 🛡 Security Focus
Prevent Insecure Direct Object Reference (IDOR / broken object-level authorization — OWASP A01) — role checks alone don't stop User A from reading or modifying User B's data by guessing or incrementing an ID.

### 🛠 Implementation
- `Models/Order.cs` — first user-owned resource in the API (`UserId`, `ProductName`, `Quantity`, `TotalPrice`)
- `Models/OrderModels.cs` — `CreateOrderRequest` DTO deliberately has no `UserId` field, so ownership can never be set by the client; the server always derives it from the authenticated caller's JWT
- `Services/OrderService.cs`:
  - `GetOrderByIdVulnerableAsync` — deliberately unsafe, returns any order by ID with no ownership check, kept as a documented reference example, never wired to a controller route
  - `GetOrderByIdSafeAsync` — enforces `order.UserId == requestingUserId` unless the caller is Admin; returns `null` (not found) rather than leaking that the ID exists
- `Controllers/OrdersController.cs` — `POST /orders`, `GET /orders/{id}`, wired only to the safe method
- `DotNetSecurityFocused.Tests/Tests/OrderAuthorizationTests.cs`:
  - Proves the vulnerable service method leaks another user's order when called directly
  - Proves the live `GET /orders/{id}` endpoint returns 404 for a different authenticated user's order
  - Confirms the legitimate owner can still fetch their own order

### 📖 Outcome
Resource-scoped endpoints reject cross-user access attempts even when the caller has a valid token and the "right" role — verified by 3 new tests (34 total passing across the project).

---

## Task 8: Security Logging & Monitoring

**Status:** Done

### 🎯 Objective
Add an audit trail for security-relevant events so suspicious activity is visible after the fact, not just blocked in the moment.

### 🛡 Security Focus
Close OWASP A09 (Security Logging & Monitoring Failures) — today, failed logins, rate-limit rejections, and role/permission changes leave no queryable trace.

### 🛠 Implementation
- `Services/SecurityEventLogger.cs` — `ISecurityEventLogger` deliberately exposes only narrow, named events (`LogLoginFailed`, `LogRateLimitRejected`, `LogRoleAssigned`, `LogAuthorizationFailure`), each logged as structured `ILogger` fields (not a free-text string), so there's no method shape that could accept a password or full token
- `Controllers/AuthController.cs` — `Login` logs a failed-login event on both the "user not found" and "wrong password" branches (same event either way, preserving the existing anti-enumeration behavior from Task 1); `Register` logs a role-assigned event per role granted
- `Extensions/RateLimitingServiceExtensions.cs` — the existing `OnRejected` callback resolves `ISecurityEventLogger` from `HttpContext.RequestServices` and logs the rejection alongside the 429 response
- `Authorization/LoggingAuthorizationMiddlewareResultHandler.cs` — decorates the framework's default `IAuthorizationMiddlewareResultHandler`; logs only when `authorizeResult.Forbidden` (a real 403 — authenticated but insufficient role), deliberately excluding `Challenged` (401 — missing/invalid token) to avoid noise from anonymous or expired-token requests; registered in `Program.cs` after `AddAppAuthentication`, since DI resolves the last-registered implementation for the interface
- `DotNetSecurityFocused.Tests/Fixtures/ListLoggerProvider.cs` — a minimal in-memory `ILoggerProvider` capturing formatted log messages, wired into `ApiFactory` for the test host
- `DotNetSecurityFocused.Tests/Tests/SecurityLoggingTests.cs` — one test per event type (login failed, role assigned, rate limit rejected, authorization failure), plus an explicit assertion that a failed-login log entry never contains the submitted password

### 📖 Outcome
Failed logins, rate-limit rejections, role assignments, and 403 authorization failures are all captured as structured, queryable log events with no sensitive data leakage — verified by 4 new tests (38 total passing across the project).

---

## Task 9: Cryptographic Choices Review

**Status:** Done

### 🎯 Objective
Explicitly verify and document the cryptographic choices already in use (password hashing, JWT signing) rather than relying on framework defaults going unexamined.

### 🛡 Security Focus
Close OWASP A02 (Cryptographic Failures) — using a secure default by accident isn't the same as knowing why it's secure and what would break that guarantee.

### 🛠 Implementation
- **Password hashing** — confirmed no `PasswordHasherOptions` override exists anywhere in the project, so ASP.NET Core Identity's pure default applies: PBKDF2 with HMAC-SHA256, 128-bit salt, 256-bit derived key, 100,000 iterations.
- **JWT signing** — `Controllers/AuthController.cs` signs with `HmacSha256` (HS256). Checked the actual configured dev secret key's *length* (not its value): 69 characters ≈ 552 bits, comfortably above the 256-bit minimum HS256 needs. HS256 (symmetric) is appropriate here rather than RS256 (asymmetric) because this API is both the sole issuer and sole verifier of its own tokens — RS256 earns its complexity when a third party needs to verify tokens without holding signing capability (e.g., multiple services trusting one auth server's public key). Revisit this choice if the project ever splits into multiple services.
- `Extensions/AuthenticationServiceExtensions.cs` — `AddAppAuthentication` now validates the configured `Jwt:SecretKey` is present and at least 32 bytes (256 bits) before building `TokenValidationParameters`, throwing `InvalidOperationException` if not. The check lives inside the `AddJwtBearer` configure callback (deferred, resolved on first use) rather than evaluated eagerly at service-registration time — the latter looked more "fail-fast" but broke under `WebApplicationFactory`'s minimal-hosting test host, since its `ConfigureAppConfiguration` overrides aren't guaranteed merged into `builder.Configuration` until `Build()` completes, after `Program.cs`'s own top-level statements have already run.
- `DotNetSecurityFocused.Tests/Tests/CryptographicConfigurationTests.cs` — plain unit tests (no `ApiFactory`/HTTP needed, since this is configuration-time behavior) proving a too-short key throws when `JwtBearerOptions` are resolved, and a sufficiently long key doesn't.

### 📖 Outcome
Password hashing and JWT signing choices are verified against the actual running configuration rather than assumed, and a weak/missing signing key can no longer pass silently — verified by 2 new tests (40 total passing across the project).

---

## Task 10: JWT Revocation / Refresh-Token Rotation

**Status:** Done

### 🎯 Objective
Add a way to invalidate an issued JWT before its natural expiry (logout, compromised token, role change).

### 🛡 Security Focus
Covers OWASP A07:2021 – Identification and Authentication Failures (specifically, the "does not properly invalidate session/tokens" failure mode listed under this category). Today's JWTs are purely stateless — once issued, a token remains valid until expiry with no way to revoke it, even if the user's password changes or the account is disabled. This is a known, real limitation of naive JWT auth.

### 🛠 Implementation
- `Controllers/AuthController.cs` — access token lifetime shrunk from 1 hour to 15 minutes; `Login` now also issues a refresh token alongside the existing access token (the JSON field name `token` was kept as-is, only `refreshToken` was added, so the ~40 existing tests built on `AuthHelper`/`TokenResponse` needed no changes)
- `Models/Entities/RefreshToken.cs` — `UserId`, `TokenHash`, `ExpiresAt`, `RevokedAt`; only a SHA-256 hash of the token is persisted, never the raw value, so a DB leak alone can't hand out usable refresh tokens
- `Services/RefreshTokenService.cs`:
  - `IssueAsync` — generates a 256-bit random opaque token (not a JWT), stores its hash
  - `RotateAsync` — validates the presented token is unexpired and unrevoked, revokes it, and issues a replacement in the same operation; a stolen-but-already-rotated-away token fails lookup because it's marked revoked, which is what rejects reuse
  - `RevokeAsync` — used by logout; doesn't reveal whether the token existed, mirroring Task 1's anti-enumeration approach
- `Controllers/AuthController.cs` — `POST /auth/refresh` (rotates and returns new tokens), `POST /auth/logout` (revokes)
- `DotNetSecurityFocused.Tests/Tests/RefreshTokenTests.cs`:
  - A valid refresh token returns a new token pair, different from the original
  - Reusing an already-rotated-away refresh token returns 401
  - Logging out, then attempting to refresh with that token, returns 401

### 📖 Outcome
Access tokens remain short-lived and stateless for performance, but the overall session can now be revoked on demand — a logged-out or rotated-away refresh token can never mint a new access token — verified by 3 new tests (43 total passing across the project).

---

## Task 11: Security Response Headers

**Status:** Done

### 🎯 Objective
Add standard secure-by-default HTTP response headers across all endpoints.

### 🛡 Security Focus
Covers OWASP A05:2021 – Security Misconfiguration (missing security headers is explicitly listed under this category). Defense-in-depth against clickjacking, MIME-sniffing, and protocol-downgrade attacks — cheap to add, expected on any production-grade API.

### 🛠 Implementation
- `Extensions/SecurityHeadersExtensions.cs` — `UseSecurityHeaders()` middleware sets `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, and `Content-Security-Policy: default-src 'none'; frame-ancestors 'none'` (a pure JSON API has no legitimate reason to load any sub-resource, so denying everything is the correct default, not just a strict one) on every response, registered as the first middleware in the pipeline so it applies even to error responses
- `Program.cs` — `app.UseHsts()` added (framework built-in, guarded by `!IsDevelopment()`, the standard pattern); `builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false)` suppresses the `Server: Kestrel` header
- `DotNetSecurityFocused.Tests/Tests/SecurityHeadersTests.cs`:
  - Confirms `X-Content-Type-Options`, `X-Frame-Options`, and `Content-Security-Policy` are present on a real response
  - Confirms `Strict-Transport-Security` is present on an HTTPS request — required pointing the test client's `BaseAddress` at a non-`localhost` host, since `HstsMiddleware` deliberately excludes `localhost`/`127.0.0.1`/`[::1]` by default (so local development over `https://localhost` never gets HSTS-pinned in a browser)
  - No test for the `Server` header removal — `WebApplicationFactory`'s in-memory `TestServer` bypasses Kestrel entirely, so that specific change can't be verified through this test host; it's a manual/production-only check (e.g., `curl -I` against the running app)

### 📖 Outcome
All API responses carry secure-by-default headers, and the app no longer discloses that it runs on Kestrel — verified by 2 new tests (45 total passing across the project).

---

## Task 12: Agentic Governance Guardrails

**Status:** Planned

### 🎯 Objective
Build a validation wrapper that inspects AI-generated code snippets before they are accepted or executed.

### 🛡 Security Focus
Not a single OWASP Top 10 category itself (it's a process/tooling control) — but the patterns it catches map to OWASP A02:2021 – Cryptographic Failures (weak ciphers like MD5/DES) and A06:2021 – Vulnerable and Outdated Components (deprecated/unsafe APIs). Detect insecure code patterns introduced through AI-augmented workflows before they reach the codebase.

### 🛠 Implementation Plan
- Create a Guardrail service class as the central validation entry point
- Implement a scanner that flags forbidden patterns (e.g., `Thread.Abort`, weak ciphers such as MD5/DES)
- Document how this service acts as a security layer for AI-augmented development
- Add unit tests covering both flagged and allowed code samples

### 📖 Expected Outcome
Snippets containing forbidden patterns are flagged and rejected with a clear reason, providing an auditable security checkpoint for AI-generated code.
