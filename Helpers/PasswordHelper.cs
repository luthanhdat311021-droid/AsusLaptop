using System.Security.Cryptography;
using System.Text;

namespace AsusLaptop.Helpers
{
    public static class PasswordHelper
    {
        /// <summary>
        /// Băm mật khẩu mới bằng thuật toán BCrypt an toàn.
        /// </summary>
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
        }

        /// <summary>
        /// Kiểm tra mật khẩu. Hỗ trợ cả BCrypt và SHA-256 cũ.
        /// Nếu là SHA-256 cũ và khớp mật khẩu, biến needsRehash sẽ là true để Controller tự cập nhật lên BCrypt.
        /// </summary>
        public static bool VerifyPassword(string password, string storedHash, out bool needsRehash)
        {
            needsRehash = false;
            if (string.IsNullOrEmpty(storedHash)) return false;

            // Kiểm tra xem storedHash có phải là dạng BCrypt không ($2a$, $2b$, $2y$)
            if (storedHash.StartsWith("$2a$") || storedHash.StartsWith("$2b$") || storedHash.StartsWith("$2y$"))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(password, storedHash);
                }
                catch
                {
                    return false;
                }
            }

            // Fallback kiểm tra SHA-256 cũ cho các tài khoản cũ trong DB
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(password));
            var legacyHash = Convert.ToBase64String(bytes);

            bool matchesLegacy = (legacyHash == storedHash);
            if (matchesLegacy)
            {
                needsRehash = true; // Báo hiệu cho Controller re-hash lên BCrypt
            }

            return matchesLegacy;
        }

        public static bool VerifyPassword(string password, string storedHash)
        {
            return VerifyPassword(password, storedHash, out _);
        }
    }
}
