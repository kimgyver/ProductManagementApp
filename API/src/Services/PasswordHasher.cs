using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System;
using System.Security.Cryptography;

namespace API.Services;

public static class PasswordHasher
{
  public static string HashPassword(string password)
  {
    byte[] salt = new byte[16];
    using (var rng = RandomNumberGenerator.Create())
    {
      rng.GetBytes(salt);
    }

    byte[] hash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 10000, 32);
    return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
  }

  public static bool VerifyPassword(string password, string storedHash)
  {
    var parts = storedHash.Split(':');
    byte[] salt = Convert.FromBase64String(parts[0]);
    byte[] hash = Convert.FromBase64String(parts[1]);

    byte[] computedHash = KeyDerivation.Pbkdf2(password, salt, KeyDerivationPrf.HMACSHA256, 10000, 32);
    return CryptographicOperations.FixedTimeEquals(computedHash, hash);
  }
}
