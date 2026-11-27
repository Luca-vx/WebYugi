namespace WebYugi.Model;

public class UserFavoriteCard
{
    public int UserId { get; set; }
    public Users User { get; set; } = null!;
    public int CardId { get; set; }
    public Card Card { get; set; } = null!;
}
