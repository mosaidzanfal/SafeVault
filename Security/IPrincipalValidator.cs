using System.Security.Claims;
namespace SafeVault.Security
{
    public interface IPrincipalValidator
    {
        /// <summary>
        /// Validate the principal against current user data in persistence.
        /// Returns true if the principal is valid and should remain authenticated.
        /// </summary>
        Task<bool> ValidateAsync(ClaimsPrincipal principal, HttpContext httpContext);
    }
}
