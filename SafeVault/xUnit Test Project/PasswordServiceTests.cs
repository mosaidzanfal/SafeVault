using SafeVault.Services;

namespace SafeVault.Tests
{
    public class PasswordServiceTests
    {
        private readonly PasswordService _passwordService = new();

        [Fact]
        public void HashPassword_ShouldNotStorePlainTextPassword()
        {
            string password = "Admin@123";

            string hash = _passwordService.HashPassword(password);

            Assert.NotEqual(password, hash);
            Assert.False(string.IsNullOrWhiteSpace(hash));
        }

        [Fact]
        public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
        {
            string password = "Admin@123";
            string hash = _passwordService.HashPassword(password);

            bool result =
                _passwordService.VerifyPassword(password, hash);

            Assert.True(result);
        }

        [Fact]
        public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
        {
            string password = "Admin@123";
            string hash = _passwordService.HashPassword(password);

            bool result =
                _passwordService.VerifyPassword("WrongPassword", hash);

            Assert.False(result);
        }
    }
}
