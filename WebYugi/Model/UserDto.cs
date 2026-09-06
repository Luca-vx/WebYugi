namespace WebYugi.Model
{
    public class UserDto
    {
        public int Id { get; set; }
        public string name { get; set; }
        public string email { get; set; }

        public UserDto() { }

        public UserDto(int id, string name, string email)
        {
            Id = id;
            this.name = name;
            this.email = email;
        }
    }
}
