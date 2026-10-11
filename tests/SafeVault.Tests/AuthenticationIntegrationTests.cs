using System.Net;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SafeVault.Tests
{
    public class AuthenticationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AuthenticationIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task SuccessfulLogin_CreatesAuthCookie()
        {
            var pw = new SafeVault.Services.PasswordService();
            var repo = new InMemoryRepoForIntegration(pw);

            using var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace IUserRepository with our in-memory repo
                    services.AddSingleton<SafeVault.Data.IUserRepository>(repo);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            // Get antiforgery token from the login page
            var get = await client.GetAsync("/Account/Login");
            var html = await get.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(html);

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", token),
                new KeyValuePair<string,string>("Username", "admin"),
                new KeyValuePair<string,string>("Password", "Admin@123")
            });

            var resp = await client.PostAsync("/Account/Login", content);

            Assert.Equal(HttpStatusCode.Redirect, resp.StatusCode);

            // Verify Set-Cookie header contains the auth cookie
            Assert.True(resp.Headers.TryGetValues("Set-Cookie", out var cookies));
            Assert.Contains(cookies, c => c.Contains(".AspNetCore.Cookies") || c.Contains(".AspNetCore.Identity.Application") || c.Contains("Cookies"));
        }

        [Fact]
        public async Task FailedLogin_DoesNotCreateAuthCookie()
        {
            var pw = new SafeVault.Services.PasswordService();
            var repo = new InMemoryRepoForIntegration(pw);

            using var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<SafeVault.Data.IUserRepository>(repo);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            // Get antiforgery token from the login page
            var get2 = await client.GetAsync("/Account/Login");
            var html2 = await get2.Content.ReadAsStringAsync();
            var token2 = ExtractAntiforgeryToken(html2);

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", token2),
                new KeyValuePair<string,string>("Username", "admin"),
                new KeyValuePair<string,string>("Password", "BadPassword")
            });

            var resp = await client.PostAsync("/Account/Login", content);

            // Login failed should return 200 with view (not redirect)
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            // Ensure no auth cookie set
            if (resp.Headers.TryGetValues("Set-Cookie", out var cookies))
            {
                Assert.DoesNotContain(cookies, c => c.Contains(".AspNetCore.Cookies") || c.Contains("Cookies"));
            }
        }

        private static string ExtractAntiforgeryToken(string html)
        {
            // Look for: <input name="__RequestVerificationToken" type="hidden" value="..." />
            var marker = "name=\"__RequestVerificationToken\"";
            var idx = html.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return string.Empty;

            var valIdx = html.IndexOf("value=\"", idx, StringComparison.OrdinalIgnoreCase);
            if (valIdx < 0) return string.Empty;

            valIdx += "value=\"".Length;
            var endIdx = html.IndexOf('"', valIdx);
            if (endIdx < 0) return string.Empty;

            return html[valIdx..endIdx];
        }

        private class InMemoryRepoForIntegration : SafeVault.Data.IUserRepository
        {
            private readonly SafeVault.Models.VaultUser _admin;

            public InMemoryRepoForIntegration(SafeVault.Services.PasswordService pw)
            {
                _admin = new SafeVault.Models.VaultUser
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = pw.HashPassword("Admin@123"),
                    Role = "admin"
                };
            }

            public Task<bool> UserExistsAsync(string username) => Task.FromResult(username == _admin.Username);
            public Task<SafeVault.Models.VaultUser?> GetUserByUsernameAsync(string username) => Task.FromResult(username == _admin.Username ? _admin : null as SafeVault.Models.VaultUser);
            public Task<SafeVault.Models.VaultUser?> GetUserByIdAsync(int userId) => Task.FromResult(userId == _admin.Id ? _admin : null as SafeVault.Models.VaultUser);
        }
    }
}
