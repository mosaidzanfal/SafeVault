using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SafeVault.Controllers;
using SafeVault.Models;
using SafeVault.Services;
using SafeVault.Data;
using Xunit;

namespace SafeVault.Tests
{
    public class ControllerValidationTests
    {
        [Fact]
        public void UsernameValidation_AllowsValidUsername()
        {
            var model = new SearchViewModel { Username = "valid_user-01.name" };
            var validationResults = new List<ValidationResult>();
            var ctx = new ValidationContext(model);

            bool valid = Validator.TryValidateObject(model, ctx, validationResults, true);

            Assert.True(valid);
        }

        [Theory]
        [InlineData("ab")]
        [InlineData("")]
        [InlineData(null)]
        public void UsernameValidation_TooShortOrEmpty_IsInvalid(string username)
        {
            var model = new SearchViewModel { Username = username ?? string.Empty };
            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(model);

            bool valid = Validator.TryValidateObject(model, ctx, results, true);

            Assert.False(valid);
        }

        [Fact]
        public void UsernameValidation_TooLong_IsInvalid()
        {
            var longName = new string('a', 51);
            var model = new SearchViewModel { Username = longName };
            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(model);

            bool valid = Validator.TryValidateObject(model, ctx, results, true);

            Assert.False(valid);
        }

        [Theory]
        [InlineData("<script>alert(1)</script>")]
        [InlineData("admin';--")]
        [InlineData("user\nname")]
        public void UsernameValidation_DisallowedCharacters_IsInvalid(string username)
        {
            var model = new SearchViewModel { Username = username };
            var results = new List<ValidationResult>();
            var ctx = new ValidationContext(model);

            bool valid = Validator.TryValidateObject(model, ctx, results, true);

            Assert.False(valid);
        }

        [Fact]
        public async Task Login_InvalidModel_DoesNotCallRepositoryOrVerify()
        {
            var repo = new SpyUserRepository();
            var pw = new SpyPasswordService();
            var controller = new AccountController(repo, pw);

            // Simulate invalid model state
            controller.ModelState.AddModelError("Username", "Invalid");

            var model = new LoginViewModel { Username = "ab", Password = "p" };

            var result = await controller.Login(model);

            Assert.False(repo.WasCalled);
            Assert.False(pw.WasCalled);
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Search_InvalidModel_DoesNotQueryRepository()
        {
            var repo = new SpyUserRepository();
            var controller = new UserController(repo);

            controller.ModelState.AddModelError("Username", "Invalid");

            var model = new SearchViewModel { Username = "ab" };

            var result = await controller.Search(model);

            Assert.False(repo.WasCalled);
            Assert.IsType<ViewResult>(result);
        }

        #region Test doubles
        private class SpyUserRepository : IUserRepository
        {
            public bool WasCalled { get; private set; }

            public Task<bool> UserExistsAsync(string username)
            {
                WasCalled = true;
                return Task.FromResult(true);
            }

            public Task<Models.VaultUser?> GetUserByUsernameAsync(string username)
            {
                WasCalled = true;
                return Task.FromResult<Models.VaultUser?>(null);
            }

            public Task<Models.VaultUser?> GetUserByIdAsync(int userId)
            {
                WasCalled = true;
                return Task.FromResult<Models.VaultUser?>(null);
            }
        }

        private class SpyPasswordService : PasswordService
        {
            public bool WasCalled { get; private set; }

            public new bool VerifyPassword(string password, string passwordHash)
            {
                WasCalled = true;
                return base.VerifyPassword(password, passwordHash);
            }
        }
        #endregion
    }
}
