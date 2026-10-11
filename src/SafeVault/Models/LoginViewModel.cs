using System.ComponentModel.DataAnnotations;

namespace SafeVault.Models
{
    public class LoginViewModel
    {
        [Required]
        [StringLength(50, MinimumLength = 3)]
    [RegularExpression(
        @"^[a-zA-Z0-9_.-]*$",
        ErrorMessage = "Username can only contain letters, numbers, underscore, dot, and hyphen.")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }
}