using System.ComponentModel.DataAnnotations;
public class RegisterRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 8)]
    [RegularExpression(@"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*\W).+$",
        ErrorMessage = "Password must contain upper, lower, digit, and special char.")]
    public string Password { get; set; } = string.Empty;
}
