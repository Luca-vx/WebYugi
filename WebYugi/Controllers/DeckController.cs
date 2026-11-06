using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using WebYugi.Infrastructure;
using WebYugi.Model;
using WebYugi.Model.Enums;

namespace WebYugi.Controllers
{
    [ApiController]
    [Route("decks")]
    public class DeckController : ControllerBase
    {
        private readonly AppDbContext _context;
        public DeckController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Deck>>> GetAll()
        {
            var decks = await _context.Decks
                .AsNoTracking()
                .ToListAsync();
            return Ok(decks);
        }
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<ActionResult<Deck>> Get(int id)
        {
            var deck = await _context.Decks
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
            if (deck is null)
            {
                return NotFound();
            }
            return Ok(deck);
        }

        [HttpPost("{nome}")]
        [Authorize]
        public async Task<ActionResult<Deck>> Create(string nome)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var deck = new Deck(userId, nome);
            _context.Decks.Add(deck);
            await _context.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = deck.Id }, deck);
        }

        [HttpPatch("{deckId:int}/cards/{cardId:int}")]
        [Authorize]
        public async Task<ActionResult> AdicionarCard(
            int deckId,
            int cardId,
            [FromBody] AdicionarCardRequest? request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var deckExists = await _context.Decks
                .AnyAsync(deck => deck.Id == deckId && deck.UserId == userId);
            if (!deckExists)
            {
                return NotFound(new { message = "Deck não encontrado para o usuário autenticado." });
            }

            var cardExists = await _context.Cards.AnyAsync(card => card.Id == cardId);
            if (!cardExists)
            {
                return NotFound(new { message = "Carta não encontrada." });
            }

            var quantity = request?.Quantity ?? 1;
            var section = request?.Section ?? DeckSection.Main;
            if (quantity <= 0) // Adicione depois uma verificação para a quantidade máxima de 3 cards iguais por deck
            {
                return BadRequest(new { message = "A quantidade deve ser maior que zero." });
            }

            var alreadyAdded = await _context.DeckCards.AnyAsync(deckCard =>
                deckCard.DeckId == deckId &&
                deckCard.CardId == cardId &&
                deckCard.Section == section);
            if (alreadyAdded)
            {
                return Conflict(new { message = "A carta já foi adicionada ao deck nessa seção." });
            }

            var deckCard = new DeckCard
            {
                DeckId = deckId,
                CardId = cardId,
                Quantity = quantity,
                Section = section
            };

            _context.DeckCards.Add(deckCard);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                deckCard.Id,
                deckCard.DeckId,
                deckCard.CardId,
                deckCard.Quantity,
                deckCard.Section
            });
        }

        public class AdicionarCardRequest
        {
            public int Quantity { get; set; } = 1;
            public DeckSection Section { get; set; } = DeckSection.Main;
        }
    }
}
