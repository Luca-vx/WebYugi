using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebYugi.DTOs;
using WebYugi.Infrastructure;
using WebYugi.Model;
using WebYugi.Model.Enums;
using WebYugi.Services;

namespace WebYugi.Controllers;

[ApiController]
[Route("decks")]
[Authorize]
public sealed class DeckController : ControllerBase
{
    private const int MinimumCards = 40;
    private const int MaximumCards = 60;
    private readonly AppDbContext _context;
    private readonly ICardCopyLimitPolicy _copyLimitPolicy;

    public DeckController(AppDbContext context, ICardCopyLimitPolicy copyLimitPolicy)
    {
        _context = context;
        _copyLimitPolicy = copyLimitPolicy;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeckDto>>> GetAll(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();

        var decks = await _context.Decks.AsNoTracking()
            .Where(deck => deck.UserId == userId)
            .OrderBy(deck => deck.Name)
            .Select(deck => new
            {
                deck.Id,
                deck.Name,
                deck.CreatedAt,
                Cards = deck.DeckCards.Select(item => new DeckCardDto(
                    item.CardId, item.Card.Name, item.Card.ImageUrl, item.Quantity, item.Section)).ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(decks.Select(deck => ToDto(deck.Id, deck.Name, deck.CreatedAt, deck.Cards)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DeckDto>> Get(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var deck = await _context.Decks.AsNoTracking()
            .Where(item => item.Id == id && item.UserId == userId)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.CreatedAt,
                Cards = item.DeckCards.Select(card => new DeckCardDto(
                    card.CardId, card.Card.Name, card.Card.ImageUrl, card.Quantity, card.Section)).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return deck is null
            ? NotFound(new { message = "Deck não encontrado." })
            : Ok(ToDto(deck.Id, deck.Name, deck.CreatedAt, deck.Cards));
    }

    [HttpPost]
    public async Task<ActionResult<DeckDto>> Create([FromBody] CreateDeckRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var name = request.Name.Trim();
        if (name.Length == 0) return BadRequest(new { message = "O nome do deck é obrigatório." });

        var deck = new Deck(userId, name);
        _context.Decks.Add(deck);
        await _context.SaveChangesAsync(cancellationToken);
        var result = ToDto(deck.Id, deck.Name, deck.CreatedAt, Array.Empty<DeckCardDto>());
        return CreatedAtAction(nameof(Get), new { id = deck.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<DeckDto>> Rename(int id, [FromBody] UpdateDeckRequest request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var deck = await _context.Decks.FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (deck is null) return NotFound(new { message = "Deck não encontrado." });

        var name = request.Name.Trim();
        if (name.Length == 0) return BadRequest(new { message = "O nome do deck é obrigatório." });
        deck.Name = name;
        await _context.SaveChangesAsync(cancellationToken);
        return await Get(id, cancellationToken);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var deck = await _context.Decks.FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId, cancellationToken);
        if (deck is null) return NotFound(new { message = "Deck não encontrado." });
        _context.Decks.Remove(deck);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    // PATCH increments quantity so adding an existing card increases its copies.
    [HttpPatch("{deckId:int}/cards/{cardId:int}")]
    public async Task<ActionResult<DeckDto>> AddCard(
        int deckId, int cardId, [FromBody] AddDeckCardRequest? request, CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        request ??= new AddDeckCardRequest();
        if (request.Quantity is < 1 or > 3 || !Enum.IsDefined(request.Section))
            return BadRequest(new { message = "Quantidade ou seção inválida." });

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var lockedDeck = await _context.Decks.FromSqlInterpolated(
                $"SELECT * FROM decks WHERE \"Id\" = {deckId} AND \"UserId\" = {userId} FOR UPDATE")
            .AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (lockedDeck is null) return NotFound(new { message = "Deck não encontrado." });

        var cardExists = await _context.Cards.AnyAsync(card => card.Id == cardId, cancellationToken);
        if (!cardExists) return NotFound(new { message = "Carta não encontrada no catálogo." });

        var entries = await _context.DeckCards
            .Where(item => item.DeckId == deckId)
            .ToListAsync(cancellationToken);
        var currentDeckCount = entries.Sum(item => item.Quantity);
        if (currentDeckCount + request.Quantity > MaximumCards)
            return Conflict(new { message = "O deck não pode ultrapassar 60 cartas.", cardCount = currentDeckCount, maximum = MaximumCards });

        var currentCardCopies = entries.Where(item => item.CardId == cardId).Sum(item => item.Quantity);
        var copyLimit = _copyLimitPolicy.GetMaximumCopies(cardId);
        if (currentCardCopies + request.Quantity > copyLimit)
            return Conflict(new { message = $"Esta carta permite no máximo {copyLimit} cópias por deck.", currentCopies = currentCardCopies, maximumCopies = copyLimit });

        var entry = entries.FirstOrDefault(item => item.CardId == cardId && item.Section == request.Section);
        if (entry is null)
        {
            _context.DeckCards.Add(new DeckCard
            {
                DeckId = deckId,
                CardId = cardId,
                Quantity = request.Quantity,
                Section = request.Section
            });
        }
        else
        {
            entry.Quantity += request.Quantity;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await Get(deckId, cancellationToken);
    }

    [HttpPut("{deckId:int}/cards/{cardId:int}")]
    public async Task<ActionResult<DeckDto>> UpdateCardQuantity(
        int deckId,
        int cardId,
        [FromQuery] DeckSection section,
        [FromBody] UpdateDeckCardQuantityRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.IsDefined(section)) return BadRequest(new { message = "Seção inválida." });

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var lockedDeck = await _context.Decks.FromSqlInterpolated(
                $"SELECT * FROM decks WHERE \"Id\" = {deckId} AND \"UserId\" = {userId} FOR UPDATE")
            .AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (lockedDeck is null) return NotFound(new { message = "Deck não encontrado." });

        var entry = await _context.DeckCards.FirstOrDefaultAsync(item =>
            item.DeckId == deckId && item.CardId == cardId && item.Section == section, cancellationToken);
        if (entry is null) return NotFound(new { message = "A carta não está nessa seção do deck." });

        var entries = await _context.DeckCards
            .Where(item => item.DeckId == deckId)
            .ToListAsync(cancellationToken);
        var deckCount = entries.Sum(item => item.Quantity) - entry.Quantity + request.Quantity;
        if (deckCount > MaximumCards)
            return Conflict(new { message = "O deck não pode ultrapassar 60 cartas.", cardCount = deckCount, maximum = MaximumCards });

        var cardCopies = entries.Where(item => item.CardId == cardId).Sum(item => item.Quantity) - entry.Quantity + request.Quantity;
        var copyLimit = _copyLimitPolicy.GetMaximumCopies(cardId);
        if (cardCopies > copyLimit)
            return Conflict(new { message = $"Esta carta permite no máximo {copyLimit} cópias por deck.", currentCopies = cardCopies, maximumCopies = copyLimit });

        entry.Quantity = request.Quantity;
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await Get(deckId, cancellationToken);
    }

    [HttpDelete("{deckId:int}/cards/{cardId:int}")]
    public async Task<ActionResult<DeckDto>> RemoveCard(
        int deckId, int cardId, [FromQuery] DeckSection section = DeckSection.Main, CancellationToken cancellationToken = default)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        if (!Enum.IsDefined(section)) return BadRequest(new { message = "Seção inválida." });

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var lockedDeck = await _context.Decks.FromSqlInterpolated(
                $"SELECT * FROM decks WHERE \"Id\" = {deckId} AND \"UserId\" = {userId} FOR UPDATE")
            .AsNoTracking().FirstOrDefaultAsync(cancellationToken);
        if (lockedDeck is null) return NotFound(new { message = "Deck não encontrado." });

        var entry = await _context.DeckCards.FirstOrDefaultAsync(item =>
            item.DeckId == deckId && item.CardId == cardId && item.Section == section, cancellationToken);
        if (entry is null) return NotFound(new { message = "A carta não está nessa seção do deck." });
        _context.DeckCards.Remove(entry);

        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await Get(deckId, cancellationToken);
    }

    private bool TryGetUserId(out int userId) =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static DeckDto ToDto(int id, string name, DateTime createdAt, IReadOnlyList<DeckCardDto> cards)
    {
        var count = cards.Sum(card => card.Quantity);
        return new DeckDto(id, name, createdAt, count, MinimumCards, MaximumCards, count >= MinimumCards, cards);
    }
}
