using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using WebYugi.Model.Enums;

namespace WebYugi.Model
{
    [Table("card_subtypes")]
    public class CardSubtype
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public CardSubtypeCategory Category { get; set; }

        public ICollection<CardCardSubtype> Cards { get; set; } = new List<CardCardSubtype>();
    }
}
