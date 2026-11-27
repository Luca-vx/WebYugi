using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebYugi.DTOs;
using WebYugi.Infrastructure;

namespace WebYugi.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = "AdminOnly")]
public sealed class AdminUsersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserDto>>> List([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = db.Users.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(user => EF.Functions.ILike(user.Name, $"%{term}%") || EF.Functions.ILike(user.Email, $"%{term}%"));
        }
        var users = await query.OrderBy(user => user.Name).Select(user => new UserDto(user.Id, user.Name, user.Email, user.DataCadastro, user.IsAdmin, user.MustChangePassword)).ToListAsync(cancellationToken);
        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDto>> Get(int id, CancellationToken cancellationToken)
    {
        var user = await db.Users.AsNoTracking().Where(item => item.Id == id)
            .Select(item => new UserDto(item.Id, item.Name, item.Email, item.DataCadastro, item.IsAdmin, item.MustChangePassword))
            .FirstOrDefaultAsync(cancellationToken);
        return user is null ? NotFound() : Ok(user);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDto>> Update(int id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name?.Trim();
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(name) || name.Length is < 3 or > 100 || string.IsNullOrWhiteSpace(email) || email.Length > 254 || !new EmailAddressAttribute().IsValid(email))
            return BadRequest(new { message = "Informe um nome e e-mail válidos." });
        var normalizedName = name.ToUpperInvariant();
        if (await db.Users.AnyAsync(user => user.Id != id && (user.NormalizedName == normalizedName || user.NormalizedEmail == email), cancellationToken))
            return Conflict(new { message = "Esse nome ou e-mail já está sendo usado." });
        var user = await db.Users.FindAsync([id], cancellationToken);
        if (user is null) return NotFound();
        user.Name = name;
        user.Email = email;
        user.NormalizedName = normalizedName;
        user.NormalizedEmail = email;
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (exception.InnerException is Npgsql.PostgresException { SqlState: Npgsql.PostgresErrorCodes.UniqueViolation })
        {
            return Conflict(new { message = "Esse nome ou e-mail já está sendo usado." });
        }
        return Ok(new UserDto(user.Id, user.Name, user.Email, user.DataCadastro, user.IsAdmin, user.MustChangePassword));
    }

    public sealed record UpdateUserRequest(string Name, string Email);
}
