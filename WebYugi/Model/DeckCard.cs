using System.ComponentModel.DataAnnotations.Schema;
using WebYugi.Model.Enums;

namespace WebYugi.Model
{
    [Table("deck_cards")]
    public class DeckCard
    {
        public int Id { get; set; }

        public int DeckId { get; set; }

        public int CardId { get; set; }

        public int Quantity { get; set; }

        public DeckSection Section { get; set; }

        public Deck Deck { get; set; } = null!;
        public Card Card { get; set; } = null!;
    }
}
