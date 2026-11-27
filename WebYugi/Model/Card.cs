using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebYugi.Model.Enums;

namespace WebYugi.Model
{
    [Table("cards")]
    public class Card
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public CardType Type { get; set; }

        public CardAttribute? Attribute { get; set; }

        public MonsterRace? MonsterRace { get; set; }

        public int? Level { get; set; }

        public int? Attack { get; set; }

        public int? Defense { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        public string? Archetype { get; set; }

        public string? Rarity { get; set; }

        public ICollection<DeckCard> DeckCards { get; set; } = new List<DeckCard>();
        public ICollection<UserFavoriteCard> FavoriteUsers { get; set; } = new List<UserFavoriteCard>();
    }
}
