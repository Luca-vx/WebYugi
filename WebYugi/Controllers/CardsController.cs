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
        public async Task<ActionResult<IEnumerable<CardDto>>> GetAll()
        {
            var cards = await _context.Cards
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync();

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
            card.Rank = request.Rank;
            card.Attack = request.Attack;
            card.Defense = request.Defense;
            card.Description = request.Description.Trim();
            card.Archetype = string.IsNullOrWhiteSpace(request.Archetype) ? null : request.Archetype.Trim();
            card.Rarity = string.IsNullOrWhiteSpace(request.Rarity) ? null : request.Rarity.Trim();
        }
    }
}
