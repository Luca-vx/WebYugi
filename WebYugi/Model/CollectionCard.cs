using System.ComponentModel.DataAnnotations.Schema;

namespace WebYugi.Model
{
    [Table("collection_cards")]
    public class CollectionCard
    {
        public int UserId { get; set; }

        public int CardId { get; set; }

        public int Quantity { get; set; }

        public Users User { get; set; } = null!;
        public Card Card { get; set; } = null!;
    }
}
