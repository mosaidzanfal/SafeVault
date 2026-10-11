using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace SafeVault.Tests
{
    public class RateLimitingTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public RateLimitingTests(WebApplicationFactory<Program> factory)
        {
            // Ensure the host runs in Testing environment so test-only middleware is enabled
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("environment", "Testing");
            });
        }

        private async Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string ip)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/Account/Login");

            request.Headers.Add("X-TEST-REMOTE-IP", ip);

            request.Content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("Username", "attacker"),
                new KeyValuePair<string, string>("Password", "badpw")
            });

            return await client.SendAsync(request);
        }

        [Fact]
        public async Task FirstFiveAllowed_SixthRejectedWith429()
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var ip = "192.0.2.1";

            for (int i = 0; i < 5; i++)
            {
                var resp = await PostLoginAsync(client, ip);
                Assert.NotEqual((HttpStatusCode)429, resp.StatusCode);
            }

            var sixth = await PostLoginAsync(client, ip);
            Assert.Equal((HttpStatusCode)429, sixth.StatusCode);
        }

        [Fact]
        public async Task DifferentClientIPPartitionsAreIndependent()
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var ipA = "192.0.2.10";
            var ipB = "198.51.100.20";

            for (int i = 0; i < 5; i++)
            {
                var resp = await PostLoginAsync(client, ipA);
                Assert.NotEqual((HttpStatusCode)429, resp.StatusCode);
            }

            // This request from a different IP should still be allowed
            var respB = await PostLoginAsync(client, ipB);
            Assert.NotEqual((HttpStatusCode)429, respB.StatusCode);
        }

        [Fact]
        public async Task RequestsArePermittedAfterWindowExpires()
        {
            using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

            var ip = "192.0.2.50";

            for (int i = 0; i < 5; i++)
            {
                await PostLoginAsync(client, ip);
            }

            var sixth = await PostLoginAsync(client, ip);
            Assert.Equal((HttpStatusCode)429, sixth.StatusCode);

            // Wait for window to expire (configured as 1 minute in the app)
            await Task.Delay(TimeSpan.FromMinutes(1) + TimeSpan.FromSeconds(2));

            var after = await PostLoginAsync(client, ip);
            Assert.NotEqual((HttpStatusCode)429, after.StatusCode);
        }
    }
}
