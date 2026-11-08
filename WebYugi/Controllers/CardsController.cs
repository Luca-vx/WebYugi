using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebYugi.DTOs;
using WebYugi.Infrastructure;
using WebYugi.Model;
using WebYugi.Services;

namespace WebYugi.Controllers
{
    [ApiController]
    [Route("cards")]
    public class CardsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ICardImageStorage _images;

        public CardsController(AppDbContext context, ICardImageStorage images)
        {
            _context = context;
            _images = images;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CardDto>>> GetAll(
            [FromQuery] string? search,
            [FromQuery] WebYugi.Model.Enums.CardType? type,
            [FromQuery] WebYugi.Model.Enums.CardAttribute? attribute,
            [FromQuery] WebYugi.Model.Enums.MonsterRace? monsterRace,
            [FromQuery] int? minAttack,
            [FromQuery] int? maxAttack,
            [FromQuery] int? minDefense,
            [FromQuery] int? maxDefense,
            [FromQuery] int? minLevel,
            [FromQuery] int? maxLevel,
            [FromQuery] string? archetype,
            [FromQuery] string? rarity,
            [FromQuery] string? description,
            CancellationToken cancellationToken)
        {
            var query = _context.Cards.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(search))
            {
                var term = search.Trim();
                query = query.Where(card => EF.Functions.ILike(card.Name, $"%{term}%"));
            }
            if (type.HasValue) query = query.Where(card => card.Type == type.Value);
            if (attribute.HasValue) query = query.Where(card => card.Attribute == attribute.Value);
            if (monsterRace.HasValue) query = query.Where(card => card.MonsterRace == monsterRace.Value);
            if (minAttack.HasValue) query = query.Where(card => card.Attack >= minAttack.Value);
            if (maxAttack.HasValue) query = query.Where(card => card.Attack <= maxAttack.Value);
            if (minDefense.HasValue) query = query.Where(card => card.Defense >= minDefense.Value);
            if (maxDefense.HasValue) query = query.Where(card => card.Defense <= maxDefense.Value);
            if (minLevel.HasValue) query = query.Where(card => card.Level >= minLevel.Value);
            if (maxLevel.HasValue) query = query.Where(card => card.Level <= maxLevel.Value);
            if (!string.IsNullOrWhiteSpace(archetype)) query = query.Where(card => card.Archetype != null && EF.Functions.ILike(card.Archetype, $"%{archetype.Trim()}%"));
            if (!string.IsNullOrWhiteSpace(rarity)) query = query.Where(card => card.Rarity != null && EF.Functions.ILike(card.Rarity, $"%{rarity.Trim()}%"));
            if (!string.IsNullOrWhiteSpace(description)) query = query.Where(card => EF.Functions.ILike(card.Description, $"%{description.Trim()}%"));

            var cards = await query.OrderBy(card => card.Name).ToListAsync(cancellationToken);

            return Ok(cards.Select(CardDto.FromEntity));
        }

        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<ActionResult<CardDto>> Get(int id)
        {
            var card = await _context.Cards
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (card is null)
            {
                return NotFound();
            }

            return Ok(CardDto.FromEntity(card));
        }

        [HttpPost]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<ActionResult<CardDto>> Create([FromForm] CardFormRequest request, CancellationToken cancellationToken)
        {
            var card = new Card();
            ApplyForm(card, request);

            try
            {
                if (request.Image is not null)
                {
                    card.ImageUrl = await _images.SaveAsync(request.Image, cancellationToken);
                }
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            _context.Cards.Add(card);
            await _context.SaveChangesAsync(cancellationToken);

            var dto = CardDto.FromEntity(card);
            return CreatedAtAction(nameof(Get), new { id = card.Id }, dto);
        }

        [HttpPut("{id:int}")]
        [Authorize]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<ActionResult<CardDto>> Update(int id, [FromForm] CardFormRequest request, CancellationToken cancellationToken)
        {
            var existing = await _context.Cards.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            ApplyForm(existing, request);

            if (request.Image is not null)
            {
                try
                {
                    var newPath = await _images.SaveAsync(request.Image, cancellationToken);
                    _images.Delete(existing.ImageUrl);
                    existing.ImageUrl = newPath;
                }
                catch (InvalidOperationException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }

            await _context.SaveChangesAsync(cancellationToken);
            return Ok(CardDto.FromEntity(existing));
        }

        [HttpDelete("{id:int}")]
        [Authorize]
        public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
        {
            var existing = await _context.Cards.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
            if (existing is null)
            {
                return NotFound();
            }

            _context.Cards.Remove(existing);

            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                return Conflict(new { message = "A carta não pode ser excluída porque está em uso em decks, coleções ou banlists." });
            }

            _images.Delete(existing.ImageUrl);
            return NoContent();
        }

        private static void ApplyForm(Card card, CardFormRequest request)
        {
            card.Name = request.Name.Trim();
            card.Type = request.Type;
            card.Attribute = request.Attribute;
            card.MonsterRace = request.MonsterRace;
            card.Level = request.Level;
            card.Attack = request.Attack;
            card.Defense = request.Defense;
            card.Description = request.Description.Trim();
            card.Archetype = string.IsNullOrWhiteSpace(request.Archetype) ? null : request.Archetype.Trim();
            card.Rarity = string.IsNullOrWhiteSpace(request.Rarity) ? null : request.Rarity.Trim();
        }
    }
}
