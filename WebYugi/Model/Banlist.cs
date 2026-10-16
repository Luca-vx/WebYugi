using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebYugi.Model
{
    [Table("banlists")]
    public class Banlist
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        public DateTime? EffectiveDate { get; set; }

        public ICollection<CardRestriction> Restrictions { get; set; } = new List<CardRestriction>();
    }
}
