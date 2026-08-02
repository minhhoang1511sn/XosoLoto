namespace Xosoloto.Models
{
    /// <summary>
    /// Cấu hình máy chủ SMTP dùng để gửi email OTP (xác thực đăng ký / quên mật khẩu).
    /// Được lưu tại %AppData%\Xosoloto\email_config.json — KHÔNG commit file này vào Git,
    /// vì nó chứa mật khẩu ứng dụng (App Password) của hộp thư gửi.
    ///
    /// Hướng dẫn nhanh với Gmail:
    ///   1. Bật xác minh 2 bước cho tài khoản Gmail dùng để gửi mail.
    ///   2. Tạo "Mật khẩu ứng dụng" (App Password) tại https://myaccount.google.com/apppasswords
    ///   3. Điền SmtpHost = smtp.gmail.com, SmtpPort = 587, EnableSsl = true,
    ///      SmtpUsername = địa chỉ gmail, SmtpPassword = App Password (16 ký tự, không phải mật khẩu Gmail thường).
    /// </summary>
    public class EmailSettings
    {
        public string SmtpHost { get; set; } = "smtp.gmail.com";
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string SmtpUsername { get; set; } = string.Empty;
        public string SmtpPassword { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string FromDisplayName { get; set; } = "Xổ Số Loto";

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(SmtpHost) &&
            !string.IsNullOrWhiteSpace(SmtpUsername) &&
            !string.IsNullOrWhiteSpace(SmtpPassword) &&
            !string.IsNullOrWhiteSpace(FromEmail);
    }
}
