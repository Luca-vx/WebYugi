using System.ComponentModel.DataAnnotations.Schema;

namespace WebYugi.Model
{
    [Table("card_card_subtypes")]
    public class CardCardSubtype
    {
        public int CardId { get; set; }

        public int CardSubtypeId { get; set; }

        public Card Card { get; set; } = null!;
        public CardSubtype CardSubtype { get; set; } = null!;
    }
}
