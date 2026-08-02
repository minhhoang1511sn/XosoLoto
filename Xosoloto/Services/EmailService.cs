using System;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Threading.Tasks;
using Xosoloto.Models;

namespace Xosoloto.Services
{
    /// <summary>
    /// Gửi email (mã OTP đăng ký / quên mật khẩu) qua SMTP.
    /// Cấu hình SMTP được đọc từ %AppData%\Xosoloto\email_config.json.
    /// Nếu file chưa tồn tại, một file mẫu (rỗng) sẽ được tạo ra để người dùng tự điền.
    /// </summary>
    public static class EmailService
    {
        private static readonly string RootFolder =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Xosoloto");

        private static readonly string ConfigFile = Path.Combine(RootFolder, "email_config.json");

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static EmailSettings LoadSettings()
        {
            if (!Directory.Exists(RootFolder))
                Directory.CreateDirectory(RootFolder);

            if (!File.Exists(ConfigFile))
            {
                var defaultSettings = new EmailSettings();
                File.WriteAllText(ConfigFile, JsonSerializer.Serialize(defaultSettings, JsonOptions));
                return defaultSettings;
            }

            try
            {
                var json = File.ReadAllText(ConfigFile);
                return JsonSerializer.Deserialize<EmailSettings>(json) ?? new EmailSettings();
            }
            catch
            {
                return new EmailSettings();
            }
        }

        /// <summary>
        /// Gửi mã OTP tới email. Trả về (success, message) — message chứa lý do lỗi nếu thất bại,
        /// để hiển thị cho người dùng (vd: chưa cấu hình SMTP, sai thông tin đăng nhập SMTP...).
        /// </summary>
        public static async Task<(bool success, string message)> SendOtpEmailAsync(string toEmail, string otp, string purpose)
        {
            var settings = LoadSettings();
            if (!settings.IsConfigured)
            {
                return (false,
                    $"Chưa cấu hình máy chủ gửi email. Vui lòng điền thông tin SMTP vào file:\n{ConfigFile}");
            }

            string subject = purpose == "register"
                ? "Xác thực đăng ký tài khoản - Xổ Số Loto"
                : "Mã đặt lại mật khẩu - Xổ Số Loto";

            string actionText = purpose == "register"
                ? "hoàn tất đăng ký tài khoản"
                : "đặt lại mật khẩu";

            string body =
                $"Xin chào,\n\n" +
                $"Mã OTP của bạn để {actionText} là: {otp}\n\n" +
                $"Mã này có hiệu lực trong 5 phút. Vui lòng không chia sẻ mã này cho bất kỳ ai.\n" +
                $"Nếu bạn không thực hiện yêu cầu này, hãy bỏ qua email này.\n\n" +
                $"Trân trọng,\nXổ Số Loto";

            try
            {
                using var client = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
                {
                    EnableSsl = settings.EnableSsl,
                    Credentials = new NetworkCredential(settings.SmtpUsername, settings.SmtpPassword)
                };

                using var message = new MailMessage
                {
                    From = new MailAddress(settings.FromEmail, settings.FromDisplayName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = false
                };
                message.To.Add(toEmail);

                await client.SendMailAsync(message);
                return (true, "Đã gửi mã OTP tới email của bạn.");
            }
            catch (Exception ex)
            {
                return (false, $"Gửi email thất bại: {ex.Message}");
            }
        }
    }
}
