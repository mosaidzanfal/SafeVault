using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using SafeVault.Models;
using Xunit;

namespace SafeVault.Tests
{
    public class InputValidationTests
    {
        private static List<ValidationResult> ValidateModel(UserInput model)
        {
            var results = new List<ValidationResult>();

            var context = new ValidationContext(model);

            Validator.TryValidateObject(
                model,
                context,
                results,
                validateAllProperties: true);

            return results;
        }

        [Fact]
        public void ValidInput_ShouldPassValidation()
        {
            var model = new UserInput
            {
                Username = "mosaad",
                Email = "mosaad@example.com"
            };

            var results = ValidateModel(model);

            Assert.Empty(results);
        }

        [Fact]
        public void XssScriptInUsername_ShouldFailValidation()
        {
            var model = new UserInput
            {
                Username = "<script>alert('XSS')</script>",
                Email = "test@example.com"
            };

            var results = ValidateModel(model);

            Assert.NotEmpty(results);
            Assert.Contains(
                results,
                r => r.MemberNames.Contains(nameof(UserInput.Username)));
        }

        [Fact]
        public void XssHtmlInEmail_ShouldFailValidation()
        {
            var model = new UserInput
            {
                Username = "safeuser",
                Email = "<script>alert('XSS')</script>@example.com"
            };

            var results = ValidateModel(model);

            Assert.NotEmpty(results);
            Assert.Contains(
                results,
                r => r.MemberNames.Contains(nameof(UserInput.Email)));
        }
    }
}
