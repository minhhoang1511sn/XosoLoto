using System;

namespace Xosoloto.Models
{
    /// <summary>
    /// Thông tin tài khoản người dùng, lưu cục bộ trên máy (%AppData%\Xosoloto\accounts.json).
    /// Mật khẩu KHÔNG bao giờ lưu dạng chữ thường (plain-text), chỉ lưu Hash + Salt (PBKDF2/SHA256).
    /// </summary>
    public class UserAccount
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string PasswordSalt { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
