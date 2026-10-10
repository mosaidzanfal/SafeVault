using SafeVault.Data;
using Xunit;

namespace SafeVault.Tests
{
    public class SqlInjectionTests
    {
        private const string ConnectionString =
            "Server=DESKTOP-A4939OM;Database=SafeVaultDb;Trusted_Connection=True;TrustServerCertificate=True;";

        [Fact]
        public async Task ExistingUser_ShouldBeFound()
        {
            var repository = new UserRepository(ConnectionString);

            bool result = await repository.UserExistsAsync("admin");

            Assert.True(result);
        }

        [Fact]
        public async Task SqlInjectionAttempt_ShouldNotBypassQuery()
        {
            var repository = new UserRepository(ConnectionString);

            string maliciousInput = "' OR '1'='1' --";

            bool result =
                await repository.UserExistsAsync(maliciousInput);

            Assert.False(result);
        }

        [Fact]
        public async Task SqlInjectionWithComment_ShouldNotBypassQuery()
        {
            var repository = new UserRepository(ConnectionString);

            string maliciousInput = "admin' --";

            bool result =
                await repository.UserExistsAsync(maliciousInput);

            Assert.False(result);
        }
    }
}
