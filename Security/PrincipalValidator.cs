using System.Security.Claims;
using SafeVault.Data;
using SafeVault.Models;
using Microsoft.AspNetCore.Http;

namespace SafeVault.Security
{
    public class PrincipalValidator : IPrincipalValidator
    {
        private readonly IUserRepository _userRepository;

        public PrincipalValidator(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<bool> ValidateAsync(ClaimsPrincipal principal, HttpContext httpContext)
        {
            if (principal == null)
            {
                return false;
            }

            var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrWhiteSpace(idClaim) || !int.TryParse(idClaim, out var userId))
            {
                return false;
            }

            VaultUser? currentUser;

            try
            {
                currentUser = await _userRepository.GetUserByIdAsync(userId);
            }
            catch
            {
                // On DB failure, fail closed: do not accept the principal.
                return false;
            }

            if (currentUser == null)
            {
                // User deleted.
                return false;
            }

            var roleClaim = principal.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

            // Compare roles in a case-insensitive manner.
            if (!string.Equals(currentUser.Role ?? string.Empty, roleClaim ?? string.Empty, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
