using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebYugi.Infrastructure;
using WebYugi.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using WebYugi.DTOs;
using Microsoft.AspNetCore.Authorization;

namespace WebYugi.Controllers
{
    [ApiController]
    [Route("users")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        public record UpdateProfileRequest(string Name, string Email, string? Senha);

        [HttpPut("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
            var name = request.Name?.Trim();
            var email = request.Email?.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 100 || string.IsNullOrWhiteSpace(email) || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
                return BadRequest(new { message = "Informe um nome e e-mail válidos." });
            var duplicate = await _context.Users.AnyAsync(u => u.Id != userId && (u.Name.ToLower() == name.ToLower() || u.Email.ToLower() == email.ToLower()), cancellationToken);
            if (duplicate) return Conflict(new { message = "Esse nome ou e-mail já está sendo usado." });
            var user = await _context.Users.FindAsync([userId], cancellationToken);
            if (user is null) return NotFound();
            user.Name = name;
            user.Email = email;
            if (!string.IsNullOrWhiteSpace(request.Senha))
            {
                if (request.Senha.Length < 8) return BadRequest(new { message = "A senha deve ter ao menos 8 caracteres." });
                user.Senha = request.Senha;
            }
            await _context.SaveChangesAsync(cancellationToken);
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.Name), new Claim(ClaimTypes.Email, user.Email) };
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            return Ok(new UserDto(user.Id, user.Name, user.Email, user.DataCadastro));
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<UserDto>> Login([FromBody] LoginRequest request)
        {
            if (request == null) return BadRequest(new { message = "Credenciais inválidas." });
            return await request.LoginAsync(_context, HttpContext);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<ActionResult> Logout()
        {
            var dto = new LoginRequest();
            return await dto.LogoutAsync(HttpContext);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<UserDto>> Me()
        {
            var dto = new LoginRequest();
            return await dto.MeAsync(_context, HttpContext);
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)) return Unauthorized();
            var list = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => new UserDto(u.Id, u.Name, u.Email, u.DataCadastro))
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<UserDto>> Get(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId)) return Unauthorized();
            if (id != currentUserId) return Forbid();
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var dto = new UserDto(user.Id, user.Name, user.Email, user.DataCadastro);
            return Ok(dto);
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<UserDto>> Create([FromBody] Users user)
        {
            if (user == null) return BadRequest();

            // validate duplicate email or username (case-insensitive)
            var normalizedEmail = user.Email?.Trim().ToLowerInvariant();
            var normalizedName = user.Name?.Trim();

            var exists = await _context.Users.AnyAsync(u =>
                u.Email.ToLower() == normalizedEmail || u.Name == normalizedName);

            if (exists)
            {
                return Conflict(new { message = "Email ou nome de usuário já cadastrado." });
            }

            // ignore any client-supplied Id; let the database set it
            var newUser = new Users(user.Name, user.Email, user.Senha);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var dto = new UserDto(newUser.Id, newUser.Name, newUser.Email, newUser.DataCadastro);
            return CreatedAtAction(nameof(Get), new { id = newUser.Id }, dto);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult> Update(int id, [FromBody] Users updated)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId)) return Unauthorized();
            if (id != currentUserId) return Forbid();
            if (updated == null) return BadRequest();

            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return NotFound();

            existing.Name = updated.Name;
            existing.Email = updated.Email;
            if (!string.IsNullOrWhiteSpace(updated.Senha))
            {
                existing.Senha = updated.Senha;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<ActionResult> Delete(int id)
        {
            if (!int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var currentUserId)) return Unauthorized();
            if (id != currentUserId) return Forbid();
            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return NotFound();

            _context.Users.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
