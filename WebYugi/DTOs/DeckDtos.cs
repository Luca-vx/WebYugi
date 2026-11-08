using System.ComponentModel.DataAnnotations;
using WebYugi.Model.Enums;

namespace WebYugi.DTOs;

public sealed class CreateDeckRequest
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateDeckRequest
{
    [Required, StringLength(150, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;
}

public sealed class AddDeckCardRequest
{
    [Range(1, 3)]
    public int Quantity { get; set; } = 1;

    public DeckSection Section { get; set; } = DeckSection.Main;
}

public sealed class UpdateDeckCardQuantityRequest
{
    [Range(1, 3)]
    public int Quantity { get; set; }
}

public sealed record DeckCardDto(int CardId, string Name, string? ImageUrl, int Quantity, DeckSection Section);

public sealed record DeckDto(
    int Id,
    string Name,
    DateTime CreatedAt,
    int CardCount,
    int MinimumCardCount,
    int MaximumCardCount,
    bool IsValid,
    IReadOnlyList<DeckCardDto> Cards);
