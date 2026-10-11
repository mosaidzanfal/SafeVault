# SafeVault — Security Report

This document summarizes the security audit, findings, mitigations, and remaining recommendations for the SafeVault sample application used in the Coursera secure development assignment.

## 1. Security audit methodology
- Static code review of controllers, models, data access, services, Program.cs and Razor views.
- Focus on authentication/authorization, input validation, SQL injection, XSS, CSRF, cookie security, and secrets handling.
- Added automated integration and unit tests to verify security controls and prevent regressions.
- All changes were implemented in the repository and validated via unit/integration tests.

## 2. Findings from repository review
- SQL injection: Repository methods used parameterized queries; no SQL injection detected in reviewed methods.
- XSS: Razor views use default encoding and do not call Html.Raw on user-supplied content — low risk in present templates.
- Input validation: Registration model had strict validation; Login and Search were lacking consistent constraints (fixed).
- Authentication weaknesses: No account lockout or global throttling initially (addressed partially with rate limiting).
- Login brute-force: POST /Account/Login had no throttling (fixed by adding IP-based rate limiting policy).
- Stale role claims: Auth cookie contained role claims that could become stale after DB role change (fixed by OnValidatePrincipal revalidation).
- Sensitive configuration: A connection string was previously present in appsettings.json and a test file; these were removed and tests were refactored to use environment variables or in-memory fakes.
- Tests: Some authentication tests previously returned early when no test DB was configured; replaced with in-memory tests and integration tests that use DI replacement.

## 3. Severity classification
- Critical: None found in the reviewed scope.
- High: Lack of account lockout (not implemented here; recommended for production), stale role claims (fixed), potential secret leakage if previously committed (addressed by removing secrets from source files, requires rotation if in Git history).
- Medium: Missing uniform input validation (fixed), cookie/session revocation mechanism (mitigated with principal revalidation), no server-side session store for revocation.
- Low: XSS and SQL injection mitigations already in place, but continuous review recommended.

## 4. Affected components
- Data/UserRepository.cs — parameterized queries (no vulnerable concatenation found).
- Controllers/AccountController.cs — Login flow (rate limiting, validation, cookie creation).
- Program.cs — cookie options, rate limiting and principal revalidation wiring.
- Models/LoginViewModel.cs, Models/UserInput.cs, Models/SearchViewModel.cs — input validation.
- tests/SafeVault.Tests — tests refactored to avoid hard-coded secrets and to test security behavior.

## 5. Root causes
- Partial or inconsistent input validation across login and search flows.
- Lack of per-request principal validation to detect role changes.
- Tests originally used hard-coded connection strings and returned early when not configured, causing misleading passes.

## 6. Implemented fixes
- Added RegularExpression validation to LoginViewModel and new SearchViewModel; updated Search view and controller to validate ModelState before repository calls.
- Implemented IP-based fixed-window rate limiter for POST /Account/Login (5 permits / 1 minute, queue limit 0) using Microsoft.AspNetCore.RateLimiting.
- Implemented CookieAuthenticationEvents.OnValidatePrincipal with a PrincipalValidator that checks current DB user existence and role via IUserRepository.GetUserByIdAsync; rejects principal on mismatch or DB error.
- Introduced IUserRepository interface and added GetUserByIdAsync on UserRepository with parameterized SQL.
- Removed hard-coded connection strings from appsettings.json and tests; tests now use environment variables or in-memory fakes; added .gitignore to avoid committing secrets and artifacts.
- Replaced misleading early-return authentication tests with in-memory repository tests and DI-based integration tests that assert cookie creation and CSRF enforcement.
- Added automated security regression tests for cookie flags and CSRF enforcement.

## 7. Security tests
- Unit tests for:
  - PasswordService hashing and verification.
  - Input validation for username and email.
  - PrincipalValidator behavior for deleted users, role changes, missing claims, and DB failures.
- Integration tests for:
  - Rate limiting behavior (first 5 requests allowed, 6th returns 429, partitions by client IP, permitted after window).
  - Authentication flow with DI-replaced IUserRepository (successful login sets cookie; failed login does not).
  - Cookie attributes verification (HttpOnly, Secure, SameSite=Strict).
  - CSRF enforcement (POST without token or with tampered token results in 400).

## 8. Before / after security improvements
- Before: Missing server-side validation for login/search, no rate limiting on login, stale role claims persisted in cookies, tests leaked connection details and could silently pass.
- After: Validation unified across flows, login rate limiting in place, principal revalidation implemented, secrets removed from source, tests isolated and thorough.

## 9. Remaining limitations
- Account lockout is not implemented — consider adding server-side failed-login tracking and lockout/backoff.
- Per-user throttling (username-based) is not present; IP-based is effective but can be circumvented by distributed attacks.
- Principal revalidation performs a DB lookup per request; consider caching or session store for high-scale scenarios.
- If secrets were committed in Git history, they must be rotated and removed from history using repository-cleaning tools (manual coordination required).

## 10. Recommendations for production deployment
- Use a production-grade authentication store or ASP.NET Core Identity for advanced features (lockout, 2FA, account confirmation) if appropriate.
- Store DataProtection keys in a secure shared store (Azure Blob, Redis, or file share) for multi-instance deployments.
- Configure ForwardedHeaders only with KnownProxies or KnownNetworks when behind a reverse proxy; do not blindly trust X-Forwarded-For.
- Harden rate limiting with additional measures: per-account throttling, CAPTCHA after repeated failures, and monitoring/alerts.
- Use a secure secrets store (Azure Key Vault, AWS Secrets Manager) for production secrets and rotate them regularly.
- Consider a WAF and logging/monitoring for anomalous authentication attempts.

---

This report documents the actions taken and remaining considerations. It avoids claiming exploitations where protections already existed.
