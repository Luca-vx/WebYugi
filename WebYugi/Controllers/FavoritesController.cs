using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebYugi.DTOs;
using WebYugi.Infrastructure;
using WebYugi.Model;

namespace WebYugi.Controllers;

[ApiController]
[Authorize]
[Route("favorites")]
public class FavoritesController(AppDbContext context) : ControllerBase
{
    private int? CurrentUserId => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet]
    public async Task<ActionResult<IEnumerable<CardDto>>> List(CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId) return Unauthorized();
        var cards = await context.UserFavoriteCards.AsNoTracking().Where(f => f.UserId == userId)
            .Select(f => f.Card).OrderBy(c => c.Name).ToListAsync(cancellationToken);
        return Ok(cards.Select(CardDto.FromEntity));
    }

    [HttpPost("{cardId:int}")]
    public async Task<IActionResult> Add(int cardId, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId) return Unauthorized();
        if (!await context.Cards.AnyAsync(c => c.Id == cardId, cancellationToken)) return NotFound(new { message = "A carta não existe no catálogo." });
        if (!await context.UserFavoriteCards.AnyAsync(f => f.UserId == userId && f.CardId == cardId, cancellationToken))
        {
            context.UserFavoriteCards.Add(new UserFavoriteCard { UserId = userId, CardId = cardId });
            await context.SaveChangesAsync(cancellationToken);
        }
        return NoContent();
    }

    [HttpDelete("{cardId:int}")]
    public async Task<IActionResult> Remove(int cardId, CancellationToken cancellationToken)
    {
        if (CurrentUserId is not int userId) return Unauthorized();
        var favorite = await context.UserFavoriteCards.FindAsync([userId, cardId], cancellationToken);
        if (favorite is null) return NoContent();
        context.UserFavoriteCards.Remove(favorite);
        await context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
