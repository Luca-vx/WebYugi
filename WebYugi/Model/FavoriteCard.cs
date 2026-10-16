using System.ComponentModel.DataAnnotations.Schema;

namespace WebYugi.Model
{
    [Table("favorite_cards")]
    public class FavoriteCard
    {
        public int UserId { get; set; }

        public int CardId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Users User { get; set; } = null!;
        public Card Card { get; set; } = null!;
    }
}
