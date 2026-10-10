using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace SafeVault.Tests
{
    public class AuthenticationCookieAndCsrfTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public AuthenticationCookieAndCsrfTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task SuccessfulLogin_SetsSecureHttpOnlySameSiteStrictCookie()
        {
            var pw = new SafeVault.Services.PasswordService();
            var repo = new InMemoryRepoForIntegration(pw);

            using var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<SafeVault.Data.IUserRepository>(repo);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });

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

            Assert.True(resp.Headers.TryGetValues("Set-Cookie", out var cookies));
            var cookie = cookies.FirstOrDefault();
            Assert.False(string.IsNullOrEmpty(cookie));

            var cookieLower = cookie.ToLowerInvariant();
            Assert.Contains("httponly", cookieLower);
            Assert.Contains("secure", cookieLower);
            Assert.Contains("samesite=strict", cookieLower);
        }

        [Fact]
        public async Task PostWithoutAntiforgery_IsRejectedWith400()
        {
            var pw = new SafeVault.Services.PasswordService();
            var repo = new InMemoryRepoForIntegration(pw);

            using var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<SafeVault.Data.IUserRepository>(repo);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("Username", "admin"),
                new KeyValuePair<string,string>("Password", "Admin@123")
            });

            var resp = await client.PostAsync("/Account/Login", content);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        [Fact]
        public async Task PostWithInvalidAntiforgery_IsRejectedWith400()
        {
            var pw = new SafeVault.Services.PasswordService();
            var repo = new InMemoryRepoForIntegration(pw);

            using var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<SafeVault.Data.IUserRepository>(repo);
                });
            }).CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });

            // Get a valid token and then tamper it
            var get = await client.GetAsync("/Account/Login");
            var html = await get.Content.ReadAsStringAsync();
            var token = ExtractAntiforgeryToken(html);

            var badToken = token + "tamper";

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("__RequestVerificationToken", badToken),
                new KeyValuePair<string,string>("Username", "admin"),
                new KeyValuePair<string,string>("Password", "Admin@123")
            });

            var resp = await client.PostAsync("/Account/Login", content);

            Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
        }

        private static string ExtractAntiforgeryToken(string html)
        {
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
