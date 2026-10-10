using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using System.Net;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddScoped<SafeVault.Services.PasswordService>();
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services

    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";

        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        // Validate principal on every request to detect role changes or deleted users.
        options.Events = new Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationEvents
        {
            OnValidatePrincipal = async context =>
            {
                try
                {
                    var validator = context.HttpContext.RequestServices.GetRequiredService<SafeVault.Security.IPrincipalValidator>();

                    var valid = await validator.ValidateAsync(context.Principal, context.HttpContext);

                    if (!valid)
                    {
                        context.RejectPrincipal();
                        await context.HttpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                    }
                }
                catch
                {
                    // On any unexpected failure, reject the principal to avoid silently
                    // accepting an elevated principal when DB cannot be reached.
                    context.RejectPrincipal();
                    await context.HttpContext.SignOutAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                }
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<SafeVault.Data.IUserRepository>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();

    var connectionString =
        configuration.GetConnectionString("SafeVaultConnection");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        throw new InvalidOperationException(
            "SafeVault database connection string was not found. Set ConnectionStrings:SafeVaultConnection using user-secrets or environment variables.");
    }

    return new SafeVault.Data.UserRepository(connectionString);
});

// Register principal validator used by cookie auth event
builder.Services.AddScoped<SafeVault.Security.IPrincipalValidator, SafeVault.Security.PrincipalValidator>();
builder.Services.AddRateLimiter(options =>
{
    // Use 429 Too Many Requests when requests are rejected by the limiter.
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    // Policy used specifically for POST /Account/Login to limit attempts per IP
    options.AddPolicy("LoginPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
});
builder.Services.AddScoped<SafeVault.Data.UserRepository>(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();

    var connectionString =
        configuration.GetConnectionString("SafeVaultConnection")
        ?? throw new InvalidOperationException(
            "SafeVault database connection string was not found.");

    return new SafeVault.Data.UserRepository(connectionString);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// In testing, allow a test-only header to set RemoteIpAddress for partitioning so
// tests can simulate different client IPs. This is only enabled when the
// environment is explicitly 'Testing' to avoid trusting forwarded headers in
// production.
if (app.Environment.IsEnvironment("Testing"))
{
    app.Use(async (context, next) =>
    {
        if (context.Request.Headers.TryGetValue("X-TEST-REMOTE-IP", out var header) &&
            IPAddress.TryParse(header, out var ip))
        {
            context.Connection.RemoteIpAddress = ip;
        }

        await next();
    });
}

// Rate limiting middleware must be placed after routing so endpoint metadata
// (EnableRateLimiting attribute) is available, and before authentication/authorization
// so abusive requests are short-circuited early.
app.UseRateLimiter();

// In testing environment, short-circuit POST /Account/Login to avoid hitting
// production DB during integration tests. Rate limiter still runs before this
// middleware so tests exercise the limiter without depending on authentication
// backend services.
if (app.Environment.IsEnvironment("Testing"))
{
    app.Use(async (context, next) =>
    {
        if (HttpMethods.IsPost(context.Request.Method) &&
            context.Request.Path.Equals("/Account/Login", StringComparison.OrdinalIgnoreCase))
        {
            context.Response.StatusCode = StatusCodes.Status200OK;
            await context.Response.WriteAsync("OK");
            return;
        }

        await next();
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

// Expose a Program partial class for integration tests (WebApplicationFactory).
public partial class Program { }
