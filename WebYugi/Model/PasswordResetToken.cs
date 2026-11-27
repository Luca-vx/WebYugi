using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebYugi.Model;

[Table("password_reset_tokens")]
public sealed class PasswordResetToken
{
    public long Id { get; set; }
    public int UserId { get; set; }
    [Required, StringLength(64)]
    public string TokenHash { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }
    public Users User { get; set; } = null!;
}
