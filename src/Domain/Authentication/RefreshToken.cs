using Domain.Users;

namespace Domain.Authentication;

public class RefreshToken
{
	public Guid Id { get; set; } = Guid.CreateVersion7();
	public string Token { get; set; } = default!;
	public DateTime ExpiresAt { get; set; }
	public DateTime CreatedAt { get; set; }
	public bool IsRevoked { get; set; }
	public Guid UserId { get; set; }
	public User User { get; set; } = default!;
}
