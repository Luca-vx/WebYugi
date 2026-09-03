using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebYugi.Model
{
    [Table("users")]
    public class Users
    {
        public Users() { }
        public int Id { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public string senha { get; set; }

        public Users(string name, string email, string senha)
        {
            this.name = name;
            this.email = email;
            this.senha = senha;
        }

    }
}
