using System.ComponentModel.DataAnnotations;

namespace WebYugi.DTOs;

public sealed class LoginRequest
{
    [Required, StringLength(254, MinimumLength = 1)]
    public string EmailOrName { get; set; } = "";

    [Required, StringLength(128, MinimumLength = 1)]
    public string Password { get; set; } = "";
}

public sealed class RegisterRequest
{
    [Required, StringLength(100, MinimumLength = 3)]
    public string Name { get; set; } = "";

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";

    [Required, StringLength(128, MinimumLength = 8)]
    public string Senha { get; set; } = "";
}

public sealed class ForgotPasswordRequest
{
    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = "";
}

public sealed class ResetPasswordRequest
{
    [Required, StringLength(200, MinimumLength = 20)]
    public string Token { get; set; } = "";

    [Required, StringLength(128, MinimumLength = 8)]
    public string NewPassword { get; set; } = "";
}
