using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Mail;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using WebYugi.DTOs;
using WebYugi.Infrastructure;
using WebYugi.Model;
using WebYugi.Services;

namespace WebYugi.Controllers;

[ApiController]
[Route("users")]
public sealed class UsersController : ControllerBase
{
    private const string GenericAuthError = "Credenciais inválidas.";
    private const string ResetResponse = "Se houver uma conta associada a esse e-mail, enviaremos instruções para redefinir a senha.";
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<Users> _passwordHasher;
    private readonly IPasswordResetEmailSender _emailSender;
    private readonly ILogger<UsersController> _logger;
    private readonly IConfiguration _configuration;
    private readonly Users _dummyUser = new("", "", "");
    private readonly string _dummyPasswordHash;

    public UsersController(AppDbContext context, IPasswordHasher<Users> passwordHasher,
        IPasswordResetEmailSender emailSender, ILogger<UsersController> logger, IConfiguration configuration)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _emailSender = emailSender;
        _logger = logger;
        _configuration = configuration;
        _dummyPasswordHash = _passwordHasher.HashPassword(_dummyUser, Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)));
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var identifier = request.EmailOrName.Trim();
        var normalizedEmail = identifier.ToLowerInvariant();
        var normalizedName = identifier.ToUpperInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(candidate =>
            candidate.NormalizedEmail == normalizedEmail || candidate.NormalizedName == normalizedName, cancellationToken);

        if (user is null)
        {
            // Perform equivalent password-hash work for unknown identifiers to reduce timing-based enumeration.
            _passwordHasher.VerifyHashedPassword(_dummyUser, _dummyPasswordHash, request.Password);
            return Unauthorized(new { message = GenericAuthError });
        }

        var valid = VerifyPassword(user, request.Password);
        if (!valid) return Unauthorized(new { message = GenericAuthError });

        if (!user.SenhaHashed || _passwordHasher.VerifyHashedPassword(user, user.Senha, request.Password) == PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.Senha = _passwordHasher.HashPassword(user, request.Password);
            user.SenhaHashed = true;
            await _context.SaveChangesAsync(cancellationToken);
        }

        await SignInAsync(user);
        return Ok(ToDto(user));
    }

    [HttpPost]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<UserDto>> Create([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        if (!IsStrongPassword(request.Senha, name, email))
            return BadRequest(new { message = "Use uma senha com pelo menos 8 caracteres, letras maiúsculas e minúsculas, um número e um símbolo. Evite senhas comuns ou dados da conta." });

        var normalizedName = name.ToUpperInvariant();
        if (await _context.Users.AnyAsync(user => user.NormalizedEmail == email || user.NormalizedName == normalizedName, cancellationToken))
            return Conflict(new { message = "Não foi possível concluir o cadastro com os dados informados." });

        var user = new Users(name, email, "") { SenhaHashed = true, NormalizedName = normalizedName, NormalizedEmail = email };
        user.Senha = _passwordHasher.HashPassword(user, request.Senha);
        _context.Users.Add(user);
        try { await _context.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException exception) when (exception.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            _logger.LogInformation("Cadastro rejeitado por conflito em restrição única.");
            return Conflict(new { message = "Não foi possível concluir o cadastro com os dados informados." });
        }
        return CreatedAtAction(nameof(Get), new { id = user.Id }, ToDto(user));
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        // Keep the response identical when delivery is not configured or the account does not exist.
        if (_emailSender.IsConfigured)
        {
        var email = request.Email.Trim().ToLowerInvariant();
            var user = await _context.Users.FirstOrDefaultAsync(candidate => candidate.NormalizedEmail == email, cancellationToken);
            if (user is not null)
            {
                await _context.PasswordResetTokens.Where(item => item.UserId == user.Id && item.UsedAt == null)
                    .ExecuteUpdateAsync(update => update.SetProperty(item => item.UsedAt, DateTime.UtcNow), cancellationToken);
                var rawToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
                var token = new PasswordResetToken
                {
                    UserId = user.Id,
                    TokenHash = HashToken(rawToken),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(20)
                };
                _context.PasswordResetTokens.Add(token);
                await _context.SaveChangesAsync(cancellationToken);
                var resetBaseUrl = _configuration["Email:PasswordResetUrl"];
                if (!Uri.TryCreate(resetBaseUrl, UriKind.Absolute, out var baseUri) || baseUri.Scheme != Uri.UriSchemeHttps)
                {
                    _logger.LogError("Password reset email is configured without a valid HTTPS reset URL.");
                    _context.PasswordResetTokens.Remove(token);
                    await _context.SaveChangesAsync(cancellationToken);
                    return Accepted(new { message = ResetResponse });
                }
                var separator = resetBaseUrl!.Contains('?') ? "&" : "?";
                try { await _emailSender.SendAsync(email, user.Name, $"{resetBaseUrl}{separator}token={Uri.EscapeDataString(rawToken)}", cancellationToken); }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    _logger.LogError("Password reset email delivery failed: {ErrorType}", exception.GetType().Name);
                    _context.PasswordResetTokens.Remove(token);
                    await _context.SaveChangesAsync(cancellationToken);
                }
            }
        }
        return Accepted(new { message = ResetResponse });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(request.Token);
        var token = await _context.PasswordResetTokens.Include(item => item.User)
            .FirstOrDefaultAsync(item => item.TokenHash == tokenHash && item.UsedAt == null && item.ExpiresAt > DateTime.UtcNow, cancellationToken);
        if (token is null) return BadRequest(new { message = "Link de redefinição inválido ou expirado." });
        if (!IsStrongPassword(request.NewPassword, token.User.Name, token.User.Email))
            return BadRequest(new { message = "Use uma senha com pelo menos 8 caracteres, letras maiúsculas e minúsculas, um número e um símbolo. Evite senhas comuns ou dados da conta." });

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var claimed = await _context.PasswordResetTokens.Where(item => item.Id == token.Id && item.UsedAt == null && item.ExpiresAt > now)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.UsedAt, now), cancellationToken);
        if (claimed != 1) return BadRequest(new { message = "Link de redefinição inválido ou expirado." });
        token.User.Senha = _passwordHasher.HashPassword(token.User, request.NewPassword);
        token.User.SenhaHashed = true;
        token.User.MustChangePassword = false;
        token.User.PasswordVersion++;
        await _context.PasswordResetTokens.Where(item => item.UserId == token.UserId && item.Id != token.Id && item.UsedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(item => item.UsedAt, now), cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Me(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == userId, cancellationToken);
        return user is null ? NotFound() : Ok(ToDto(user));
    }

    [HttpPut("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return await UpdateUser(userId, request, cancellationToken);
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var user = await _context.Users.AsNoTracking().Where(item => item.Id == userId)
            .Select(item => new UserDto(item.Id, item.Name, item.Email, item.DataCadastro, item.IsAdmin, item.MustChangePassword)).ToListAsync(cancellationToken);
        return Ok(user);
    }

    [HttpGet("{id:int}")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Get(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var currentUserId)) return Unauthorized();
        if (id != currentUserId) return Forbid();
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken);
        return user is null ? NotFound() : Ok(ToDto(user));
    }

    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var currentUserId)) return Unauthorized();
        if (id != currentUserId) return Forbid();
        var result = await UpdateUser(id, request, cancellationToken);
        if (result.Result is not OkObjectResult) return result.Result ?? NoContent();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var currentUserId)) return Unauthorized();
        if (id != currentUserId) return Forbid();
        var user = await _context.Users.FindAsync([id], cancellationToken);
        if (user is null) return NotFound();
        if (user.IsAdmin)
            return Conflict(new { message = "Um administrador não pode excluir a própria conta por este endpoint. Faça a transferência administrativa manualmente antes." });
        _context.Users.Remove(user);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    public sealed record UpdateProfileRequest(string Name, string Email, string? Senha, string? SenhaAtual = null);

    private async Task<ActionResult<UserDto>> UpdateUser(int userId, UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || name.Length < 3 || string.IsNullOrWhiteSpace(email) || email.Length > 254 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            return BadRequest(new { message = "Informe um nome e e-mail válidos." });
        var normalizedName = name.ToUpperInvariant();
        if (await _context.Users.AnyAsync(user => user.Id != userId && (user.NormalizedName == normalizedName || user.NormalizedEmail == email), cancellationToken))
            return Conflict(new { message = "Esse nome ou e-mail já está sendo usado." });
        var user = await _context.Users.FindAsync([userId], cancellationToken);
        if (user is null) return NotFound();
        if (!string.IsNullOrWhiteSpace(request.Senha) && !IsStrongPassword(request.Senha, name, email))
            return BadRequest(new { message = "Use uma senha com pelo menos 8 caracteres, letras maiúsculas e minúsculas, um número e um símbolo. Evite senhas comuns ou dados da conta." });
        if (!string.IsNullOrWhiteSpace(request.Senha) && (string.IsNullOrWhiteSpace(request.SenhaAtual) || !VerifyPassword(user, request.SenhaAtual)))
            return Unauthorized(new { message = "A senha atual está incorreta." });
        user.Name = name;
        user.Email = email;
        user.NormalizedName = normalizedName;
        user.NormalizedEmail = email;
        if (!string.IsNullOrWhiteSpace(request.Senha))
        {
            user.Senha = _passwordHasher.HashPassword(user, request.Senha);
            user.SenhaHashed = true;
            user.PasswordVersion++;
            user.MustChangePassword = false;
        }
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            return Conflict(new { message = "Esse nome ou e-mail já está sendo usado." });
        }
        await SignInAsync(user);
        return Ok(ToDto(user));
    }

    private async Task SignInAsync(Users user)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim("pwd_ver", user.PasswordVersion.ToString())
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    }

    private bool TryGetUserId(out int id) => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);
    private bool VerifyPassword(Users user, string password) => user.SenhaHashed
        ? _passwordHasher.VerifyHashedPassword(user, user.Senha, password) != PasswordVerificationResult.Failed
        : CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(user.Senha), Encoding.UTF8.GetBytes(password));
    private static UserDto ToDto(Users user) => new(user.Id, user.Name, user.Email, user.DataCadastro, user.IsAdmin, user.MustChangePassword);
    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static bool IsStrongPassword(string password, string name, string email)
    {
        if (password.Length < 8 || password.Length > 128 || password.All(char.IsWhiteSpace)) return false;
        if (!password.Any(char.IsUpper) || !password.Any(char.IsLower) || !password.Any(char.IsDigit) || !password.Any(character => !char.IsLetterOrDigit(character))) return false;
        var normalized = Regex.Replace(password, "[^a-z0-9]", "", RegexOptions.IgnoreCase).ToLowerInvariant();
        string[] common = ["password", "senha", "qwerty", "123456", "admin", "letmein", "iloveyou"];
        if (common.Any(value => normalized.Contains(value, StringComparison.Ordinal))) return false;
        var accountTerms = new[] { name, email.Split('@')[0] };
        return accountTerms.Where(term => term.Length >= 4).All(term => !normalized.Contains(Regex.Replace(term, "[^a-z0-9]", "", RegexOptions.IgnoreCase), StringComparison.OrdinalIgnoreCase));
    }
}
