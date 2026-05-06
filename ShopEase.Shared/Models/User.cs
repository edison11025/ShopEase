using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace ShopEase.Shared.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username must be less than 50 characters")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100, ErrorMessage = "Email must be less than 100 characters")]
        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        [StringLength(20, ErrorMessage = "Phone number must be less than 20 characters")]
        public string? Phone { get; set; }

        // Raw password from client (will be hashed in controller)
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        public string Password { get; set; } = string.Empty;

        // Stored hashed password in DB
        public string PasswordHash { get; set; } = string.Empty;

        // Default role assignment
        public string Role { get; set; } = "Customer";

        // Navigation property: one user can have many cart items
        public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

        // Navigation property: one user can have many refresh tokens
        public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    }
}
