namespace WebYugi.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime DataCadastro { get; set; }

        public UserDto() { }

        public UserDto(int id, string name, string email, DateTime dataCadastro)
        {
            Id = id;
            Name = name;
            Email = email;
            DataCadastro = dataCadastro;
        }
    }
}
