using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebYugi.Infrastructure;
using WebYugi.Model;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Collections.Generic;

namespace WebYugi.DTOs
{
    public class LoginRequest
    {
        [Required]
        public string EmailOrName { get; set; }

        [Required]
        public string Password { get; set; }

        // Perform login using this DTO instance. Returns 200 with UserDto on success or appropriate error result.
        public async Task<ActionResult<UserDto>> LoginAsync(AppDbContext context, HttpContext httpContext)
        {
            if (this == null || string.IsNullOrWhiteSpace(this.Password) || string.IsNullOrWhiteSpace(this.EmailOrName))
            {
                return new BadRequestObjectResult(new { message = "Credenciais inválidas." });
            }

            var query = this.EmailOrName.Trim();

            var user = await context.Users.FirstOrDefaultAsync(u => u.Email == query || u.Name == query);
            if (user == null) return new UnauthorizedObjectResult(new { message = "Usuário não encontrado." });

            if (!user.ValidatePassword(this.Password))
            {
                return new UnauthorizedObjectResult(new { message = "Senha inválida." });
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            var dto = new UserDto(user.Id, user.Name, user.Email, user.DataCadastro);
            return new OkObjectResult(dto);
        }

        // Perform logout (invalidate cookie/session)
        public async Task<ActionResult> LogoutAsync(HttpContext httpContext)
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return new NoContentResult();
        }

        // Return current authenticated user's data
        public async Task<ActionResult<UserDto>> MeAsync(AppDbContext context, HttpContext httpContext)
        {
            var userPrincipal = httpContext.User;
            if (userPrincipal?.Identity == null || !userPrincipal.Identity.IsAuthenticated)
                return new UnauthorizedResult();

            if (!int.TryParse(userPrincipal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
                return new UnauthorizedResult();

            var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return new NotFoundResult();

            var dto = new UserDto(user.Id, user.Name, user.Email, user.DataCadastro);
            return new OkObjectResult(dto);
        }
    }
}
