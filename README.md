# DotNetSecurityFocused

A hands-on tour of .NET/ASP.NET Core security engineering — one real attack surface at a time. Each task in this repo takes on a specific vulnerability class, grounded in the OWASP Top 10 and beyond, hardens it in working code, and proves the fix holds with tests.

See **[SECURITY-TASKS.md](SECURITY-TASKS.md)** for the full breakdown: what each task covers, which OWASP category it maps to, where the implementation lives, and its status. All 12 tasks are currently **Done**.

## What's covered

- JWT authentication with role-based access control (RBAC)
- SQL injection prevention (parameterized queries vs. a documented vulnerable reference)
- Input validation (Data Annotations + FluentValidation cross-field rules)
- Secrets management (User Secrets locally, environment variables in production)
- API rate limiting (sliding window, brute-force protection on login)
- Dependency vulnerability remediation (NuGet security advisories)
- Authorization beyond roles — object-level ownership checks (IDOR prevention)
- Structured security event logging (failed logins, rate-limit rejections, role changes, authorization failures)
- Verified cryptographic choices (password hashing, JWT signing key strength)
- JWT revocation via refresh-token rotation
- Secure-by-default HTTP response headers
- An AI-code guardrail scanner wired into a real git pre-commit hook

## Tech stack

- .NET 10 / ASP.NET Core Web API
- Entity Framework Core + SQLite
- ASP.NET Core Identity + JWT Bearer authentication (HS256)
- FluentValidation
- xUnit + `WebApplicationFactory` integration tests

## Getting started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)

### Setup

All commands below are run from the repository root. The web app lives in `src/DotNetSecurityFocused`, so commands that target it pass `--project src/DotNetSecurityFocused`.

1. Clone the repo and restore dependencies (restores the whole solution):
   ```
   dotnet restore
   ```

2. Set the required JWT secrets for local development (see `src/DotNetSecurityFocused/secrets.template.json` for the full list of keys):
   ```
   dotnet user-secrets set "Jwt:SecretKey" "<a long, high-entropy random string, at least 32 bytes>" --project src/DotNetSecurityFocused
   dotnet user-secrets set "Jwt:Issuer" "DotNetSecurityFocused" --project src/DotNetSecurityFocused
   dotnet user-secrets set "Jwt:Audience" "DotNetSecurityFocusedUsers" --project src/DotNetSecurityFocused
   ```
   ASP.NET Core only reads User Secrets when `ASPNETCORE_ENVIRONMENT=Development` (the default for `dotnet run` locally). For any other environment, set `Jwt__SecretKey`, `Jwt__Issuer`, `Jwt__Audience` as real environment variables instead — see Task 4 in `SECURITY-TASKS.md` for details.

3. (Optional) Copy the development logging template:
   ```
   cp src/DotNetSecurityFocused/appsettings.Development.json.template src/DotNetSecurityFocused/appsettings.Development.json
   ```

4. Run the API:
   ```
   dotnet run --project src/DotNetSecurityFocused
   ```

5. Run the tests (discovers both test projects via the solution):
   ```
   dotnet test
   ```

### Exploring the API

`DotNetSecurityFocused.http` (in the repo root) contains a full set of ready-to-run requests covering every endpoint and several deliberate attack scenarios (SQL injection payload, cross-user IDOR attempt, refresh-token reuse, rate-limit exhaustion). Open it in an editor with REST Client support (e.g. VS Code's REST Client extension) and run requests top to bottom — it includes inline instructions for copying tokens between requests.

### Code guardrails (pre-commit hook)

`tools/GuardrailCli` scans staged C# files for insecure patterns (weak crypto, insecure deserialization, hardcoded secrets) and is wired into a git `pre-commit` hook that blocks a commit if it finds a violation. Git hooks live in `.git/hooks/` and are **not** tracked by git, so after a fresh clone the hook isn't installed automatically — see Task 12 in `SECURITY-TASKS.md` for how it's set up. The scanner logic itself lives in the `DotNetSecurityFocused.Guardrails` library and is covered by unit tests, independent of the hook.

## Project structure

```
src/
  DotNetSecurityFocused/                 ASP.NET Core Web API (the main app)
    Controllers/                         API endpoints
    Services/                            Business logic (orders, product search, refresh tokens, security event logging)
    Models/Entities/                     EF Core entities (DB tables)
    Models/DTOs/                         Request/response contracts (deliberately separate from entities)
    Data/                                DbContext, seeding, migrations
    Extensions/                          DI/middleware registration (auth, rate limiting, security headers)
    Authorization/                       Custom authorization pipeline hooks
    Validators/                          FluentValidation rules
  DotNetSecurityFocused.Guardrails/      Standalone library: scans code for insecure patterns
tests/
  DotNetSecurityFocused.Tests/           Integration tests (WebApplicationFactory)
  DotNetSecurityFocused.Guardrails.Tests/  Unit tests for the guardrail scanner
tools/
  GuardrailCli/                          Console wrapper around the Guardrails library, invoked by the pre-commit hook
```
