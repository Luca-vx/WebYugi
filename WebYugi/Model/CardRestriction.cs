using System.ComponentModel.DataAnnotations.Schema;
using WebYugi.Model.Enums;

namespace WebYugi.Model
{
    [Table("card_restrictions")]
    public class CardRestriction
    {
        public int Id { get; set; }

        public int BanlistId { get; set; }

        public int CardId { get; set; }

        public RestrictionStatus Status { get; set; }

        public int AllowedQuantity { get; set; }

        public Banlist Banlist { get; set; } = null!;
        public Card Card { get; set; } = null!;
    }
}
