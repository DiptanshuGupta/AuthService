// Models/Role.cs
namespace AuthService.Models;
public class Role
{
    public short Id { get; set; }
    public string Name { get; set; } = default!;
    public List<UserRole> UserRoles { get; set; } = new();

}