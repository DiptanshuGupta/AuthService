using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Security.Cryptography;

namespace AuthService.Utils;

public static class PasswordHasher
{
    public static string Hash(string password, int iterCount = 100_000)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(16);
        byte[] hash = KeyDerivation.Pbkdf2(
            password,
            salt,
            KeyDerivationPrf.HMACSHA256,
            iterCount,
            32);

        return $"pbkdf2$sha256${iterCount}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string encoded)
    {
        try
        {
            var parts = encoded.Split('$');
            if (parts.Length != 5 || parts[0] != "pbkdf2" || parts[1] != "sha256") return false;
            int iterCount = int.Parse(parts[2]);
            byte[] salt = Convert.FromBase64String(parts[3]);
            byte[] expected = Convert.FromBase64String(parts[4]);

            byte[] actual = KeyDerivation.Pbkdf2(
                password,
                salt,
                KeyDerivationPrf.HMACSHA256,
                iterCount,
                32);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
        catch { return false; }
    }
}