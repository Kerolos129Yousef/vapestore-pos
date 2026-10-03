using System;
using System.Security.Cryptography;

namespace VapeShopPos.Services
{
    /// <summary>
    /// PBKDF2 (Rfc2898 / SHA-based) password hashing. Passwords are never stored
    /// in plaintext. Stored format: "iterations.saltBase64.hashBase64".
    /// </summary>
    public static class PasswordHasher
    {
        private const int SaltSize = 16;      // 128-bit salt
        private const int HashSize = 32;      // 256-bit subkey
        private const int Iterations = 100000;

        public static string Hash(string password)
        {
            if (password == null) password = string.Empty;

            byte[] salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(salt);

            byte[] hash;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations))
                hash = pbkdf2.GetBytes(HashSize);

            return Iterations + "." + Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(stored)) return false;
            if (password == null) password = string.Empty;

            string[] parts = stored.Split('.');
            if (parts.Length != 3) return false;

            int iterations;
            if (!int.TryParse(parts[0], out iterations)) return false;

            byte[] salt, expected;
            try
            {
                salt = Convert.FromBase64String(parts[1]);
                expected = Convert.FromBase64String(parts[2]);
            }
            catch (FormatException)
            {
                return false;
            }

            byte[] actual;
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
                actual = pbkdf2.GetBytes(expected.Length);

            return FixedTimeEquals(actual, expected);
        }

        // Constant-time comparison to avoid timing attacks.
        private static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
