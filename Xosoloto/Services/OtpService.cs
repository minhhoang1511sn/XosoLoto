using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Xosoloto.Services
{
    /// <summary>
    /// Sinh và xác thực mã OTP (dùng cho xác thực đăng ký và quên mật khẩu).
    /// OTP được lưu tạm trong bộ nhớ (RAM) của tiến trình ứng dụng, gắn với một "key"
    /// (thường là email đã chuẩn hoá lowercase), có thời hạn 5 phút và chỉ dùng được 1 lần.
    /// Vì đây là app desktop chạy 1 người dùng, không cần lưu OTP xuống đĩa/DB.
    /// </summary>
    public static class OtpService
    {
        private static readonly TimeSpan OtpLifetime = TimeSpan.FromMinutes(5);
        private static readonly int MaxAttempts = 5;

        private class OtpEntry
        {
            public string Code = string.Empty;
            public DateTime ExpiresAt;
            public int Attempts;
        }

        private static readonly ConcurrentDictionary<string, OtpEntry> _store = new();

        private static string NormalizeKey(string key) => (key ?? string.Empty).Trim().ToLowerInvariant();

        /// <summary>Sinh mã OTP 6 chữ số ngẫu nhiên (bảo mật bằng RandomNumberGenerator) và lưu lại theo key.</summary>
        public static string GenerateOtp(string key)
        {
            int code = RandomNumberGenerator.GetInt32(0, 1_000_000);
            string otp = code.ToString("D6");

            _store[NormalizeKey(key)] = new OtpEntry
            {
                Code = otp,
                ExpiresAt = DateTime.UtcNow.Add(OtpLifetime),
                Attempts = 0
            };

            return otp;
        }

        /// <summary>
        /// Kiểm tra mã OTP người dùng nhập vào có đúng, còn hạn không.
        /// Nếu đúng, mã sẽ bị xoá ngay (chỉ dùng được 1 lần).
        /// </summary>
        public static (bool success, string message) VerifyOtp(string key, string inputOtp)
        {
            string normalizedKey = NormalizeKey(key);

            if (!_store.TryGetValue(normalizedKey, out var entry))
                return (false, "Không tìm thấy mã OTP. Vui lòng gửi lại mã.");

            if (DateTime.UtcNow > entry.ExpiresAt)
            {
                _store.TryRemove(normalizedKey, out _);
                return (false, "Mã OTP đã hết hạn. Vui lòng gửi lại mã.");
            }

            entry.Attempts++;
            if (entry.Attempts > MaxAttempts)
            {
                _store.TryRemove(normalizedKey, out _);
                return (false, "Bạn đã nhập sai quá nhiều lần. Vui lòng gửi lại mã.");
            }

            if (!string.Equals(entry.Code, (inputOtp ?? string.Empty).Trim(), StringComparison.Ordinal))
                return (false, "Mã OTP không đúng.");

            _store.TryRemove(normalizedKey, out _);
            return (true, "Xác thực OTP thành công.");
        }

        /// <summary>Xoá OTP đang chờ (vd: khi người dùng huỷ thao tác).</summary>
        public static void Clear(string key)
        {
            _store.TryRemove(NormalizeKey(key), out _);
        }
    }
}
