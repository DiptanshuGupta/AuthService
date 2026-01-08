using AuthService.Data;
using AuthService.Models;
using AuthService.Services;
using AuthService.Utils;
using Microsoft.EntityFrameworkCore;

namespace AuthService.GraphQL;

public class Mutation
{
    public async Task<User> Register(RegisterInput input, AppDbContext db)
    {
        if (string.IsNullOrWhiteSpace(input.Username) ||
            string.IsNullOrWhiteSpace(input.Email) ||
            string.IsNullOrWhiteSpace(input.Password))
        {
            throw new GraphQLException("All fields are required.");
        }

        var uname = input.Username.Trim().ToLowerInvariant();
        var email = input.Email.Trim().ToLowerInvariant();

        var exists = await db.Users.AnyAsync(u => u.Username == uname || u.Email == email);
        if (exists) throw new GraphQLException("Username or Email already exists.");

        var user = new User
        {
            Username = uname,
            Email = email,
            PasswordHash = PasswordHasher.Hash(input.Password),
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);

        // Default to roleId = 1 if not provided
        var roleId = input.RoleId ?? 1;
        db.UserRoles.Add(new UserRole { User = user, RoleId = roleId });

        await db.SaveChangesAsync();
        return user;
    }

    public async Task<AuthPayload> Login(LoginInput input, AppDbContext db, JwtService jwt)
    {
        var uname = input.UsernameOrEmail.Trim().ToLowerInvariant();
        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == uname || u.Email == uname);

        if (user is null || !PasswordHasher.Verify(input.Password, user.PasswordHash))
            throw new GraphQLException("Invalid credentials.");

        if (!user.IsActive) throw new GraphQLException("User is inactive.");

        // create refresh token (random string)
        var refreshToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "").Replace("/", "").Replace("=", "");

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();

        var pair = jwt.IssueTokens(user, roles, refreshToken);

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = pair.RefreshToken,
            ExpiresAt = pair.RefreshExpiresAt,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        return new AuthPayload(pair.AccessToken, pair.RefreshToken, user.Id, user.Username, user.Email, roles.ToArray(), pair.AccessExpiresAt);
    }

    public async Task<AuthPayload> Refresh(RefreshInput input, AppDbContext db, JwtService jwt)
    {
        var rt = await db.RefreshTokens
            .Include(x => x.User)!.ThenInclude(u => u.UserRoles)!.ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x => x.Token == input.Token);

        if (rt is null || !rt.IsActive) throw new GraphQLException("Invalid refresh token.");

        var user = rt.User!;
        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();

        // rotate refresh token
        var newRefresh = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
            .Replace("+", "").Replace("/", "").Replace("=", "");

        var pair = jwt.IssueTokens(user, roles, newRefresh);

        // revoke old and add new token
        rt.RevokedAt = DateTime.UtcNow;

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = pair.RefreshToken,
            ExpiresAt = pair.RefreshExpiresAt,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        return new AuthPayload(pair.AccessToken, pair.RefreshToken, user.Id, user.Username, user.Email, roles.ToArray(), pair.AccessExpiresAt);
    }

    public async Task<bool> Revoke(RevokeInput input, AppDbContext db)
    {
        var rt = await db.RefreshTokens.FirstOrDefaultAsync(x => x.Token == input.Token);
        if (rt is null) return false;
        if (!rt.RevokedAt.HasValue) rt.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> Logout(LoginInput input, AppDbContext db)
    {
        // optional: revoke all refresh tokens for this user
        var uname = input.UsernameOrEmail.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == uname || u.Email == uname);
        if (user is null) return false;

        var tokens = db.RefreshTokens.Where(t => t.UserId == user.Id && t.RevokedAt == null);
        await tokens.ForEachAsync(t => t.RevokedAt = DateTime.UtcNow);
        await db.SaveChangesAsync();
        return true;
    }
}