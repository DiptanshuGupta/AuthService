// Models/UserRole.cs
namespace AuthService.Models;
public class UserRole
{
    public long UserId { get; set; }
    public short RoleId { get; set; }
    public User? User { get; set; }
    public Role? Role { get; set; }
}