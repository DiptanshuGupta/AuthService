using AuthService.Data;
using AuthService.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AuthService.GraphQL;

public class Query
{
    public async Task<User?> Me(AppDbContext db, ClaimsPrincipal principal)
    {
        var idStr = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        if (string.IsNullOrWhiteSpace(idStr)) return null;
        if (!long.TryParse(idStr, out var userId)) return null;

        return await db.Users.FirstOrDefaultAsync(u => u.Id == userId);
    }
}