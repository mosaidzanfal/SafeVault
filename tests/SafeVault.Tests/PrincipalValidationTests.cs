using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Testing;
using SafeVault.Data;
using SafeVault.Security;
using Microsoft.AspNetCore.Http;
using System.Linq;
using Xunit;

namespace SafeVault.Tests
{
    public class PrincipalValidationTests
    {
        [Fact]
        public async Task ValidAdminPrincipal_ShouldRemainValid()
        {
            var fakeRepo = new InMemoryUserRepository();
            fakeRepo.AddUser(new Models.VaultUser { Id = 1, Username = "admin", Role = "admin" });

            var validator = new PrincipalValidator(fakeRepo);

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "admin")
            }, "Test"));

            var valid = await validator.ValidateAsync(principal, new DefaultHttpContext());

            Assert.True(valid);
        }

        [Fact]
        public async Task RoleChangedToUser_ShouldBeRejected()
        {
            var fakeRepo = new InMemoryUserRepository();
            fakeRepo.AddUser(new Models.VaultUser { Id = 2, Username = "bob", Role = "user" });

            var validator = new PrincipalValidator(fakeRepo);

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "2"),
                new Claim(ClaimTypes.Role, "admin")
            }, "Test"));

            var valid = await validator.ValidateAsync(principal, new DefaultHttpContext());

            Assert.False(valid);
        }

        [Fact]
        public async Task DeletedUser_ShouldBeRejected()
        {
            var fakeRepo = new InMemoryUserRepository();

            var validator = new PrincipalValidator(fakeRepo);

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "999"),
                new Claim(ClaimTypes.Role, "admin")
            }, "Test"));

            var valid = await validator.ValidateAsync(principal, new DefaultHttpContext());

            Assert.False(valid);
        }

        [Fact]
        public async Task MissingOrInvalidUserIdClaim_ShouldBeRejected()
        {
            var fakeRepo = new InMemoryUserRepository();
            fakeRepo.AddUser(new Models.VaultUser { Id = 3, Username = "joe", Role = "user" });

            var validator = new PrincipalValidator(fakeRepo);

            var principalNoClaim = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "user")
            }, "Test"));

            Assert.False(await validator.ValidateAsync(principalNoClaim, new DefaultHttpContext()));

            var principalBadClaim = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "notanint"),
                new Claim(ClaimTypes.Role, "user")
            }, "Test"));

            Assert.False(await validator.ValidateAsync(principalBadClaim, new DefaultHttpContext()));
        }

        [Fact]
        public async Task DatabaseLookupFailure_ShouldBeRejected()
        {
            var fakeRepo = new FailingUserRepository();

            var validator = new PrincipalValidator(fakeRepo);

            var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "1"),
                new Claim(ClaimTypes.Role, "admin")
            }, "Test"));

            var valid = await validator.ValidateAsync(principal, new DefaultHttpContext());

            Assert.False(valid);
        }

        #region Test doubles

        private class InMemoryUserRepository : IUserRepository
        {
            private readonly Dictionary<int, Models.VaultUser> _users = new();

            public void AddUser(Models.VaultUser user) => _users[user.Id] = user;

            public Task<bool> UserExistsAsync(string username)
            {
                return Task.FromResult(_users.Values.Any(u => u.Username == username));
            }

            public Task<Models.VaultUser?> GetUserByUsernameAsync(string username)
            {
                var found = _users.Values.FirstOrDefault(u => u.Username == username);
                return Task.FromResult(found);
            }

            public Task<Models.VaultUser?> GetUserByIdAsync(int userId)
            {
                _users.TryGetValue(userId, out var user);
                return Task.FromResult(user);
            }
        }

        private class FailingUserRepository : IUserRepository
        {
            public Task<bool> UserExistsAsync(string username) => throw new Exception("DB down");
            public Task<Models.VaultUser?> GetUserByUsernameAsync(string username) => throw new Exception("DB down");
            public Task<Models.VaultUser?> GetUserByIdAsync(int userId) => throw new Exception("DB down");
        }

        #endregion
    }
}
