namespace WebYugi.DTOs
{
    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
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
