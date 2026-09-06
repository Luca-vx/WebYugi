using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WebYugi.Infrastructure;
using WebYugi.Model;

namespace WebYugi.Controllers
{
    [ApiController]
    [Route("users")]
    public class UsersController : ControllerBase
    {
        private readonly DbConnection _context;

        public UsersController(DbConnection context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDto>>> GetAll()
        {
            var list = await _context.Users
                .AsNoTracking()
                .Select(u => new UserDto(u.Id, u.name, u.email))
                .ToListAsync();

            return Ok(list);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDto>> Get(int id)
        {
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null) return NotFound();

            var dto = new UserDto(user.Id, user.name, user.email);
            return Ok(dto);
        }


        //parei
        [HttpPost]
        public async Task<ActionResult<UserDto>> Create([FromBody] Users user)
        {
            if (user == null) return BadRequest();

            // validate duplicate email or username (case-insensitive)
            var normalizedEmail = user.email?.Trim().ToLowerInvariant();
            var normalizedName = user.name?.Trim();

            var exists = await _context.Users.AnyAsync(u =>
                u.email.ToLower() == normalizedEmail || u.name == normalizedName);

            if (exists)
            {
                return Conflict(new { message = "Email ou nome de usuário já cadastrado." });
            }

            // ignore any client-supplied Id; let the database set it
            var newUser = new Users(user.name, user.email, user.senha);

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            var dto = new UserDto(newUser.Id, newUser.name, newUser.email);
            return CreatedAtAction(nameof(Get), new { id = newUser.Id }, dto);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(int id, [FromBody] Users updated)
        {
            if (updated == null) return BadRequest();

            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return NotFound();

            existing.name = updated.name;
            existing.email = updated.email;
            if (!string.IsNullOrWhiteSpace(updated.senha))
            {
                existing.senha = updated.senha;
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            var existing = await _context.Users.FindAsync(id);
            if (existing == null) return NotFound();

            _context.Users.Remove(existing);
            await _context.SaveChangesAsync();
            return NoContent();
        }
    }
}
