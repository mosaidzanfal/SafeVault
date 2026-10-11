using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SafeVault.Data;
using SafeVault.Services;
using Xunit;

namespace SafeVault.Tests
{
    public class AuthenticationTests
    {
        private readonly SafeVault.Services.PasswordService _passwordService = new();

        private class InMemoryUserRepository : SafeVault.Data.IUserRepository
        {
            private readonly Dictionary<string, SafeVault.Models.VaultUser> _users = new();

            public InMemoryUserRepository(PasswordService pw)
            {
                var admin = new SafeVault.Models.VaultUser
                {
                    Id = 1,
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = pw.HashPassword("Admin@123"),
                    Role = "admin"
                };

                _users[admin.Username] = admin;
            }

            public Task<bool> UserExistsAsync(string username)
            {
                return Task.FromResult(_users.ContainsKey(username));
            }

            public Task<SafeVault.Models.VaultUser?> GetUserByUsernameAsync(string username)
            {
                _users.TryGetValue(username, out var u);
                return Task.FromResult(u);
            }

            public Task<SafeVault.Models.VaultUser?> GetUserByIdAsync(int userId)
            {
                var user = _users.Values.FirstOrDefault(x => x.Id == userId);
                return Task.FromResult(user);
            }
        }

        [Fact]
        public async Task ValidCredentials_ShouldAuthenticate()
        {
            var repo = new InMemoryUserRepository(_passwordService);

            var user = await repo.GetUserByUsernameAsync("admin");

            Assert.NotNull(user);

            var valid = _passwordService.VerifyPassword("Admin@123", user!.PasswordHash);

            Assert.True(valid);
        }

        [Fact]
        public async Task InvalidPassword_ShouldFailAuthentication()
        {
            var repo = new InMemoryUserRepository(_passwordService);

            var user = await repo.GetUserByUsernameAsync("admin");

            Assert.NotNull(user);

            var valid = _passwordService.VerifyPassword("WrongPassword123", user!.PasswordHash);

            Assert.False(valid);
        }

        [Fact]
        public async Task NonExistingUser_ShouldNotAuthenticate()
        {
            var repo = new InMemoryUserRepository(_passwordService);

            var user = await repo.GetUserByUsernameAsync("nonexistent_user_12345");

            Assert.Null(user);
        }
    }
}
