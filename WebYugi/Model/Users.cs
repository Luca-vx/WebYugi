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
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Senha { get; set; }

        public DateTime DataCadastro { get; set; }

        // Authentication helpers (kept in Users as requested)
        public bool ValidatePassword(string password)
        {
            // As requested, no hashing or security; plain comparison
            return Senha == password;
        }

        public Users(string name, string email, string senha)
        {
            Name = name;
            Email = email;
            Senha = senha;
            DataCadastro = DateTime.UtcNow;
        }

         // # Serve para que apenas 1 usuario tenha acesso ao seus decks, e que ao deletar o usuario, todos os decks dele sejam deletados também
        public ICollection<Deck> Decks { get; set; } = new List<Deck>();
        public ICollection<UserFavoriteCard> FavoriteCards { get; set; } = new List<UserFavoriteCard>();
    }
}
