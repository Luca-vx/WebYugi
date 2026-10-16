using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebYugi.Model
{
    [Table("decks")]
    public class Deck
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Users User { get; set; } = null!;
        public ICollection<DeckCard> DeckCards { get; set; } = new List<DeckCard>();
    }
}
