using Microsoft.AspNetCore.Authorization;
using SafeVault.Controllers;
using Xunit;

namespace SafeVault.Tests
{
    public class AuthorizationTests
    {
        [Fact]
        public void AdminController_ShouldRequireAuthorization()
        {
            var attribute = Attribute.GetCustomAttribute(
                typeof(AdminController),
                typeof(AuthorizeAttribute));

            Assert.NotNull(attribute);
        }

        [Fact]
        public void AdminController_ShouldRequireAdminRole()
        {
            var attribute = (AuthorizeAttribute?)
                Attribute.GetCustomAttribute(
                    typeof(AdminController),
                    typeof(AuthorizeAttribute));

            Assert.NotNull(attribute);
            Assert.Equal("admin", attribute.Roles);
        }
    }
}
