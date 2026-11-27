using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebYugi.Model
{
    [Table("users")]
    public class Users
    {
        public Users() { }
        public int Id { get; set; }
        [Required]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string NormalizedName { get; set; } = "";
        public string NormalizedEmail { get; set; } = "";

        [Required]
        public string Senha { get; set; } = string.Empty;

        // False somente para contas legadas, migradas de forma transparente no próximo login válido.
        public bool SenhaHashed { get; set; }

        public int PasswordVersion { get; set; }

        public bool IsAdmin { get; set; }

        public bool MustChangePassword { get; set; }

        public DateTime DataCadastro { get; set; }

        public Users(string name, string email, string senha)
        {
            Name = name;
            Email = email;
            Senha = senha;
            SenhaHashed = false;
            DataCadastro = DateTime.UtcNow;
        }

         // # Serve para que apenas 1 usuario tenha acesso ao seus decks, e que ao deletar o usuario, todos os decks dele sejam deletados também
        public ICollection<Deck> Decks { get; set; } = new List<Deck>();
        public ICollection<UserFavoriteCard> FavoriteCards { get; set; } = new List<UserFavoriteCard>();
        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    }
}
