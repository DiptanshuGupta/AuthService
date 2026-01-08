namespace AuthService.Models;

public class RefreshToken
{
    public long Id { get; set; }

    public long UserId { get; set; }
    public User? User { get; set; }

    public string Token { get; set; } = default!;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }

    public bool IsActive => RevokedAt == null && DateTime.UtcNow < ExpiresAt;
}