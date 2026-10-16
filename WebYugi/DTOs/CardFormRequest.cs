using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using WebYugi.Model.Enums;

namespace WebYugi.DTOs
{
    public class CardFormRequest
    {
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public CardType Type { get; set; }

        public CardAttribute? Attribute { get; set; }

        public MonsterRace? MonsterRace { get; set; }

        [Range(0, 13)]
        public int? Level { get; set; }

        [Range(0, 13)]
        public int? Rank { get; set; }

        public int? Attack { get; set; }

        public int? Defense { get; set; }

        [Required]
        public string Description { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Archetype { get; set; }

        [MaxLength(50)]
        public string? Rarity { get; set; }

        [FromForm(Name = "image")]
        public IFormFile? Image { get; set; }
    }
}
