using System.ComponentModel.DataAnnotations;

namespace SafeVault.Models
{
    public class UserInput
    {
        // Use secure input validation for a SafeVault web application.
        // Create a UserInput model with Username and Email properties.
        // Username is required, 3-50 characters, and should allow only letters, numbers, underscore, dot, and hyphen.
        // Email is required and must be a valid email address.
        // Add validation that rejects potentially malicious HTML/script input to help prevent XSS.
        [Required]
        [StringLength(50, MinimumLength = 3)]
        [RegularExpression(
            @"^[a-zA-Z0-9_.-]*$",
            ErrorMessage = "Username can only contain letters, numbers, underscore, dot, and hyphen.")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [RegularExpression(
            @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
            ErrorMessage = "Please enter a valid email address.")]
        public string Email { get; set; } = string.Empty;
    }
}