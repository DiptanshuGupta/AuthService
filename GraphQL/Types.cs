namespace AuthService.GraphQL;

public record RegisterInput(string Username, string Email, string Password, short? RoleId);
public record LoginInput(string UsernameOrEmail, string Password, string? DeviceInfo);
public record RefreshInput(string Token);
public record RevokeInput(string Token);

public record AuthPayload(
    string AccessToken,
    string RefreshToken,
    long UserId,
    string Username,
    string Email,
    string[] Roles,
    DateTime ExpiresAt);