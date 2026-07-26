using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Xosoloto.Models;

namespace Xosoloto.Services
{
    /// <summary>
    /// Quản lý tài khoản người dùng và cấu hình (settings) đi kèm từng tài khoản.
    /// Toàn bộ dữ liệu lưu cục bộ trong thư mục AppData của Windows,
    /// không cần server / kết nối mạng.
    /// </summary>
    public static class AccountService
    {
        private static readonly string RootFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Xosoloto");

        private static readonly string AccountsFile = Path.Combine(RootFolder, "accounts.json");
        private static readonly string RememberFile = Path.Combine(RootFolder, "remember.txt");

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private static string SettingsFile(string username) =>
            Path.Combine(RootFolder, $"settings_{Sanitize(username)}.json");

        private static string Sanitize(string username) =>
            string.Concat((username ?? string.Empty).Trim().ToLowerInvariant()
                .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '.'));

        private static void EnsureFolder()
        {
            if (!Directory.Exists(RootFolder))
                Directory.CreateDirectory(RootFolder);
        }

        private static List<UserAccount> LoadAccounts()
        {
            EnsureFolder();
            if (!File.Exists(AccountsFile)) return new List<UserAccount>();
            try
            {
                var json = File.ReadAllText(AccountsFile);
                return JsonSerializer.Deserialize<List<UserAccount>>(json) ?? new List<UserAccount>();
            }
            catch
            {
                return new List<UserAccount>();
            }
        }

        private static void SaveAccounts(List<UserAccount> accounts)
        {
            EnsureFolder();
            var json = JsonSerializer.Serialize(accounts, JsonOptions);
            File.WriteAllText(AccountsFile, json);
        }

        private static (string hash, string salt) HashPassword(string password)
        {
            byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100_000, HashAlgorithmName.SHA256);
            byte[] hashBytes = pbkdf2.GetBytes(32);
            return (Convert.ToBase64String(hashBytes), Convert.ToBase64String(saltBytes));
        }

        private static bool VerifyPassword(string password, string storedHash, string storedSalt)
        {
            byte[] saltBytes = Convert.FromBase64String(storedSalt);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, saltBytes, 100_000, HashAlgorithmName.SHA256);
            byte[] hashBytes = pbkdf2.GetBytes(32);
            return CryptographicOperations.FixedTimeEquals(hashBytes, Convert.FromBase64String(storedHash));
        }

        public static bool UsernameExists(string username)
        {
            var accounts = LoadAccounts();
            return accounts.Any(a => a.Username.Equals((username ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase));
        }

        public static (bool success, string message) Register(string username, string password)
        {
            username = (username ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
                return (false, "Tên đăng nhập phải có ít nhất 3 ký tự.");
            if (string.IsNullOrWhiteSpace(password) || password.Length < 4)
                return (false, "Mật khẩu phải có ít nhất 4 ký tự.");

            var accounts = LoadAccounts();
            if (accounts.Any(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase)))
                return (false, "Tên đăng nhập đã tồn tại.");

            var (hash, salt) = HashPassword(password);
            accounts.Add(new UserAccount
            {
                Username = username,
                PasswordHash = hash,
                PasswordSalt = salt,
                CreatedAt = DateTime.Now
            });
            SaveAccounts(accounts);
            return (true, "Tạo tài khoản thành công.");
        }

        public static (bool success, string message) Login(string username, string password)
        {
            username = (username ?? string.Empty).Trim();
            var accounts = LoadAccounts();
            var account = accounts.FirstOrDefault(a => a.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
            if (account == null)
                return (false, "Tài khoản không tồn tại.");

            if (!VerifyPassword(password ?? string.Empty, account.PasswordHash, account.PasswordSalt))
                return (false, "Sai mật khẩu.");

            return (true, "Đăng nhập thành công.");
        }

        // ---------- Ghi nhớ đăng nhập ----------

        public static void RememberUser(string username)
        {
            EnsureFolder();
            File.WriteAllText(RememberFile, (username ?? string.Empty).Trim());
        }

        public static void ForgetUser()
        {
            if (File.Exists(RememberFile))
                File.Delete(RememberFile);
        }

        public static string? GetRememberedUser()
        {
            if (!File.Exists(RememberFile)) return null;
            var username = File.ReadAllText(RememberFile).Trim();
            return UsernameExists(username) ? username : null;
        }

        // ---------- Cấu hình (settings) theo từng tài khoản ----------

        public static void SaveSettings(string username, AppSettingsData settings)
        {
            if (string.IsNullOrWhiteSpace(username)) return;
            EnsureFolder();
            settings.LastSaved = DateTime.Now;
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            File.WriteAllText(SettingsFile(username), json);
        }

        public static AppSettingsData? LoadSettings(string username)
        {
            if (string.IsNullOrWhiteSpace(username)) return null;
            var path = SettingsFile(username);
            if (!File.Exists(path)) return null;
            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppSettingsData>(json);
            }
            catch
            {
                return null;
            }
        }
    }
}
