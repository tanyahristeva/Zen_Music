using System;
using System.Security.Cryptography;
using System.Text;

namespace Zen_Music
{
    public static class PasswordHelper
    {
        public static string GenerateSalt()
        {
            byte[] salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(salt);
            return Convert.ToBase64String(salt);
        }

        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(
                    Encoding.UTF8.GetBytes(password + salt));
                return Convert.ToBase64String(bytes);
            }
        }

        public static bool VerifyPassword(string password, string salt, string hash)
        {
            return HashPassword(password, salt) == hash;
        }
    }
}