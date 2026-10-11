# SafeVault

SafeVault is a secure sample ASP.NET Core MVC application (built with .NET 10) developed for a Coursera secure development assignment. It demonstrates defense-in-depth secure architecture: robust authentication, role-based authorization, comprehensive input validation, SQL injection prevention, cross-site scripting (XSS) defense, cross-site request forgery (CSRF) mitigation, IP-based login rate limiting, per-request cookie principal revalidation, and automated security testing.

## Overview
SafeVault models a user management and credential verification system with secure registration, authentication, search, and admin-restricted features. The project adheres to secure software development lifecycle (SSDLC) principles, demonstrating proactive security controls and regression test verification.

## Technology Stack
- **Framework:** .NET 10 (C# 13)
- **Web Layer:** ASP.NET Core MVC (Razor Views)
- **Data Access:** ADO.NET via `Microsoft.Data.SqlClient` (parameterized queries)
- **Cryptography:** BCrypt via `BCrypt.Net-Next`
- **Authentication:** Cookie Authentication (`Microsoft.AspNetCore.Authentication.Cookies`)
- **Rate Limiting:** `Microsoft.AspNetCore.RateLimiting` (fixed-window throttling)
- **Testing:** xUnit (`xunit`), `Microsoft.NET.Test.Sdk`, `Microsoft.AspNetCore.Mvc.Testing`
- **Solution & Build:** Visual Studio 2026 / .NET 10 CLI (`SafeVault.slnx`)
- **Version Control:** Git & GitHub

## Repository Structure
The repository is organized following clean architectural separation between source code and automated tests:

```text
SafeVault/
├── SafeVault.slnx                 # Unified solution file referencing app and tests
├── README.md                      # Architecture, build, test, and setup guide
├── SECURITY_REPORT.md             # Security audit report and mitigations
├── COPILOT_REFLECTION.md          # Reflection on GitHub Copilot-assisted development
├── .gitignore                     # Git ignore rules for .NET, build artifacts, secrets
├── .gitattributes                 # Git line ending configuration
│
├── src/
│   └── SafeVault/                 # ASP.NET Core MVC application project
│       ├── SafeVault.csproj
│       ├── Program.cs             # App entry point, DI, pipeline configuration
│       ├── appsettings.json       # App configuration (no secrets stored)
│       ├── Controllers/           # Account, Admin, Home, User controllers
│       ├── Data/                  # UserRepository and database interfaces
│       ├── Models/                # ViewModels and Domain models with validation
│       ├── Properties/            # Launch settings
│       ├── Security/              # PrincipalValidator and security contracts
│       ├── Services/              # PasswordService (BCrypt hashing/verification)
│       ├── Views/                 # Razor views with anti-forgery & encoding
│       └── wwwroot/               # Static client-side assets (CSS, JS, libraries)
│
└── tests/
    └── SafeVault.Tests/           # Automated security test suite (37 tests)
        ├── SafeVault.Tests.csproj # Test project referencing src/SafeVault
        ├── GlobalUsings.cs
        ├── AuthenticationCookieAndCsrfTests.cs
        ├── AuthenticationIntegrationTests.cs
        ├── AuthenticationTests.cs
        ├── AuthorizationTests.cs
        ├── ControllerValidationTests.cs
        ├── InputValidationTests.cs
        ├── PasswordServiceTests.cs
        ├── PrincipalValidationTests.cs
        ├── RateLimitingTests.cs
        └── SqlInjectionTests.cs
```

## Security Architecture & Controls

1. **Input Validation:**
   - Models (`UserInput`, `LoginViewModel`, `SearchViewModel`) enforce strict `[Required]`, `[StringLength]`, and `[RegularExpression]` attributes.
   - Controllers reject invalid state prior to interacting with data services.

2. **SQL Injection Prevention:**
   - All database access in `UserRepository` uses ADO.NET parameterized queries with `SqlParameter`. No dynamic string concatenation is used for SQL execution.

3. **Cross-Site Scripting (XSS) Defense:**
   - Razor automatic HTML encoding is leveraged across all views; raw HTML rendering (`Html.Raw`) is forbidden on untrusted data.

4. **Cross-Site Request Forgery (CSRF) Protection:**
   - State-changing HTTP POST endpoints enforce `@Html.AntiForgeryToken()` forms and `[ValidateAntiForgeryToken]` action filters. Requests with missing or forged tokens are rejected with HTTP 400.

5. **Authentication & Session Security:**
   - Passwords are salted and hashed using BCrypt.
   - Authentication cookies are hardened with `HttpOnly`, `Secure`, and `SameSite=Strict`.

6. **IP-Based Login Rate Limiting:**
   - Throttles brute-force login attempts using ASP.NET Core RateLimiting (5 requests per 1-minute fixed window per IP). Exceeding requests receive HTTP 429.

7. **Per-Request Principal Revalidation:**
   - `CookieAuthenticationEvents.OnValidatePrincipal` delegates to `PrincipalValidator`, querying `IUserRepository.GetUserByIdAsync` to confirm the user still exists and their assigned role has not changed or been revoked.

## Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/)
- Git 2.30+
- (Optional) SQL Server / LocalDB / SQL Server Express (for local real-database runs)

## Build Instructions
From the repository root directory:

```shell
dotnet restore SafeVault.slnx
dotnet build SafeVault.slnx
```

## Test Instructions
The automated test suite contains 37 tests covering unit, integration, and security regression scenarios:

```shell
dotnet test SafeVault.slnx
```

All integration tests use `WebApplicationFactory<Program>` with in-memory test doubles and dependency injection substitution, ensuring zero external database dependencies during automated test execution.

## Local Configuration Instructions
Do **not** commit real connection strings or credentials to version control.

### Using .NET User Secrets (Recommended for Local Development)
From `src/SafeVault`:
```shell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:SafeVaultConnection" "Server=localhost;Database=SafeVaultDb;User Id=sa;Password=YourSecurePassword!;TrustServerCertificate=True;"
```

### Using Environment Variables
For CI/CD or production environments, set the environment variable:
- Windows PowerShell:
  ```powershell
  $env:ConnectionStrings__SafeVaultConnection="Server=localhost;Database=SafeVaultDb;User Id=sa;Password=YourSecurePassword!;TrustServerCertificate=True;"
  ```
- Linux / macOS:
  ```bash
  export ConnectionStrings__SafeVaultConnection="Server=localhost;Database=SafeVaultDb;User Id=sa;Password=YourSecurePassword!;TrustServerCertificate=True;"
  ```

### Running the Application
From `src/SafeVault`:
```shell
dotnet run
```
Open a browser and navigate to the displayed HTTPS URL (e.g., `https://localhost:5001`).

## GitHub Copilot Development Notes
During project development and review, GitHub Copilot was utilized as an AI pair programming assistant:
- Guided the implementation of standardized validation models and security regression test fixtures.
- Assisted in identifying stale role claim vulnerabilities in persistent cookies, leading to the creation of the `PrincipalValidator` middleware pipeline.
- Helped architect automated integration tests using `WebApplicationFactory<Program>` to test CSRF and cookie security attributes without requiring an active SQL Server database.
- Full reflections, methodologies, and human oversight considerations are documented in [COPILOT_REFLECTION.md](COPILOT_REFLECTION.md).
