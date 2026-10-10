# SafeVault

SafeVault is a secure sample ASP.NET Core MVC application (framework: .NET 10) used for a Coursera secure development assignment. It demonstrates secure authentication, authorization, input validation, SQL protection, and automated security testing.

## Overview
SafeVault models a minimal user management system with secure login and admin-restricted functionality. The project focuses on secure coding patterns and defenses rather than production features.

## Technologies used
- .NET 10 / ASP.NET Core MVC
- Razor views
- Microsoft.Data.SqlClient for data access
- BCrypt.Net-Next for password hashing
- Microsoft.AspNetCore.Authentication.Cookies for cookie authentication
- Microsoft.AspNetCore.RateLimiting for login throttling
- xUnit and Microsoft.AspNetCore.Mvc.Testing for tests

## Application architecture
- Controllers: MVC controllers handle web requests (AccountController, UserController, AdminController, HomeController).
- Models: View models and domain models (VaultUser, UserInput, LoginViewModel, SearchViewModel).
- Data: UserRepository implements parameterized SQL access to the Users table via ADO.NET.
- Services: PasswordService encapsulates BCrypt hashing and verification.
- Security: Cookie authentication, role-based authorization, rate limiting, principal revalidation, and CSRF protection.

## Features implemented
- Secure registration input validation (username, email).
- Secure login with BCrypt password verification.
- Role-based authorization for admin endpoints.
- IP-based login rate limiting (5 attempts per minute, fixed window).
- Cookie principal revalidation on each request to detect role changes or user deletion.
- CSRF protection on state-changing POST endpoints.
- Parameterized SQL queries to prevent injection.
- Automated unit and integration tests covering auth, validation, rate limiting, and security regressions.

## Security controls (high-level)
- Input validation: Models enforce Required, StringLength, and RegularExpression where appropriate.
- SQL injection prevention: All ADO.NET queries use SqlParameter and parameterized SQL.
- XSS protection: Razor automatic encoding is used; views avoid Html.Raw on user data.
- Password hashing: BCrypt via PasswordService with Verify and Hash functions.
- Cookie authentication: HttpOnly, Secure, and SameSite=Strict configured for cookies.
- Role-based authorization: [Authorize(Roles = "admin")] applied to admin controller.
- Login rate limiting: ASP.NET Core rate limiting configured and applied to POST /Account/Login.
- Principal revalidation: CookieAuthenticationEvents.OnValidatePrincipal checks current DB role and existence.
- CSRF protection: @Html.AntiForgeryToken() used in forms and [ValidateAntiForgeryToken] on POST actions.

## Automated testing
- Unit and integration tests in the `xUnit Test Project` project.
- Tests cover password hashing, SQL injection regression, input validation, rate limiting, principal validation, authentication cookie behavior, and CSRF enforcement.
- Tests use in-memory repository fakes or DI replacement to avoid hitting production databases.

## Local setup instructions
1. Prerequisites
   - .NET 10 SDK
   - SQL Server or SQL Server Express (for local run with a real DB)

2. Configuration
   - The application reads `ConnectionStrings:SafeVaultConnection` from configuration. Do NOT hard-code credentials in source.
   - For local development use `dotnet user-secrets` or environment variables.

3. Use user secrets (recommended for local dev)
   - From the SafeVault project directory:
	 - `dotnet user-secrets init`
	 - `dotnet user-secrets set "ConnectionStrings:SafeVaultConnection" "<your-connection-string>"`
   - Example (do not commit):
	 - `dotnet user-secrets set "ConnectionStrings:SafeVaultConnection" "Server=localhost;Database=SafeVaultDb;User Id=sa;Password=YourStrong!Pass;TrustServerCertificate=True;"`

4. Environment variables
   - For CI or production, set `ConnectionStrings__SafeVaultConnection` (double underscores) as an environment variable.
   - Tests that need an actual database can use `SAFEVAULT_TEST_CONNECTION` environment variable if you want to run DB-backed integration tests, but test code defaults to in-memory fakes.

## Build and test
- Build:
  - `dotnet build`
- Run tests:
  - `dotnet test`

## How to run the application
- Configure `ConnectionStrings:SafeVaultConnection` via user secrets or environment variable.
- From the SafeVault project folder:
  - `dotnet run`
- Open the browser at the address shown by the host (HTTPS recommended).

## SQL Server database requirements
- A `Users` table with columns: Id (int), Username (nvarchar), Email (nvarchar), PasswordHash (nvarchar), Role (nvarchar).
- The sample tests and repository expect a table named `Users` with these columns. When running with a real DB, ensure the test DB is dedicated and not production.

## Repository structure
- SafeVault/ (ASP.NET Core MVC project)
  - Controllers/
  - Data/
  - Models/
  - Services/
  - Views/
  - Program.cs
- xUnit Test Project/ (test project)
  - Authentication and security tests

## Security considerations
- Do not commit secrets or connection strings to the repository. Use user-secrets or environment variables.
- Rotate credentials if they were ever checked into version control.
- For production, secure DataProtection keys and configure persistent key storage.
- Configure trusted proxies explicitly if the app is deployed behind a load balancer and you must trust forwarded headers.

---

This README is intended for developers and graders. It omits any real credentials or machine-specific names.
