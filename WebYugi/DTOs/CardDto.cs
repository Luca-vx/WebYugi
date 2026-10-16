using WebYugi.Model;
using WebYugi.Model.Enums;

namespace WebYugi.DTOs
{
    public class CardDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public CardType Type { get; set; }
        public CardAttribute? Attribute { get; set; }
        public MonsterRace? MonsterRace { get; set; }
        public int? Level { get; set; }
        public int? Rank { get; set; }
        public int? Attack { get; set; }
        public int? Defense { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public string? Archetype { get; set; }
        public string? Rarity { get; set; }

        public static CardDto FromEntity(Card card)
        {
            return new CardDto
            {
                Id = card.Id,
                Name = card.Name,
                Type = card.Type,
                Attribute = card.Attribute,
                MonsterRace = card.MonsterRace,
                Level = card.Level,
                Rank = card.Rank,
                Attack = card.Attack,
                Defense = card.Defense,
                Description = card.Description,
                ImageUrl = card.ImageUrl,
                Archetype = card.Archetype,
                Rarity = card.Rarity
            };
        }
    }
}
