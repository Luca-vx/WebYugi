using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebYugi.Model
{
    [Table("card_effects")]
    public class CardEffect
    {
        public int Id { get; set; }

        public int CardId { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        public string? ActivationCondition { get; set; }

        public string? Cost { get; set; }

        public string? Resolution { get; set; }

        public Card Card { get; set; } = null!;
    }
}
